using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Transformer : Element
    {
        #region Constants
        private const string SYMBOL_TX = "Symbol 1";
        private const string SYMBOL_TX_OLTC = "Symbol 5";
        private const string SYMBOL_TX_DYN11_OLTC = "Symbol 19";
        private const string SYMBOL_TX_DYN3_OLTC = "Symbol 20";
        private const string SYMBOL_TX_DYN11 = "Symbol 21";
        private const string SYMBOL_TX_DYN3 = "Symbol 22";
        
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
        private const string IDF_TX_S1BASEKV = "s1baseKV";
        private const string IDF_TX_S1CONNECTIONTYPE = "s1connectionType";
        private const string IDF_TX_S1RATEDKV = "s1ratedKV";
        private const string IDF_TX_S2BASEKV = "s2baseKV";
        private const string IDF_TX_S2CONNECTIONTYPE = "s2connectionType";
        private const string IDF_TX_S2RATEDKV = "s2ratedKV";
        private const string IDF_TX_ROTATION = "standardRotation";
        private const string IDF_TX_TAPSIDE = "tapSide";
        private const string IDF_TX_TXTYPE = "transformerType";

        private const string IDF_TX_WINDING_WYE = "Wye";
        private const string IDF_TX_WINDING_ZIGZAG = "Zigzag";
        private const string IDF_TX_WINDING_ZIGZAGG = "Zigzag-G";
        private const string IDF_TX_WINDING_WYEG = "Wye-G";
        private const string IDF_TX_WINDING_DELTA = "Delta";

        private const string IDF_SWITCH_TYPE_FUSE = "Fuse";
        private const string IDF_SWITCH_TYPE_BREAKER = "Breaker";
        private const string IDF_SWITCH_TYPE_SWITCH = "Switch";
        private const string IDF_SWITCH_TYPE_RECLOSER = "Recloser";
        private const string IDF_SWITCH_TYPE_SECTIONALISER = "Sectionaliser";
        #endregion

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
        private string _percresistance = "";
        private string _percreactance = "";
        private string _tapSteps = "";
        private string _nerResistance = "";
        private string _transformerTypeType = "Fixed";
        private string _phaseshift;

        private enum TransformerSide
        {
            HV,
            LV
        }

        public Transformer(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _transformerType = $"TRANSFORMER_TYPE_HV_MV_Transformer";
                if (Node.Attribute(GIS_T1_ASSET) != null)
                    _t1assetno = Node.Attribute(GIS_T1_ASSET).Value;
                

                if (Node.Attribute(IDF_TX_S1BASEKV) != null)
                    _s1BaseKv = Node.Attribute(IDF_TX_S1BASEKV).Value;

                if (Node.Attribute("s2baseKV").Value == "0.2300")
                {
                    Node.SetAttributeValue("s2baseKV", "0.4000");
                    Info("Overriding base voltage from 230V to 400V");
                }

                if (Node.Attribute(IDF_TX_S2BASEKV) != null)
                    _s2BaseKv = Node.Attribute(IDF_TX_S2BASEKV).Value;

                _bidirectional = IDF_TRUE;
                _controlPhase = "2G";
                _nominalUpstreamSide = "1";
                _standardRotation = IDF_TRUE;
                _tapSide = "1";

                if (string.IsNullOrEmpty(_t1assetno))
                {
                    Error( $"T1 asset number is unset");
                    ValidateRatedVoltage(_s1BaseKv, _s1BaseKv, out _s1RatedKv);
                    ValidateRatedVoltage(_s2BaseKv, _s2BaseKv, out _s2RatedKv);
                }
                else
                {
                    DataType asset = DataManager.I.RequestRecordById<T1Transformer>(_t1assetno);
                    if (asset == null)
                    {
                        
                        Error( $"T1 asset number [{_t1assetno}] was not in T1");
                        ValidateRatedVoltage(_s1BaseKv, _s1BaseKv, out _s1RatedKv);
                        ValidateRatedVoltage(_s2BaseKv, _s2BaseKv, out _s2RatedKv);
                    }
                    else
                    {
                        _s1kV = double.Parse(_s1BaseKv) * 1000;
                        _s2kV = double.Parse(_s2BaseKv) * 1000;

                        var t1s1kv = asset.AsDouble(T1_TX_PRI_OPERATINGKV);
                        if (t1s1kv.HasValue && t1s1kv > 300)
                        {
                            if (t1s1kv.Value != _s1kV)
                            {
                                Error($"T1 s1kv [{t1s1kv.Value}] doesn't equal GIS [{_s1kV}]");
                                _s1kV = t1s1kv.Value;
                                _s1BaseKv = (t1s1kv.Value / 1000).ToString();
                            }
                        }
                        else
                        {
                            Warn("T1 s1kv is unset");
                        }
                        var t1s2kv = asset.AsDouble(T1_TX_SEC_OPERATINGKV);
                        if (t1s2kv.HasValue && t1s2kv > 300)
                        {
                            if (t1s2kv.Value != _s2kV)
                            {
                                Error($"T1 s2kv [{t1s2kv.Value}] doesn't equal GIS [{_s2kV}]");
                                _s2kV = t1s2kv.Value;
                                _s2BaseKv = (t1s2kv.Value / 1000).ToString();
                            }
                        }
                        else
                        {
                            Warn("T1 s2kv is unset or invalid");
                        }

                        ValidatePhases(asset.AsInt(T1_TX_PHASES));
                        ValidatekVA(asset.AsDouble(T1_TX_KVA));
                        ValidateVectorGroup(asset[T1_TX_VECTORGROUP]);
                        ValidateRatedVoltage(_s1BaseKv, asset[T1_TX_PRI_RATEDKV], out _s1RatedKv);
                        ValidateRatedVoltage(_s2BaseKv, asset[T1_TX_SEC_RATEDKV], out _s2RatedKv);
                        CalculateStepSize(asset.AsDouble(T1_TX_MINTAP), asset.AsDouble(T1_TX_MAXTAP));
                        CalculateTransformerImpedances(asset.AsDouble(T1_TX_IMPEDANCE), asset.AsInt(T1_TX_LOADLOSS));
                        
                    }
                    asset = DataManager.I.RequestRecordById<AdmsTransformer>(_t1assetno);
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
                    _transformerType = $"{Id}_type";
                    ParentGroup.AddGroupElement(GenerateTransformerType());
                }


                Node.SetAttributeValue(IDF_TX_BANDWIDTH, _bandwidth);
                Node.SetAttributeValue(IDF_TX_BIDIRECTIONAL, _bidirectional);
                Node.SetAttributeValue(IDF_TX_CONTROLPHASE, _controlPhase);
                Node.SetAttributeValue(IDF_TX_DESIREDVOLTAGE, _desiredVoltage);
                //TODO these are invalid fields according to maestro
                //Node.SetAttributeValue(IDF_TX_INITIALTAP1, _initialTap1);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP2, _initialTap2);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP3, _initialTap3);
                Node.SetAttributeValue(IDF_TX_MAXTAPLIMIT, _maxTapLimit);
                Node.SetAttributeValue(IDF_TX_MINTAPLIMIT, _minTapLimit);
                Node.SetAttributeValue(IDF_TX_NOMINALUPSTREAMSIDE, _nominalUpstreamSide);
                Node.SetAttributeValue(IDF_TX_PARALLELSET, _parallelSet);
                Node.SetAttributeValue(IDF_TX_REGULATIONTYPE, _regulationType);
                Node.SetAttributeValue(IDF_TX_S1BASEKV, _s1BaseKv);
                Node.SetAttributeValue(IDF_TX_S1CONNECTIONTYPE, _s1ConnectionType);
                Node.SetAttributeValue(IDF_TX_S1RATEDKV, _s1RatedKv);
                Node.SetAttributeValue(IDF_TX_S2BASEKV, _s2BaseKv);
                Node.SetAttributeValue(IDF_TX_S2CONNECTIONTYPE, _s2ConnectionType);
                Node.SetAttributeValue(IDF_TX_S2RATEDKV, _s2RatedKv);
                Node.SetAttributeValue(IDF_TX_ROTATION, _standardRotation);
                Node.SetAttributeValue(IDF_TX_TAPSIDE, _tapSide);
                Node.SetAttributeValue(IDF_TX_TXTYPE, _transformerType);

                //TODO: Backport into GIS Extractor
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, IDF_TRUE);

                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_TX_DYN11, 0.1, 0);
                GenerateScadaLinking();
                RemoveExtraAttributes();

                Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Transformer, short.Parse(_phaseshift));
            }
            catch (Exception ex)
            {
                Fatal( $"Uncaught exception: {ex.Message}");
            }
        }

        private void ValidateOperatingVoltage(double? v, out double dkv, out string skv)
        {
            if (v != null)
            {
                dkv = v.Value;
                skv = (dkv / 1000).ToString();
            }
            else
            {
                dkv = double.NaN;
                skv = "";
            }
        }
        
        private void CalculateTransformerImpedances(double? zpu, double? loadlossW)
        {
            double baseIHV, baseILV, baseZHV, baseZLV, loadlossV, loadlossIHV, zohmHV, xohmHV, rohmHV, xpu, rpu;
            if (zpu == null || loadlossW == null || loadlossW == 0 || _dkva.Equals(double.NaN) || _s1kV.Equals(double.NaN) || _s2kV.Equals(double.NaN))
            {
                EstimateTransormerImpedance();
                return;
            }
            baseIHV = _dkva * 1000 / _phases / (_s1kV / Math.Sqrt(_phases));
            baseILV = _dkva * 1000 / _phases / (_s2kV / Math.Sqrt(_phases));
            baseZHV = _s1kV / Math.Sqrt(_phases) / baseIHV;
            baseZLV = _s2kV / Math.Sqrt(_phases) / baseILV;
            loadlossV = zpu.Value / Math.Sqrt(_phases) / 100 * _s1kV;
            zohmHV = zpu.Value * baseZHV / 100;
            loadlossIHV = loadlossV / zohmHV;
            rohmHV = loadlossW.Value / _phases / Math.Pow(loadlossIHV, 2);
            xohmHV = Math.Sqrt(Math.Pow(zohmHV, 2) - Math.Pow(rohmHV, 2));
            xpu = xohmHV / baseZHV * 100;
            rpu = rohmHV / baseZHV * 100;
            if (xpu.Equals(double.NaN) || rpu.Equals(double.NaN))
            {
                Warn("xpu or rpu were NaN, will try estimate from kVA instead");
                EstimateTransormerImpedance();
                return;
            }
            _percreactance = xpu.ToString("N5");
            _percresistance = rpu.ToString("N5");
        }

        private void EstimateTransormerImpedance()
        {
            if (!_dkva.Equals(double.NaN))
            {
                double xpu, rpu;
                xpu = -0.00000001 + 0.0007 * _dkva + 3.875;
                rpu = Math.Pow(_dkva, -0.161) * 2.7351;
                _percreactance = xpu.ToString("N5");
                _percresistance = rpu.ToString("N5");
                Info($"Estimating transformer parameters based on kva as input parameters are not valid ({_dkva}kVA, x:{_percreactance} r:{_percresistance}).");
                return;
            }
            else
            {
                Warn("Could not estimate transformer impedances - kVA was null");
                return;
            }
        }

        private void ValidatePhases(int? v)
        {
            if (v == null)
            {
                Warn( "Number of phases is null, assuming 3");
                _phases = 3;
            }
            else if (v == 1 || v == 3)
            {
                _phases =  v.Value;
            }
            else
            {
                Warn( $"Invalid number of phases [{v}], assuming 3");
                _phases = 3;
            }
        }

        private void ValidatekVA(double? v)
        {
            if (v != null && v != 0)
            {
                _dkva = v.Value;
                _kva = v.Value.ToString();
            }
            else
                Warn( "kVA is unset");
        }

        private void CalculateStepSize(double? v1, double? v2)
        {
            double tapLow, tapHigh, kva;

            if (_kva == "") {
                Warn( "Can't calculate tap steps as kva is unknown");
                return;
            }
            else
            {
                //guaranteed as we validated previously
                kva = double.Parse(_kva);
            }
            
            if (v1 == null)
            {
                Warn( "Can't calculate tap steps as tapLow wasn't a valid int");
                return;
            }
            if (v2 == null)
            {
                Warn( "Can't calculate tap steps as tapHigh wasn't a valid int");
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
                    Warn( $"Was expecting tapLow {tapLow} and tapHigh {tapHigh} to be multiples of 1.25");
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
                    Warn( $"Could not determine tap size, it wasn't 2.5, 2 or 1.25");
                }
                _numTaps = (int)((tapLow - tapHigh) / 1.25 + 1);
                _tapSteps = _numTaps.ToString();
                double maxTap = (1 + tapLow / 100);
                double minTap = (1 + tapHigh / 100);
                _maxTap = maxTap.ToString();
                _minTap = minTap.ToString();
                _initialTap1 = _initialTap2 = _initialTap3 = ((maxTap - 1) / ((maxTap - minTap) / (_numTaps - 1)) + 1).ToString();
            }
        }

        private void ValidateRatedVoltage(string opVoltage, string ratedVoltage, out string v)
        {
            //TODO voltages should be line to line, but what about single phase?
            //TODO clean up this mess
            try
            {
                var iOpVoltage = float.Parse(opVoltage);
                if (string.IsNullOrEmpty(ratedVoltage))
                    v = (iOpVoltage * 1.1).ToString();


                if (float.TryParse(ratedVoltage, out var iNewValue))
                {
                    iNewValue /= 1000;

                    if (iNewValue > iOpVoltage)
                    {
                        v = iNewValue.ToString();
                    }
                    else if (iNewValue == iOpVoltage)
                    {
                        Info($"Rated voltage [{iNewValue}] == the operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                    else
                    {
                        Error($"Rated voltage [{iNewValue}] is < or = to the operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                }
                else
                {
                    Error( $"Could not parse rated voltage [{ratedVoltage}], setting to 110% of operating voltage");
                }
                v = (iOpVoltage * 1.1).ToString();
            }
            catch
            {
                Error( $"Operating voltage [{opVoltage}] is not a valid float");
                v =  opVoltage;
            }
        }

        private void ValidateVectorGroup(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                Warn("Vector group is unset, assuming Dyn11");
                _s1ConnectionType = IDF_TX_WINDING_DELTA;
                _s2ConnectionType = IDF_TX_WINDING_WYEG;
                _phaseshift = "11";
                return;
            }
            //TODO we are doing lots of double up here, sort this shit out
            Regex rWinding = new Regex("^[A-Z][a-z]+");
            Regex rphase = new Regex("[0-9]+$");
            var mWinding = rWinding.Match(v);
            var mPhase = rphase.Match(v);
            if (mPhase.Success)
            {
                _phaseshift = mPhase.Value;
            }
            else
            {
                _phaseshift = "11";
                Warn("Couldn't determine phase shift from vector group, guessing at 11");
            }
            if (mWinding.Success)
            {
                switch (mWinding.Value)
                {
                    case "Dyn":
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_WYEG;
                        return;
                    case "Dy":
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_WYE;
                        return;
                    case "Dzn":
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_ZIGZAGG;
                        return;
                    case "Dz":
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_ZIGZAG;
                        return;
                    case "Yyn":
                    case "Yna":
                        _s1ConnectionType = IDF_TX_WINDING_WYE;
                        _s2ConnectionType = IDF_TX_WINDING_WYEG;
                        return;
                    case "YNyn":
                        _s1ConnectionType = IDF_TX_WINDING_WYEG;
                        _s2ConnectionType = IDF_TX_WINDING_WYEG;
                        return;
                    case "Single":
                        //TODO: fiddle with the phases
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_WYEG;
                        return;
                    default:
                        Warn($"Couldn't parse vector group [{v}], assuming Dyn11");
                        _s1ConnectionType = IDF_TX_WINDING_DELTA;
                        _s2ConnectionType = IDF_TX_WINDING_WYEG;
                        break;
                }
            }
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GIS_T1_ASSET, null);
            Node.SetAttributeValue(GIS_TAP, null);
        }

        private void GenerateScadaLinking()
        {
            //throw new NotImplementedException();
        }

        private XElement GenerateTransformerType()
        {
            return XElement.Parse($"<element type=\"Transformer Type\" id=\"{Id}_type\" name=\"{Name}\" kva=\"{_kva}\" ratedKVA=\"{_kva}\" percentResistance=\"{_percresistance}\" percentReactance=\"{_percreactance}\" maxTap=\"{_maxTap}\" minTap=\"{_minTap}\" phases=\"{_phases}\" tapSteps=\"{_tapSteps}\" transformerType=\"{_transformerTypeType}\" lowNeutralResistance=\"{_nerResistance}\" />");
        }
    }
}
