﻿// Copyright (c)  PinusDB All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using PDB.DotNetSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PinusDB.Data
{
    /// <summary>
    ///     Provides methods for reading the result of a command executed against a Taos database.
    /// </summary>
    public class PinusDataReader : DbDataReader
    {
        private readonly PDBConnection _pinus;
        private readonly PinusCommand _command;
        private readonly byte[] _buffer;
        private readonly ProtoHeader proHdr;
        private bool _hasRows;
        private int  _fieldCount;
        private PDBCommand _cmd;
        private int offset;
        List<(string colname, PinusType type)> _metas = new List<(string colname, PinusType type)>();
        private List<object> _record;

        internal PinusDataReader(PinusCommand taosCommand, byte[] buffer)
        {
            _pinus = taosCommand.Connection._pinus;
            _command = taosCommand;
            _buffer = buffer;
            proHdr = new ProtoHeader(buffer);
            PDBErrorCode errorCode = (PDBErrorCode)proHdr.GetReturnVal();
            if (errorCode == PDBErrorCode.PdbE_OK)
            {
                _hasRows = proHdr.GetRecordCnt() > 0;
                _fieldCount = (int)proHdr.GetFieldCnt();
                _cmd = _command._cmd;
                offset = ProtoHeader.kHeadLen;
                var headList = _cmd.GetRecordWithLen(proHdr.GetFieldCnt(), buffer, ref offset);
                foreach (var obj in headList)
                {
                    if (obj.type == PDBType.String)
                    {
                        string str = (string)obj.obj;
                        string[] strArr = str.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (strArr.Count() != 2)
                        {
                            throw new PDBException(PDBErrorCode.PdbE_PACKET_ERROR, "报文错误：非法的表头");
                        }
                        if (Enum.TryParse(strArr[0], true, out PinusType pinusType))
                        {
                            _metas.Add((strArr[1], pinusType));
                        }
                    }
                    else
                    {
                        throw new PDBException(PDBErrorCode.PdbE_PACKET_ERROR, "报文错误：非法的表头");
                    }
                }
            }
            else
            {
                throw new PDBException(errorCode);
            }
        }



        /// <summary>
        ///     Gets the depth of nesting for the current row. Always zero.
        /// </summary>
        /// <value>The depth of nesting for the current row.</value>
        public override int Depth => 0;

        /// <summary>
        ///     Gets the number of columns in the current row.
        /// </summary>
        /// <value>The number of columns in the current row.</value>
        public override int FieldCount => _fieldCount;
 
        /// <summary>
        ///     Gets a value indicating whether the data reader contains any rows.
        /// </summary>
        /// <value>A value indicating whether the data reader contains any rows.</value>
        public override bool HasRows
            => _hasRows;


        /// <summary>
        ///     Gets a value indicating whether the data reader is closed.
        /// </summary>
        /// <value>由于这里是纯粹解析数据， 因此永远返回false，除非后期改成在线读写。</value>
        public override bool IsClosed => false;

        /// <summary>
        ///     Gets the number of rows inserted, updated, or deleted. -1 for SELECT statements.
        /// </summary>
        /// <value>The number of rows inserted, updated, or deleted.</value>
        public override int RecordsAffected
        {
            get
            {
                return (int)proHdr.GetRecordCnt();
            }
        }

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="name">The name of the column. The value is case-sensitive.</param>
        /// <returns>The value.</returns>
        public override object this[string name]
            => this[GetOrdinal(name)];

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value.</returns>
        public override object this[int ordinal]
            => GetValue(ordinal);

        /// <summary>
        ///     Gets an enumerator that can be used to iterate through the rows in the data reader.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public override IEnumerator GetEnumerator()
            => new DbEnumerator(this, closeReader: false);


        /// <summary>
        ///     Advances to the next row in the result set.
        /// </summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public override bool Read()
        {
            bool havedata = offset < _buffer.Count();
            if (havedata)
            {
                _record = _cmd.GetRecord((uint)_fieldCount, _buffer, ref offset);
            }
            return havedata;
        }

        /// <summary>
        ///     Advances to the next result set for batched statements.
        /// </summary>
        /// <returns>true if there are more result sets; otherwise, false.</returns>
        public override bool NextResult()
        {
            return Read();
        }

        /// <summary>
        ///     Closes the data reader.
        /// </summary>
        public override void Close()
            => Dispose(true);

        /// <summary>
        ///     Releases any resources used by the data reader and closes it.
        /// </summary>
        /// <param name="disposing">
        ///     true to release managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _command.DataReader = null;
        }

        /// <summary>
        ///     Gets the name of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the column.</returns>
        public override string GetName(int ordinal)
        {
            return _metas[ordinal].colname;
        }

        /// <summary>
        ///     Gets the ordinal of the specified column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        public override int GetOrdinal(string name)
            => _metas.IndexOf(_metas.FirstOrDefault(m => m.colname == name));

        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).Name;
        }

        /// <summary>
        ///     Gets the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The data type of the column.</returns>
        public override Type GetFieldType(int ordinal)
        {
            if (_metas == null || ordinal >= _metas.Count)
            {
                throw new InvalidOperationException($"DataReaderClosed{nameof(GetFieldType)}");
            }
            return _metas[ordinal].type.ToType();
        }

        /// <summary>
        ///     Gets a value indicating whether the specified column is <see cref="DBNull" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>true if the specified column is <see cref="DBNull" />; otherwise, false.</returns>
        public override bool IsDBNull(int ordinal)
                => GetValue(ordinal) == DBNull.Value;

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="bool" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override bool GetBoolean(int ordinal) => (bool)_record[ordinal];

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="byte" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override byte GetByte(int ordinal) => (byte)_record[ordinal];

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="char" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="DateTime" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)_record[ordinal];
        }

       

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="decimal" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="double" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="float" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="Guid" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="short" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="int" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="long" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="string" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        /// <summary>
        ///     Reads a stream of bytes from the specified column. Not supported.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which the data is copied.</param>
        /// <param name="bufferOffset">The index to which the data will be copied.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {

            throw new NotSupportedException();
        }

        /// <summary>
        ///     Reads a stream of characters from the specified column. Not supported.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which the data is copied.</param>
        /// <param name="bufferOffset">The index to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of characters read.</returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
           => throw new NotSupportedException();

        /// <summary>
        ///     Retrieves data as a Stream. If the reader includes rowid (or any of its aliases), a
        ///     <see cref="TaosBlob"/> is returned. Otherwise, the all of the data is read into memory and a
        ///     <see cref="MemoryStream"/> is returned.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The returned object.</returns>
        public override Stream GetStream(int ordinal)
              => throw new NotSupportedException();

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override T GetFieldValue<T>(int ordinal) => (T)Convert.ChangeType(GetValue(ordinal), typeof(T));




        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override object GetValue(int ordinal) => _record[ordinal];
 

        /// <summary>
        ///     Gets the column values of the current row.
        /// </summary>
        /// <param name="values">An array into which the values are copied.</param>
        /// <returns>The number of values copied into the array.</returns>
        public override int GetValues(object[] values)
        {
            int count = 0;
            for (int i = 0; i < _fieldCount; i++)
            {
                var obj = GetValue(i);
                if (obj != null)
                {
                    values[i] = obj;
                    count++;
                }
            }
            return count;
        }


        /// <summary>
        ///     Returns a System.Data.DataTable that describes the column metadata of the System.Data.Common.DbDataReader.
        /// </summary>
        /// <returns>A System.Data.DataTable that describes the column metadata.</returns>
        public override DataTable GetSchemaTable()
        {
            var schemaTable = new DataTable("SchemaTable");
            if (_metas != null && _metas.Count > 0)
            {
                var ColumnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
                var ColumnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
                var ColumnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
                var NumericPrecision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
                var NumericScale = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));

                var DataType = new DataColumn(SchemaTableColumn.DataType, typeof(Type));
                var DataTypeName = new DataColumn("DataTypeName", typeof(string));

                var IsLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
                var AllowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));

                var IsUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
                var IsKey = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
                var IsAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));

                var BaseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
                var BaseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
                var BaseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
                var BaseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));

                var BaseServerName = new DataColumn(SchemaTableOptionalColumn.BaseServerName, typeof(string));
                var IsAliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(bool));
                var IsExpression = new DataColumn(SchemaTableColumn.IsExpression, typeof(bool));

                var columns = schemaTable.Columns;

                columns.Add(ColumnName);
                columns.Add(ColumnOrdinal);
                columns.Add(ColumnSize);
                columns.Add(NumericPrecision);
                columns.Add(NumericScale);
                columns.Add(IsUnique);
                columns.Add(IsKey);
                columns.Add(BaseServerName);
                columns.Add(BaseCatalogName);
                columns.Add(BaseColumnName);
                columns.Add(BaseSchemaName);
                columns.Add(BaseTableName);
                columns.Add(DataType);
                columns.Add(DataTypeName);
                columns.Add(AllowDBNull);
                columns.Add(IsAliased);
                columns.Add(IsExpression);
                columns.Add(IsAutoIncrement);
                columns.Add(IsLong);

                for (var i = 0; i < _metas.Count; i++)
                {
                    var schemaRow = schemaTable.NewRow();

                    schemaRow[ColumnName] = GetName(i);
                    schemaRow[ColumnOrdinal] = i;
                 //   schemaRow[ColumnSize] = 
                    schemaRow[NumericPrecision] = DBNull.Value;
                    schemaRow[NumericScale] = DBNull.Value;
                    schemaRow[BaseServerName] = _command.Connection.DataSource;
                    var databaseName = _command.Connection.Database;
                    schemaRow[BaseCatalogName] = databaseName;
                    var columnName = GetName(i);
                    schemaRow[BaseColumnName] = columnName;
                    schemaRow[BaseSchemaName] = DBNull.Value;
                    var tableName = string.Empty;
                    schemaRow[BaseTableName] = tableName;
                    schemaRow[DataType] = GetFieldType(i);
                    schemaRow[DataTypeName] = GetDataTypeName(i);
                    schemaRow[IsAliased] = columnName != GetName(i);
                    schemaRow[IsExpression] = columnName == null;
                    schemaRow[IsLong] = DBNull.Value;
                    if (i == 0)
                    {
                        schemaRow[IsKey] = true;
                        schemaRow[DataType] = GetFieldType(i);
                        schemaRow[DataTypeName] = GetDataTypeName(i);
                    }
                    schemaTable.Rows.Add(schemaRow);
                }

            }

            return schemaTable;
        }
    }
}