using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MainPower.Osi.Enricher
{
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
                return this[SCADA_KEY].PadLeft(8,'0');
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
