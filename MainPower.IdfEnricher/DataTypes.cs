using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MainPower.IdfEnricher
{
    public abstract class DataType
    {
        private readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public string this[string s]
        {
            get
            {
                if (Data.ContainsKey(s))
                    return Data[s];
                else
                    return null;
            }
        }

        public int? AsInt(string s)
        {
            if (Data.ContainsKey(s))
                if (int.TryParse(Data[s], out int i))
                    return i;
                else
                    return null;
            else
                return null;
        }

        public double? AsDouble(string s)
        {
            if (Data.ContainsKey(s))
                if (double.TryParse(Data[s], out double d))
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

    internal class T1Disconnector : DataType
    {

    }

    internal class T1Fuse : DataType
    {

    }
    internal class T1HvCircuitBreaker : DataType
    {

    }
    internal class T1RingMainUnit : DataType
    {

    }

    internal class T1Transformer : DataType
    {

    }

    internal class OsiScadaPoint : DataType
    {
        public const string SCADA_KEY = "Key";

        public string Key
        {
            get
            {
                return this[SCADA_KEY];
            }
        }
    }

    internal class OsiScadaStatus : OsiScadaPoint
    {
        
        public const string SCADA_STATES = "pStates";

        public bool QuadState
        {
            get
            {
                return this[SCADA_STATES]?.Count(x => x == '/') == 3;
            }
        }
    }
    internal class OsiScadaAccumulator : OsiScadaPoint
    {

    }
    internal class OsiScadaAnalog : OsiScadaPoint
    {

    }
    internal class AdmsSwitch : DataType
    {

    }
    internal class AdmsTransformer : DataType
    {

    }
    internal class Icp : DataType
    {

    }
}
