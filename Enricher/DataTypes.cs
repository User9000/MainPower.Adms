using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MainPower.Adms.Enricher
{
    public class T1Disconnector : DataType { }
    public class T1Fuse : DataType { }
    public class T1HvCircuitBreaker : DataType { }
    public class T1RingMainUnit : DataType { }
    public class T1Transformer : DataType { }

    public class TranspowerTransformer : DataType { }

    public class OsiScadaPoint : DataType
    {
        public const string SCADA_KEY = "Key";

        public string Key
        {
            get
            {
                return this[SCADA_KEY].PadLeft(8, '0');
            }
        }
    }
    public class OsiScadaStatus : OsiScadaPoint
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
    public class OsiScadaAccumulator : OsiScadaPoint { }
    public class OsiScadaAnalog : OsiScadaPoint { }

    public class OsiScadaSetpoint : OsiScadaPoint { }
    public class AdmsSwitch : DataType { }
    public class AdmsTransformer : DataType { }
    public class AdmsSource : DataType { }
    public class AdmsConductor : DataType { }
    public class Icp : DataType { }

}