// Copyright (c)  PinusDB All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using PDB.DotNetSDK;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PinusDB.Data
{
    /// <summary>
    ///     Represents a connection to a Taos database.
    /// </summary>
    public partial class PinusConnection : DbConnection
    {

        private readonly IList<WeakReference<PinusCommand>> _commands = new List<WeakReference<PinusCommand>>();

        private string _connectionString;
        private ConnectionState _state;
        internal PDBConnection _pinus;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PinusConnection" /> class.
        /// </summary>
        public PinusConnection()
        {
          
        }

       
        /// <summary>
        ///     Initializes a new instance of the <see cref="PinusConnection" /> class.
        /// </summary>
        /// <param name="connectionString">The string used to open the connection.</param>
        /// <seealso cref="PinusConnectionStringBuilder" />
        public PinusConnection(string connectionString) : this()
        {
            ConnectionStringBuilder = new PinusConnectionStringBuilder(connectionString);
                _connectionString = connectionString;
            _pinus = new PDBConnection(_connectionString);
        }



        /// <summary>
        ///     Gets or sets a string used to open the connection.
        /// </summary>
        /// <value>A string used to open the connection.</value>
        /// <seealso cref="PinusConnectionStringBuilder" />
        public override string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                ConnectionStringBuilder = new PinusConnectionStringBuilder(value);
                if (_pinus != null)
                {
                    _pinus.Dispose();
                }
                _pinus = new PDBConnection(_connectionString);
            }
        }

        internal PinusConnectionStringBuilder ConnectionStringBuilder { get; set; }


        /// <summary>
        ///     Gets the path to the database file. Will be absolute for open connections.
        /// </summary>
        /// <value>The path to the database file.</value>
        public override string DataSource
        {
            get
            {
                string dataSource = null;

                return dataSource ?? ConnectionStringBuilder.Server;
            }
        }

        /// <summary>
        ///     Gets or sets the default <see cref="PinusCommand.CommandTimeout"/> value for commands created using
        ///     this connection. This is also used for internal commands in methods like
        ///     <see cref="BeginTransaction()"/>.
        /// </summary>
        /// <value>The default <see cref="PinusCommand.CommandTimeout"/> value</value>
        public virtual int DefaultTimeout { get; set; } = 60;


        string _version = string.Empty;
        /// <summary>
        ///     Gets the version of Taos used by the connection.
        /// </summary>
        /// <value>The version of Taos used by the connection.</value>
        public override string ServerVersion
        {
            get
            {
                return _version;
            }
        }
        string _client_version = string.Empty;
        public   string ClientVersion
        {
            get
            {
                return _client_version;
            }
        }
        /// <summary>
        ///     Gets the current state of the connection.
        /// </summary>
        /// <value>The current state of the connection.</value>
        public override ConnectionState State
            => _state;

        /// <summary>
        ///     Gets the <see cref="DbProviderFactory" /> for this connection.
        /// </summary>
        /// <value>The <see cref="DbProviderFactory" />.</value>
        protected override DbProviderFactory DbProviderFactory
            => PinusFactory.Instance;

        /// <summary>
        ///     Gets or sets the transaction currently being used by the connection, or null if none.
        /// </summary>
        /// <value>The transaction currently being used by the connection.</value>
        protected internal virtual PinusTransaction Transaction { get; set; }

        public override string Database => "PinusDB";

        private void SetState(ConnectionState value)
        {
            var originalState = _state;
            if (originalState != value)
            {
                _state = value;
                OnStateChange(new StateChangeEventArgs(originalState, value));
            }
        }

        /// <summary>
        ///     Opens a connection to the database using the value of <see cref="ConnectionString" />. If
        ///     <c>Mode=ReadWriteCreate</c> is used (the default) the file is created, if it doesn't already exist.
        /// </summary>
        /// <exception cref="PinusException">A Taos error occurs while opening the connection.</exception>
        public override void Open()
        {
       
            if (State == ConnectionState.Open)
            {
                return;
            }
            if (ConnectionString == null)
            {
                throw new InvalidOperationException("Open Requires Set ConnectionString");
            }
            _pinus.Open();
           if (_pinus.IsValid())
            {
                SetState(ConnectionState.Open);
            }
            else
            {
                PinusException.ThrowExceptionForRC(-1,"Can't open PinusDB",null);
            }
        }
      
        /// <summary>
        ///     Closes the connection to the database. Open transactions are rolled back.
        /// </summary>
        public override void Close()
        {
            if (State != ConnectionState.Closed)
                _pinus.Close();

            Transaction?.Dispose();
            _nowdatabase = string.Empty;
            foreach (var reference in _commands)
            {
                if (reference.TryGetTarget(out var command))
                {
                    command.Dispose();
                }
            }

            _commands.Clear();


            SetState(ConnectionState.Closed);
        }

        /// <summary>
        ///     Releases any resources used by the connection and closes it.
        /// </summary>
        /// <param name="disposing">
        ///     true to release managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Creates a new command associated with the connection.
        /// </summary>
        /// <returns>The new command.</returns>
        /// <remarks>
        ///     The command's <seealso cref="PinusCommand.Transaction" /> property will also be set to the current
        ///     transaction.
        /// </remarks>
        public new virtual PinusCommand CreateCommand()
            => new PinusCommand { Connection = this, CommandTimeout = DefaultTimeout, Transaction = Transaction };
        public virtual PinusCommand CreateCommand(string commandtext)
          => new PinusCommand { Connection = this, CommandText = commandtext, CommandTimeout = DefaultTimeout, Transaction = Transaction };

        /// <summary>
        ///     Creates a new command associated with the connection.
        /// </summary>
        /// <returns>The new command.</returns>
        protected override DbCommand CreateDbCommand()
            => CreateCommand();

        internal void AddCommand(PinusCommand command)
            => _commands.Add(new WeakReference<PinusCommand>(command));

        internal void RemoveCommand(PinusCommand command)
        {
            for (var i = _commands.Count - 1; i >= 0; i--)
            {
                if (!_commands[i].TryGetTarget(out var item)
                    || item == command)
                {
                    _commands.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///     Create custom collation.
        /// </summary>
        /// <param name="name">Name of the collation.</param>
        /// <param name="comparison">Method that compares two strings.</param>
        public virtual void CreateCollation(string name, Comparison<string> comparison)
            => CreateCollation(name, null, comparison != null ? (_, s1, s2) => comparison(s1, s2) : (Func<object, string, string, int>)null);

        /// <summary>
        ///     Create custom collation.
        /// </summary>
        /// <typeparam name="T">The type of the state object.</typeparam>
        /// <param name="name">Name of the collation.</param>
        /// <param name="state">State object passed to each invocation of the collation.</param>
        /// <param name="comparison">Method that compares two strings, using additional state.</param>
        public virtual void CreateCollation<T>(string name, T state, Func<T, string, string, int> comparison)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(CreateCollation)}");
            }


        }

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <returns>The transaction.</returns>
        public new virtual PinusTransaction BeginTransaction()
            => BeginTransaction(IsolationLevel.Unspecified);

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <returns>The transaction.</returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => BeginTransaction(isolationLevel);

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <returns>The transaction.</returns>
        public new virtual PinusTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(BeginTransaction)}");
            }
            if (Transaction != null)
            {
                throw new InvalidOperationException($"ParallelTransactionsNotSupported");
            }

            return Transaction = new PinusTransaction(this, isolationLevel);
        }
        internal string _nowdatabase = string.Empty;

        /// <summary>
        ///     Changes the current database.  
        /// </summary>
        /// <param name="databaseName">The name of the database to use.</param>
        public override void ChangeDatabase(string databaseName)
        {
            throw new  NotSupportedException("PinusDB 不支持数据库的概念。");
        }

        private class AggregateContext<T>
        {
            public AggregateContext(T seed)
                => Accumulate = seed;

            public T Accumulate { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
