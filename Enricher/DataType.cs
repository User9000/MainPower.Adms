using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// Base class for all externally sourced data.
    /// Is a key-value store - all keys and values are stored as strings.
    /// </summary>
    public abstract class DataType
    {
        private readonly Dictionary<string, string> Data = new Dictionary<string, string>();

        /// <summary>
        /// Get a value for the given key
        /// </summary>
        /// <param name="key">Name of the value (column) to fetch</param>
        /// <returns>The value, or null</returns>
        public string this[string key]
        {
            get
            {
                if (Data.ContainsKey(key))
                    return Data[key];
                else
                    return null;
            }
        }

        /// <summary>
        /// Get a value as an integer for the given key
        /// </summary>
        /// <param name="key">Name of the value (column) to fetch</param>
        /// <returns>The value, or null</returns>
        public int? AsInt(string key)
        {
            if (Data.ContainsKey(key))
                if (int.TryParse(Data[key], out int i))
                    return i;
                else
                    return null;
            else
                return null;
        }

        public bool? AsBool(string key)
        {
            if (Data.ContainsKey(key))
                if (bool.TryParse(Data[key], out bool b))
                    return b;
                else
                    return null;
            else
                return null;
        }

        public double? AsDouble(string key)
        {
            if (Data.ContainsKey(key))
                if (double.TryParse(Data[key], out double d))
                    return d;
                else
                    return null;
            else
                return null;
        }


        public virtual void FromDataRow(DataRow r)
        {
            if (r == null)
                throw new ArgumentNullException();
            for (int i = 0; i < r.ItemArray.Length; i++)
            {
                Data.Add(r.Table.Columns[i].ColumnName, r.ItemArray[i].ToString());
            }
        }

        public virtual void FromSql() { }
        public virtual void FromRestApi() { }
        public virtual void FromOsiDatabase() { }
    }
}