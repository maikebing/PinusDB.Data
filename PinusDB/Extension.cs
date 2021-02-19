using PinusDB.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PinusDB
{
    public static class Extension
    {
        public static PDB.DotNetSDK.PDBType ToPDBType(this Type type)
        {
            return (PDB.DotNetSDK.PDBType)type.ToPinusType();
        }
        public static PDB.DotNetSDK.PDBType ToPDBType(this PinusType type) => (PDB.DotNetSDK.PDBType)type;

        public static Type ToType(this PinusType pType)
        {
            Type type = typeof(object);
            switch (pType)
            {
                case PinusType.Null:
                    type = typeof(DBNull);
                    break;
                case PinusType.Bool:
                    type = typeof(bool);
                    break;
                case PinusType.TinyInt:
                    type = typeof(byte);
                    break;
                case PinusType.ShortInt:
                    type = typeof(ushort);
                    break;
                case PinusType.Int:
                    type = typeof(uint);
                    break;
                case PinusType.BigInt:
                    type = typeof(ulong);
                    break;
                case PinusType.DateTime:
                    type = typeof(DateTime);
                    break;
                case PinusType.Float:
                    type = typeof(float);
                    break;
                case PinusType.Double:
                    type = typeof(double);
                    break;
                case PinusType.String:
                    type = typeof(string);
                    break;
                case PinusType.Blob:
                default:
                    type = typeof(object);
                    break;
            }
          
            return type;
        }

        public  static PinusType ToPinusType(this Type type)
        {
            PinusType pDB;
            switch (TypeInfo.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    pDB = PinusType.Bool;
                    break;
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.SByte:
                    pDB = PinusType.TinyInt;
                    break;
                case TypeCode.DateTime:
                    pDB = PinusType.DateTime;
                    break;
                case TypeCode.DBNull:
                    pDB = PinusType.Null;
                    break;
                case TypeCode.Single:
                    pDB = PinusType.Float;
                    break;
                case TypeCode.Decimal:
                case TypeCode.Double:
                    pDB = PinusType.Double;
                    break;
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    pDB = PinusType.ShortInt;
                    break;
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    pDB = PinusType.Int;
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    pDB = PinusType.BigInt;
                    break;
                case TypeCode.String:
                    pDB = PinusType.String;
                    break;
                case TypeCode.Object:
                default:
                    pDB = PinusType.Blob;
                    break;
            }
            return pDB;
        }
      
        public static List<T> ToObject<T>(this PinusDataReader dataReader)
        {
            List<T> jArray = new List<T>();
            try
            {
                var t = typeof(T);
                var pots = t.GetProperties();
                while (dataReader.Read())
                {
                    T jObject = Activator.CreateInstance<T>();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        try
                        {
                            string strKey = dataReader.GetName(i);
                            if (dataReader[i] != DBNull.Value)
                            {
                                var pr = from p in pots where p.Name == strKey && p.CanWrite select p;
                                if (pr.Any())
                                {
                                    var pi = pr.FirstOrDefault();
                                    pi.SetValue(jObject, Convert.ChangeType(dataReader[i], pi.PropertyType));
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    jArray.Add(jObject);
                }
            }
            catch (Exception ex)
            {
                PinusException.ThrowExceptionForRC(-10002, $"ToObject<{nameof(T)}>  Error", ex);
            }
            return jArray;
        }
         
        public static DataTable ToDataTable(this PinusDataReader reader)
        {
            var datatable = new DataTable();
            datatable.Load(reader);
            return datatable;
        }
    }
}
