// Copyright (c)  PinusDB All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace PinusDB.Data
{
    /// <summary>
    ///     Provides a simple way to create and manage the contents of connection strings used by
    ///     <see cref="PinusConnection" />.
    /// </summary>
    public class PinusConnectionStringBuilder : DbConnectionStringBuilder
    {
        //server=127.0.0.1;port=4517;username=sa;password=pinusdb<
        private const string ServerKeyword = "server";
        private const string UserNameKeyword = "username";
        private const string PasswordKeyword = "password";
        private const string PortKeyword = "port";
        //
        private enum Keywords
        {
            Server,
            Username,
            Password,
            Port

        }

        private static readonly IReadOnlyList<string> _validKeywords;
        private static readonly IReadOnlyDictionary<string, Keywords> _keywords;

        private string _server = string.Empty;
        private string _userName = "sa";
        private string _password = string.Empty;
        private int  _port = 8105;
        static PinusConnectionStringBuilder()
        {
            var validKeywords = new string[4];
            validKeywords[(int)Keywords.Server] = ServerKeyword;
            validKeywords[(int)Keywords.Username] = UserNameKeyword;
            validKeywords[(int)Keywords.Password] = PasswordKeyword;
            validKeywords[(int)Keywords.Port] = PortKeyword;
            _validKeywords = validKeywords;

            _keywords = new Dictionary<string, Keywords>(4, StringComparer.OrdinalIgnoreCase)
            {
                [ServerKeyword] = Keywords.Server,
                [UserNameKeyword] = Keywords.Username,
                [PasswordKeyword] = Keywords.Password,
                [PortKeyword] = Keywords.Port
            };
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PinusConnectionStringBuilder" /> class.
        /// </summary>
        public PinusConnectionStringBuilder()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PinusConnectionStringBuilder" /> class.
        /// </summary>
        /// <param name="connectionString">
        ///     The initial connection string the builder will represent. Can be null.
        /// </param>
        public PinusConnectionStringBuilder(string connectionString)
            => ConnectionString = connectionString;

        /// <summary>
        ///     Gets or sets the ip address.
        /// </summary>
        /// <value>The server ip address.</value>
        public virtual string Server
        {
            get => _server;
            set => base[ServerKeyword] = _server = value;
        }
        public virtual string Username
        {
            get => _userName;
            set => base[UserNameKeyword] = _userName = value;
        }
  
        public virtual string Password
        {
            get => _password;
            set => base[PasswordKeyword] = _password = value;
        }
        public virtual int  Port
        {
            get => _port;
            set => base[PortKeyword] = _port = value;
        }



        /// <summary>
        ///     Gets a collection containing the keys used by the connection string.
        /// </summary>
        /// <value>A collection containing the keys used by the connection string.</value>
        public override ICollection Keys
            => new ReadOnlyCollection<string>((string[])_validKeywords);

        /// <summary>
        ///     Gets a collection containing the values used by the connection string.
        /// </summary>
        /// <value>A collection containing the values used by the connection string.</value>
        public override ICollection Values
        {
            get
            {
                var values = new object[_validKeywords.Count];
                for (var i = 0; i < _validKeywords.Count; i++)
                {
                    values[i] = GetAt((Keywords)i);
                }

                return new ReadOnlyCollection<object>(values);
            }
        }

      
    


        /// <summary>
        ///     Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="keyword">The key.</param>
        /// <returns>The value.</returns>
        public override object this[string keyword]
        {
            get => GetAt(GetIndex(keyword));
            set
            {
                if (value == null)
                {
                    Remove(keyword);

                    return;
                }

                switch (GetIndex(keyword))
                {
                    case Keywords.Server:
                        Server = Convert.ToString(value, CultureInfo.InvariantCulture);
                        return;
                    case Keywords.Username:
                        Username= Convert.ToString(value, CultureInfo.InvariantCulture);
                        return;
                    case Keywords.Password:
                        Password = Convert.ToString(value, CultureInfo.InvariantCulture);
                        return;
                    case Keywords.Port:
                        Port = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                        return;
                    default:
                        Debug.Assert(false, "Unexpected keyword: " + keyword);
                        return;
                }
            }
        }

        private static TEnum ConvertToEnum<TEnum>(object value)
            where TEnum : struct
        {
            if (value is string stringValue)
            {
                return (TEnum)Enum.Parse(typeof(TEnum), stringValue, ignoreCase: true);
            }

            if (value is TEnum enumValue)
            {
                enumValue = (TEnum)value;
            }
            else if (value.GetType().GetTypeInfo().IsEnum)
            {
                throw new ArgumentException($"ConvertFailed{value.GetType()},{typeof(TEnum)}");
            }
            else
            {
                enumValue = (TEnum)Enum.ToObject(typeof(TEnum), value);
            }

            if (!Enum.IsDefined(typeof(TEnum), enumValue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Invalid Enum Value{typeof(TEnum)},{enumValue}");
            }

            return enumValue;
        }

        /// <summary>
        ///     Clears the contents of the builder.
        /// </summary>
        public override void Clear()
        {
            base.Clear();

            for (var i = 0; i < _validKeywords.Count; i++)
            {
                Reset((Keywords)i);
            }
        }

        /// <summary>
        ///     Determines whether the specified key is used by the connection string.
        /// </summary>
        /// <param name="keyword">The key to look for.</param>
        /// <returns>true if it is use; otherwise, false.</returns>
        public override bool ContainsKey(string keyword)
            => _keywords.ContainsKey(keyword);

        /// <summary>
        ///     Removes the specified key and its value from the connection string.
        /// </summary>
        /// <param name="keyword">The key to remove.</param>
        /// <returns>true if the key was used; otherwise, false.</returns>
        public override bool Remove(string keyword)
        {
            if (!_keywords.TryGetValue(keyword, out var index)
                || !base.Remove(_validKeywords[(int)index]))
            {
                return false;
            }

            Reset(index);

            return true;
        }

        /// <summary>
        ///     Determines whether the specified key should be serialized into the connection string.
        /// </summary>
        /// <param name="keyword">The key to check.</param>
        /// <returns>true if it should be serialized; otherwise, false.</returns>
        public override bool ShouldSerialize(string keyword)
            => _keywords.TryGetValue(keyword, out var index) && base.ShouldSerialize(_validKeywords[(int)index]);

        /// <summary>
        ///     Gets the value of the specified key if it is used.
        /// </summary>
        /// <param name="keyword">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the key was used; otherwise, false.</returns>
        public override bool TryGetValue(string keyword, out object value)
        {
            if (!_keywords.TryGetValue(keyword, out var index))
            {
                value = null;

                return false;
            }

            value = GetAt(index);

            return true;
        }

        private object GetAt(Keywords index)
        {
            switch (index)
            {
                case Keywords.Server:
                    return Server;
                case Keywords.Password:
                    return Password;
                case Keywords.Username:
                    return Username;
                case Keywords.Port:
                    return Port;
                default:
                    Debug.Assert(false, "Unexpected keyword: " + index);
                    return null;
            }
        }

        private static Keywords GetIndex(string keyword)
            => !_keywords.TryGetValue(keyword, out var index)
                ? throw new ArgumentException($"Keyword Not Supported{keyword}")
                : index;

        private void Reset(Keywords index)
        {
            switch (index)
            {
                case Keywords.Server:
                    _server = string.Empty;
                    return;
                case Keywords.Password:
                    _password = string.Empty;
                    return;
                case Keywords.Username:
                    _userName = string.Empty;
                    return;
                case Keywords.Port:
                    _port=6060;
                    return;
                default:
                    Debug.Assert(false, "Unexpected keyword: " + index);
                    return;
            }
        }
    }
}
