using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
    internal class TransformerProcessor : DeviceProcessor
    {
        #region Constants
        private const string SYMBOL_TX = "Symbol 1";
        private const string SYMBOL_TX_OLTC = "Symbol 5";
        private const string SYMBOL_TX_DYN11_OLTC = "Symbol 19";
        private const string SYMBOL_TX_DYN3_OLTC = "Symbol 20";
        private const string SYMBOL_TX_DYN11 = "Symbol 21";
        private const string SYMBOL_TX_DYN3 = "Symbol 22";
        
        private const string TRUE = "True";
        private const string FALSE = "False";

        private const string T1_TX_PRI_OPERATINGKV = "Op Voltage";            //the primary operating voltage
        private const string T1_TX_PRI_RATEDKV = "Rated Voltage";             //the rated primary voltage
        private const string T1_TX_IMPEDANCE = "Z @ Nom Tap & ONAN";          //the impedance at the base kva and base HV voltage(NOT THE ONAN rating)
        private const string T1_TX_LOADLOSS = "Load Loss @ Nom Tap";          //the load loss in W
        private const string T1_TX_NOLOADLOSS = "NoLoad Loss @ NomTap";       //the no load loss in W
        private const string T1_TX_MAXTAP= "Max Tap";                         //the % increase in nom.voltage on the highest tap setting
        private const string T1_TX_MINTAP= "Min Tap";                         //the % decrease in nom.voltage on the lowest tap setting
        private const string T1_TX_VECTORGROUP= "Vector Grouping";            //Dyn11, Dyn3, Yya0, Yyan0, Ii0, Ii6, single, 1PH
        private const string T1_TX_PHASES= "No# of Phases";                    //
        private const string T1_TX_KVA = "Rated Power kVA";                   //maximum rated power
        private const string T1_TX_PRI_AMPS = "Rated Current (Prim)";         //
        private const string T1_TX_SEC_AMPS = "Rated Current (Sec)";          //
        private const string T1_TX_SEC_RATEDKV = "Rated Voltage (Sec)";          //the secondary operating voltage
        private const string T1_TX_SEC_OPERATINGKV = "Op Voltage (Sec)";   //the base secondary voltage

        private const string GIS_TAP = "mpwr_tap_position";
        private const string GIS_T1_ASSET = "mpwr_t1_asset_nbr";
        
        private const string ADMS_TX_BASEKVA = "basekVA";                    //the base kVA to be used for calculation
        private const string ADMS_TX_S1BASEKV = "s1basekV";                  //the primary side base kV used for calculation
        private const string ADMS_TX_S2BASEKV = "s2basekV";                  //the secondary side base kV used for calculation
        private const string ADMS_TX_BANDWIDTH = "bandwidth";                // 
        private const string ADMS_TX_DESIREDVOLTAGE = "desiredVoltage";      // 
        private const string ADMS_TX_REGULATIONTYPE = "regulationType";      // 
        private const string ADMS_TX_TAPSIDE = "tapSide";                    // 
        private const string ADMS_TX_MAXTAPLIMIT = "maxTapControlLimit";     // 
        private const string ADMS_TX_MINTAPLIMIT = "minTapControlLimit";     // 
        private const string ADMS_TX_SCADAID = "scadaId";                    //the prefix used for SCADA e.g.BHL T1

        private const string IDF_TX_BANDWIDTH = "bandwidth";
        private const string IDF_TX_BIDIRECTIONAL = "bidirectional";
        private const string IDF_TX_CONTROLPHASE = "controlPhase";
        private const string IDF_TX_DESIREDVOLTAGE = "desiredVoltage";
        private const string IDF_TX_INITIALTAP1 = "initialTap1";
        private const string IDF_TX_INITIALTAP2 = "initialTap2";
        private const string IDF_TX_INITIALTAP3 = "initialTap3";
        private const string IDF_TX_MAXTAPLIMIT = "maxTapControlLimit";
        private const string IDF_TX_MINTAPLIMIT = "minTapControlLimit";
        private const string IDF_TX_NOMINALUPSTREAMSIDE = "nominalUpstreamSide";
        private const string IDF_TX_PARALLELSET = "parallelSet";
        private const string IDF_TX_REGULATIONTYPE = "regulationType";
        private const string IDF_TX_S1BASEKV = "s1basekV";
        private const string IDF_TX_S1CONNECTIONTYPE = "s1connectionType";
        private const string IDF_TX_S1RATEDKV = "s1ratedKV";
        private const string IDF_TX_S2BASEKV = "s2basekV";
        private const string IDF_TX_S2CONNECTIONTYPE = "s2connectionType";
        private const string IDF_TX_S2RATEDKV = "s2ratedKV";
        private const string IDF_TX_ROTATION = "standardRotation";
        private const string IDF_TX_TAPSIDE = "tapSide";
        private const string IDF_TX_TXTYPE = "transformerType";

        private const string IDF_TX_WINDING_WYE = "Wye";
        private const string IDF_TX_WINDING_ZIGZAG = "Zigzag";
        private const string IDF_TX_WINDING_WYEG = "Wye-Ground";
        private const string IDF_TX_WINDING_DELTA = "Delta";

        private const string IDF_TX_NAME = "name";
        private const string IDF_TX_ID = "id";

        private const string IDF_SWITCH_TYPE_FUSE = "Fuse";
        private const string IDF_SWITCH_TYPE_BREAKER = "Breaker";
        private const string IDF_SWITCH_TYPE_SWITCH = "Switch";
        private const string IDF_SWITCH_TYPE_RECLOSER = "Recloser";
        private const string IDF_SWITCH_TYPE_SECTIONALISER = "Sectionaliser";

        private const string ERR_CAT_TX = "TRANSFORMER";
        private const string ERR_CAT_SCADA = "SCADA";
        private const string ERR_CAT_GENERAL = "GENERAL";

        #endregion

        //compulsory fields
        private string _name = "";
        private string _id = "";

        //temporary fields from GIS
        private string _t1assetno = "";
        private string _gistap = "";
        

        //fields that should be set and validated by this class
        private string _bandwidth = "";
        private string _bidirectional = "";
        private string _controlPhase= "";
        private string _desiredVoltage= "";
        private string _initialTap1= "";
        private string _initialTap2= "";
        private string _initialTap3= "";
        private string _maxTapLimit= "";
        private string _minTapLimit= "";
        private string _nominalUpstreamSide = "";
        private string _parallelSet = "";
        private string _regulationType= "";
        private string _s1BaseKv= "";
        private string _s1ConnectionType = "";
        private string _s1RatedKv= "";
        private string _s2BaseKv= "";
        private string _s2ConnectionType= "";
        private string _s2RatedKv= "";
        private string _standardRotation= "";
        private string _tapSide= "";
        private string _transformerType= "";

        //not exported fields
        private int _numTaps = 0;
        private double _tapSize = double.NaN;
        private int _phases = 3;
        private double _dkva = double.NaN;
        private double _s1kV = double.NaN;
        private double _s2kV = double.NaN;

        //transformer type fields
        private string _kva = "";
        private string _maxTap = "";
        private string _minTap = "";


        private enum TransformerSide
        {
            HV,
            LV
        }

        public TransformerProcessor(XmlElement node, GroupProcessor processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _id = Node.Attributes[IDF_TX_ID].InnerText;
                _name = Node.Attributes[IDF_TX_NAME].InnerText;
                if (Node.HasAttribute(GIS_T1_ASSET))
                    _t1assetno = Node.Attributes[GIS_T1_ASSET].InnerText;
                

                if (Node.HasAttribute(IDF_TX_S1BASEKV))
                    _s1BaseKv = Node.Attributes[IDF_TX_S1BASEKV].InnerText;
                if (Node.HasAttribute(IDF_TX_S2BASEKV))
                    _s2BaseKv = Node.Attributes[IDF_TX_S2BASEKV].InnerText;

                _bidirectional = TRUE;
                _controlPhase = "2G";
                _nominalUpstreamSide = "1";
                _standardRotation = TRUE;
                _tapSide = "1";

                if (string.IsNullOrEmpty(_t1assetno))
                {
                    Error(ERR_CAT_TX, $"T1 asset number is unset");
                }
                else
                {
                    var asset = Enricher.Singleton.GetT1TransformerByAssetNumber(_t1assetno);
                    if (asset == null)
                    {
                        Error(ERR_CAT_TX, $"T1 asset number [{_t1assetno}] was not in T1");
                    }
                    else
                    {
                        //TODO process the tranny t1 data
                        _phases = ValidatePhases(asset[T1_TX_PHASES] as int?);
                        _kva = ValidatekVA(asset[T1_TX_KVA] as double?);
                        _s1ConnectionType = ValidateConnectionType(asset[T1_TX_VECTORGROUP] as string, TransformerSide.HV);
                        _s2ConnectionType = ValidateConnectionType(asset[T1_TX_VECTORGROUP]as string, TransformerSide.LV);
                        _s1RatedKv = ValidateRatedVoltage(asset[T1_TX_PRI_RATEDKV] as string, _s1BaseKv);
                        _s2RatedKv = ValidateRatedVoltage(asset[T1_TX_SEC_RATEDKV] as string, _s2BaseKv);
                        CalculateStepSize(asset[T1_TX_MINTAP]as double?, (double?)asset[T1_TX_MAXTAP] as double?);
                        CalculateTransformerImpedances(asset[T1_TX_IMPEDANCE] as double?, asset[T1_TX_LOADLOSS] as double?);
                        
                    }
                    asset = Enricher.Singleton.GetAdmsTransformerByAssetNumber(_t1assetno);
                    if (asset != null)//not being in the adms database is not an error
                    {
                        //TODO process data from adms database
                        //_bandwidth = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_desiredVoltage = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_maxTapLimit = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_minTapLimit = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_parallelSet = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_regulationType = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_s1BaseKv = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //_s2BaseKv = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as double?);
                        //GenerateScadaLinking();
                    }
                    //GenerateTransformerType();
                }


                Node.SetAttribute(IDF_TX_BANDWIDTH, _bandwidth);
                Node.SetAttribute(IDF_TX_BIDIRECTIONAL, _bidirectional);
                Node.SetAttribute(IDF_TX_CONTROLPHASE, _controlPhase);
                Node.SetAttribute(IDF_TX_DESIREDVOLTAGE, _desiredVoltage);
                Node.SetAttribute(IDF_TX_INITIALTAP1, _initialTap1);
                Node.SetAttribute(IDF_TX_INITIALTAP2, _initialTap2);
                Node.SetAttribute(IDF_TX_INITIALTAP3, _initialTap3);
                Node.SetAttribute(IDF_TX_MAXTAPLIMIT, _maxTapLimit);
                Node.SetAttribute(IDF_TX_MINTAPLIMIT, _minTapLimit);
                Node.SetAttribute(IDF_TX_NOMINALUPSTREAMSIDE, _nominalUpstreamSide);
                Node.SetAttribute(IDF_TX_PARALLELSET, _parallelSet);
                Node.SetAttribute(IDF_TX_REGULATIONTYPE, _regulationType);
                Node.SetAttribute(IDF_TX_S1BASEKV, _s1BaseKv);
                Node.SetAttribute(IDF_TX_S1CONNECTIONTYPE, _s1ConnectionType);
                Node.SetAttribute(IDF_TX_S1RATEDKV, _s1RatedKv);
                Node.SetAttribute(IDF_TX_S2BASEKV, _s2BaseKv);
                Node.SetAttribute(IDF_TX_S2CONNECTIONTYPE, _s2ConnectionType);
                Node.SetAttribute(IDF_TX_S2RATEDKV, _s2RatedKv);
                Node.SetAttribute(IDF_TX_ROTATION, _standardRotation);
                Node.SetAttribute(IDF_TX_TAPSIDE, _tapSide);
                Node.SetAttribute(IDF_TX_TXTYPE, _transformerType);

                GenerateScadaLinking();
                RemoveExtraAttributes();

            }
            catch (Exception ex)
            {
                Error(ERR_CAT_GENERAL, $"Uncaught exception in {nameof(Process)}: {ex.Message}");
            }
        }

        private void CalculateTransformerImpedances(double? v1, double? v2)
        {
            //throw new NotImplementedException();
        }

        private int ValidatePhases(int? v)
        {
            if (v == null)
            {
                Warn(ERR_CAT_TX, "Number of phases is null, assuming 3");
                return 3;
            }
            else if (v == 1 || v == 3)
            {
                return (int)v;
            }
            else
            {
                Warn(ERR_CAT_TX, $"Invalid number of phases [{v}], assuming 3");
                return 3;
            }
        }

        private string ValidatekVA(double? v)
        {
            if (v != null)
                return v.Value.ToString();
            else
                return "";
        }

        private void CalculateStepSize(double? v1, double? v2)
        {
            double tapLow, tapHigh, kva;

            if (_kva == "") {
                Warn(ERR_CAT_TX, "Can't calculate tap steps as kva is unknown");
                return;
            }
            else
            {
                //guaranteed as we validated previously
                kva = double.Parse(_kva);
            }
            
            if (v1 == null)
            {
                Warn(ERR_CAT_TX, "Can't calculate tap steps as tapLow wasn't a valid int");
                return;
            }
            if (v2 == null)
            {
                Warn(ERR_CAT_TX, "Can't calculate tap steps as tapHigh wasn't a valid int");
                return;
            }
            tapLow = v1.Value ;
            tapHigh = v2.Value;
            if (kva > 3000)
            {
                if (tapLow / 1.25 == (int)(tapLow / 1.25) && tapHigh / 1.25 == (int)(tapHigh / 1.25))
                {
                    _tapSize = 1.25;
                }
                else
                {
                    Warn(ERR_CAT_TX, $"Was expecting tapLow {tapLow} and tapHigh {tapHigh} to be multiples of 1.25");
                    return;
                }
            }
            else
            {
                if (tapLow / 2.5 == (int)(tapLow / 2.5) && tapHigh / 2.5 == (int)(tapHigh / 2.5))
                {
                    _tapSize = 2.5;
                }
                else if (tapLow / 2 == (int)(tapLow / 2) && tapHigh / 2 == (int)(tapHigh / 2))
                {
                    _tapSize = 2;
                }
                else if (tapLow / 1.25 == (int)(tapLow / 1.25) && tapHigh / 1.25 == (int)(tapHigh / 1.25))
                {
                    _tapSize = 1.25;
                }
                else
                {
                    Warn(ERR_CAT_TX, $"Could not determine tap size, it wasn't 2.5, 2 or 1.25");
                }
                _numTaps = (int)((tapLow - tapHigh) / 1.25 + 1);

                double maxTap = (1 + tapLow / 100);
                double minTap = (1 + tapHigh / 100);
                _maxTap = maxTap.ToString();
                _minTap = minTap.ToString();
                _initialTap1 = _initialTap2 = _initialTap3 = ((maxTap - 1) / ((maxTap - minTap) / (_numTaps - 1)) + 1).ToString();
            }
        }

        private string ValidateRatedVoltage(string opVoltage, string ratedVoltage)
        {
            //TODO voltages should be line to line, but what about single phase?
            try
            {
                var iOpVoltage = float.Parse(opVoltage);
                if (string.IsNullOrEmpty(ratedVoltage))
                    return (iOpVoltage * 1.1).ToString();


                if (float.TryParse(ratedVoltage, out var iNewValue))
                {
                    iNewValue /= 1000;

                    if (iNewValue > iOpVoltage)
                    {
                        return iNewValue.ToString();
                    }
                    else
                    {
                        Error(ERR_CAT_TX, $"Rated voltage [{ratedVoltage}] is less the operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                }
                else
                {
                    Error(ERR_CAT_TX, $"Could not parse rated voltage [{ratedVoltage}], setting to 110% of operating voltage");
                }
                return (iOpVoltage * 1.1).ToString();
            }
            catch
            {
                Error(ERR_CAT_TX, $"Operating voltage [{opVoltage}] is not a valid float");
                return opVoltage;
            }
        }

        private string ValidateConnectionType(string v, TransformerSide lV)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                Warn(ERR_CAT_TX, "Vector group is blank");
                return "";
            }
            Regex r = new Regex("^[A-Z][a-z]+");
            var m = r.Match(v);
            if (m.Success)
            {
                switch (m.Value)
                {
                    case "Dyn":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_DELTA : IDF_TX_WINDING_WYEG;
                    case "Dy":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_DELTA : IDF_TX_WINDING_WYE;
                    case "Dzn":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_DELTA : IDF_TX_WINDING_ZIGZAG;
                    case "Yyn":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_WYE : IDF_TX_WINDING_WYEG;
                    case "YNyn":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_WYEG : IDF_TX_WINDING_WYEG;
                    case "Yna":
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_WYE : IDF_TX_WINDING_WYEG;
                    case "Single"://TODO
                        return lV == TransformerSide.HV ? IDF_TX_WINDING_WYE : IDF_TX_WINDING_WYEG;
                    default:
                        Warn(ERR_CAT_TX, $"Couldn't parse vector group [{v}]");
                        return "";
                }
            }
            return "";
        }

        private void RemoveExtraAttributes()
        {
            Node.RemoveAttribute(GIS_T1_ASSET);
            Node.RemoveAttribute(GIS_TAP);
        }

        private void GenerateScadaLinking()
        {
            //throw new NotImplementedException();
        }

        #region Overrides
        protected override void Debug(string code, string message)
        {
            _log.Debug($"TRANSFORMER,{code},{_id}/{_name},{message}");
        }

        protected override void Error(string code, string message)
        {
            _log.Error($"TRANSFORMER,{code},{_id}/{_name},{message}");
        }

        protected override void Info(string code, string message)
        {
            _log.Info($"TRANSFORMER,{code},{_id}/{_name},{message}");
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn($"TRANSFORMER,{code},{_id}/{_name},{message}");
        }
        #endregion
    }
}
