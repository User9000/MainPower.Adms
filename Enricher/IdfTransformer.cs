using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public  class IdfTransformer : IdfElement
    {
        #region Constants
        private const string SYMBOL_TX_UNK = "Symbol 34";
        private const string SYMBOL_TX_DYN = "Symbol 21";
        private const string SYMBOL_TX_DYN_OLTC = "Symbol 19";
        private const string SYMBOL_TX_DZN_OLTC = "Symbol 32";
        private const string SYMBOL_TX_ZN = "Symbol 8";
        private const string SYMBOL_TX_YYN = "Symbol 33";
        private const string SYMBOL_TX_YNYN = "Symbol 38";
        private const string SYMBOL_TX_YNA = "Symbol 39";
        private const string SYMBOL_TX_II0 = "Symbol 1";

        private const double SYMBOL_TX_SCALE = 0.3;
        private const double SYMBOL_TX_SCALE_INTERNALS = 0.3;
        private const double SYMBOL_TX_ROTATION = 0;
        
        private const string T1_TX_PRI_OPERATINGKV = "Op#Volt-Tx Power";            //the primary operating voltage
        private const string T1_TX_PRI_RATEDKV = "Rat#Volt-Tx Power";             //the rated primary voltage
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
        private const string ADMS_TX_PARALLEL_SET = "parallelSet";
        private const string ADMS_TX_NER = "nerResistance";

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
        private const string IDF_TX_WINDING_ZIGZAG = "Zig-Zag";
        //TODO: update later
        //private const string IDF_TX_WINDING_ZIGZAGG = "Zig-Zag-G";
        private const string IDF_TX_WINDING_ZIGZAGG = "Wye-G";
        private const string IDF_TX_WINDING_WYEG = "Wye-G";
        private const string IDF_TX_WINDING_DELTA = "Delta";

        private const string IDF_TX_DEFAULT_TYPE = "transformerType_default";

        private const string GIS_TAP = "mpwr_tap_position";
        #endregion

        #region Private fields
        //temporary fields from GIS
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
        private string _symbolName = SYMBOL_TX_UNK;
        private bool _hasOltc = false;

        //transformer type fields
        private string _kva = "";
        private string _maxTap = "";
        private string _minTap = "";
        private string _percresistance = "";
        private string _percreactance = "";
        private string _tapSteps = "";
        private string _nerResistance = "";
        private string _transformerTypeType = "Fixed";
        private short _phaseshift = 11;
        private string _vGroup;

        private DataType _t1Asset = null;
        private DataType _admsAsset = null;
        private DataType _tpAsset = null;

        #endregion

        public IdfTransformer(XElement node, IdfGroup processor) : base(node, processor) { }

        //A list of parallel sets already processed in this import
        private static List<string> ParallelSets { get; set; } = new List<string>();

        public override void Process()
        {
            try
            {
                SetAllNominalStates();
                var geo = ParentGroup.GetSymbolGeometry(Id);
                //default transformer type
                _transformerType = IDF_TX_DEFAULT_TYPE;
                _s1BaseKv = Node.Attribute(IDF_TX_S1BASEKV)?.Value;
                _s2BaseKv = Node.Attribute(IDF_TX_S2BASEKV)?.Value;

                //TODO: need to check these arent empty, e.g. earthing transformer
                _s1kV = double.Parse(_s1BaseKv) * 1000;
                _s2kV = double.Parse(_s2BaseKv) * 1000;

                _bidirectional = IdfTrue;
                _controlPhase = "12";
                _nominalUpstreamSide = "1";
                _standardRotation = IdfTrue;
                _tapSide = "1";

                DataType asset1, asset2 = null;

                //if there is no T1Id, look it up in the transpower dataset
                if (string.IsNullOrEmpty(T1Id))
                {
                    Warn($"T1 asset number is unset");
                    _tpAsset = DataManager.I.RequestRecordById<TranspowerTransformer>(Id);
                    asset1 = asset2 = _tpAsset;
                }
                else
                {
                    //try and find a matching t1 and adms asset
                    _t1Asset = DataManager.I.RequestRecordById<T1Transformer>(T1Id);
                    if (_t1Asset == null)
                    {
                        Warn($"T1 asset number [{T1Id}] was not in T1");
                    }
                    else
                    {
                        _admsAsset = DataManager.I.RequestRecordById<AdmsTransformer>(T1Id);

                    }
                    asset1 = _t1Asset;
                    asset2 = _admsAsset;
                }

                if (asset1 != null)
                {
                    ParseT1kVA(asset1);
                    ParseT1VectorGroup(asset1);

                    if (_vGroup != "ZN")
                    {
                        CalculateStepSize(asset1);
                        CalculateTransformerImpedances(asset1);
                    }
                    else
                    {
                        _percreactance = "7.0";
                        _percresistance = "0.5";
                    }

                    if (asset2 != null)
                    {
                        double? ner = asset2.AsDouble(ADMS_TX_NER);
                        if (ner != null)
                        {
                            _nerResistance = asset2[ADMS_TX_NER];
                        }
                        string parallelSet = asset2[ADMS_TX_PARALLEL_SET];
                        if (!string.IsNullOrWhiteSpace(parallelSet))
                        {
                            if (!ParallelSets.Contains(parallelSet))
                            {
                                ParallelSets.Add(parallelSet);
                            }
                        }
                        string scadaPrefix = asset2[ADMS_TX_SCADAID];
                        if (!string.IsNullOrWhiteSpace(scadaPrefix))
                        {
                            UpdateName(scadaPrefix);
                            GenerateScadaLinking(scadaPrefix);
                        }
                    }
                }


                //If we don't even have the kva, then no point generating a type as it will just generate errors in maestro
                if (string.IsNullOrWhiteSpace(_kva))
                {
                    //TODO: assume 1MVA or something
                    Err("Using generic transformer type as kva was unset");
                }
                else
                {
                    _transformerType = $"{Id}_type";
                    ParentGroup.AddGroupElement(GenerateTransformerType());
                }

                //TODO: only set asset2 attributes
                Node.SetAttributeValue(IDF_TX_BANDWIDTH, _bandwidth);
                Node.SetAttributeValue(IDF_TX_BIDIRECTIONAL, _bidirectional);
                Node.SetAttributeValue(IDF_TX_CONTROLPHASE, _controlPhase);
                Node.SetAttributeValue(IDF_TX_DESIREDVOLTAGE, _desiredVoltage);
                Node.SetAttributeValue(IDF_TX_MAXTAPLIMIT, _maxTapLimit);
                Node.SetAttributeValue(IDF_TX_MINTAPLIMIT, _minTapLimit);
                Node.SetAttributeValue(IDF_TX_PARALLELSET, _parallelSet);
                Node.SetAttributeValue(IDF_TX_REGULATIONTYPE, _regulationType);

                Node.SetAttributeValue(IDF_TX_NOMINALUPSTREAMSIDE, _nominalUpstreamSide);
                Node.SetAttributeValue(IDF_TX_S1BASEKV, _s1BaseKv);
                Node.SetAttributeValue(IDF_TX_S1CONNECTIONTYPE, _s1ConnectionType);
                Node.SetAttributeValue(IDF_TX_S1RATEDKV, _s1BaseKv);
                Node.SetAttributeValue(IDF_TX_S2BASEKV, _s2BaseKv);
                Node.SetAttributeValue(IDF_TX_S2CONNECTIONTYPE, _s2ConnectionType);
                Node.SetAttributeValue(IDF_TX_S2RATEDKV, _s2BaseKv);
                Node.SetAttributeValue(IDF_TX_ROTATION, _standardRotation);
                Node.SetAttributeValue(IDF_TX_TAPSIDE, _tapSide);
                Node.SetAttributeValue(IDF_TX_TXTYPE, _transformerType);

                //TODO: these are invalid fields according to maestro
                //Node.SetAttributeValue(IDF_TX_INITIALTAP1, _initialTap1);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP2, _initialTap2);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP3, _initialTap3);

                //we can remove a bunch of s2 parameters for Zig-Zag transformers
                if (_vGroup == "ZN")
                {
                    Node.SetAttributeValue(IDF_TX_S2BASEKV, null);
                    Node.SetAttributeValue(IDF_TX_S2CONNECTIONTYPE, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId1, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId2, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId3, null);
                    Node.SetAttributeValue(IdfDeviceS2Node, null);
                    Node.SetAttributeValue(IDF_TX_S2RATEDKV, "1");
                }

                ParentGroup.SetSymbolNameByDataLink(Id, _symbolName, SYMBOL_TX_SCALE, SYMBOL_TX_SCALE_INTERNALS, SYMBOL_TX_ROTATION);
                GenerateDeviceInfo();
                RemoveExtraAttributes();

                //TODO: fix this
                if (_vGroup != "ZN")
                {
                    Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Transformer, geo, _phaseshift);
                }

            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }

        public static void GenerateParallelSets(string file)
        {
            XDocument doc = new XDocument();
            XElement data = new XElement("data", new XAttribute("type", "Electric Distribution"), new XAttribute("timestamp", DateTime.UtcNow.ToString("s")), new XAttribute("format", "1.0"));
            XElement groups = new XElement("groups");
            doc.Add(data);
            data.Add(groups);
            XElement xgroup = new XElement("group", new XAttribute("id", "Transformer Parallel Sets"));
            groups.Add(xgroup);

            foreach (var set in ParallelSets)
            {
                XElement x = new XElement("element");
                XAttribute id = new XAttribute("id", $"parallelSet_{set}");
                XAttribute name = new XAttribute("name", set);
                XAttribute type = new XAttribute("type", "Transformer Parallel Set");
                XAttribute pmode = new XAttribute("parallelMode", "Enabled");
                XAttribute ptype = new XAttribute("parallelType", "Solo/Parallel");

                x.Add(id);
                x.Add(type);
                x.Add(name);
                x.Add(pmode);
                x.Add(ptype);
                xgroup.Add(x);
            }
            doc.Save(file);
        }

        #region Tech1 Validation

        private void ParseT1kVA(DataType asset)
        {
            double? v = asset.AsDouble(T1_TX_KVA);
            if (v != null && v != 0)
            {
                _dkva = v.Value;
                //TODO: workaround for small kva's generating weird problems with IDFs
                if (_dkva < 15)
                    _dkva = 15;
                _kva = _dkva.ToString();
            }
            else
                Warn("kVA is unset");
        }

        private void ParseT1VectorGroup(DataType asset)
        {
            string v = _vGroup = asset[T1_TX_VECTORGROUP];
            if (string.IsNullOrWhiteSpace(v))
            {
                if (S2Phases == 3)
                {
                    v = "Dyn11";
                    Warn("Vector group is unset, assuming Dyn11");
                }
                else if (S2Phases == 1)
                {
                    v = "Ii0";
                    Warn("Vector group is unset, assuming Ii0");
                }
                else
                {
                    Err("Vector group is unset, unable to guess vector group");
                }

            }

            switch (v)
            {
                case "Dyn3":
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = _hasOltc ? SYMBOL_TX_DYN_OLTC : SYMBOL_TX_DYN;
                    _phaseshift = 3;
                    return;
                case "Dyn11":
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = _hasOltc ? SYMBOL_TX_DYN_OLTC : SYMBOL_TX_DYN;
                    _phaseshift = 11;
                    return;
                case "Dzn2":
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_ZIGZAGG;
                    _symbolName = SYMBOL_TX_DZN_OLTC;
                    _phaseshift = 2;
                    return;
                case "Yyn0":
                    _s1ConnectionType = IDF_TX_WINDING_WYE;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = SYMBOL_TX_YYN;
                    _phaseshift = 0;
                    return;
                case "Yna0":
                    _s1ConnectionType = IDF_TX_WINDING_WYEG;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = SYMBOL_TX_YNA;
                    _phaseshift = 0;
                    return;
                case "YNyn0":
                    _s1ConnectionType = IDF_TX_WINDING_WYEG;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = SYMBOL_TX_YNYN;
                    _phaseshift = 0;
                    return;
                case "Single":
                case "Ii0":
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = SYMBOL_TX_II0;
                    //phase shift is -30 for non SWER, but 0 for SWER.
                    if (S1Phases == 1)
                        _phaseshift = 0;
                    else
                        _phaseshift = 11;
                    return;
                case "Ii6":
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    _symbolName = SYMBOL_TX_II0;
                    //phase shift is +150 for non SWER, but 180 for SWER
                    if (S1Phases == 1)
                        _phaseshift = 6;
                    else
                        _phaseshift = 5;
                    return;
                case "ZN":
                    _s1ConnectionType = IDF_TX_WINDING_ZIGZAG;
                    _s2ConnectionType = "";
                    _symbolName = SYMBOL_TX_ZN;
                    _phaseshift = 0;
                    break;
                default:
                    Warn($"Couldn't parse vector group [{v}], assuming Dyn11");
                    _s1ConnectionType = IDF_TX_WINDING_DELTA;
                    _s2ConnectionType = IDF_TX_WINDING_WYEG;
                    break;
            }

        }
        #endregion
       
        private void CalculateTransformerImpedances(DataType asset)
        {
            double? zpu = asset.AsDouble(T1_TX_IMPEDANCE);
            double? loadlossW = asset.AsInt(T1_TX_LOADLOSS);
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

        private void CalculateStepSize(DataType asset)
        {
            double? v1 = asset.AsDouble(T1_TX_MINTAP);
            double? v2 = asset.AsDouble(T1_TX_MAXTAP);

            double tapLow, tapHigh, kva;

            if (_kva == "")
            {
                Warn("Can't calculate tap steps as kva is unknown");
                return;
            }
            else
            {
                //guaranteed as we validated previously
                kva = double.Parse(_kva);
            }

            if (v1 == null)
            {
                Warn("Can't calculate tap steps as tapLow wasn't a valid double");
                return;
            }
            if (v2 == null)
            {
                Warn("Can't calculate tap steps as tapHigh wasn't a valid double");
                return;
            }
            tapLow = v1.Value;
            tapHigh = v2.Value;
            if (kva > 3000)
            {
                if (tapLow / 1.25 == (int)(tapLow / 1.25) && tapHigh / 1.25 == (int)(tapHigh / 1.25))
                {
                    _tapSize = 1.25;
                }
                else
                {
                    Warn($"Was expecting tapLow {tapLow} and tapHigh {tapHigh} to be multiples of 1.25");
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
                    Warn($"Could not determine tap size, it wasn't 2.5, 2 or 1.25");
                }
            }
            _numTaps = (int)((tapLow - tapHigh) / 1.25 + 1);
            _tapSteps = _numTaps.ToString();
            double maxTap = (1 + tapLow / 100);
            double minTap = (1 + tapHigh / 100);
            _maxTap = maxTap.ToString();
            _minTap = minTap.ToString();
            _initialTap1 = _initialTap2 = _initialTap3 = ((maxTap - 1) / ((maxTap - minTap) / (_numTaps - 1)) + 1).ToString();
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GisT1Asset, null);
            Node.SetAttributeValue(GIS_TAP, null);
        }

        private void GenerateScadaLinking(string scadaId)
        {
            var tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Tap Position", true);

            //if we don't have the tap position, then assume we don't have any other telemtry either
            //TODO this assumption needs to be documented
            if (tap == null)
                return;
            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            x.SetAttributeValue("tapPosition", tap.Key);

            var remote = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} Supervisory", true);
            if (remote != null)
            {
                x.SetAttributeValue("remoteLocalPoint", remote.Key);
                x.SetAttributeValue("controlAllowState", "1");
            }
            else
            {
                remote = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} AVR Supervisory", true);
                if (remote != null)
                {
                    x.SetAttributeValue("remoteLocalPoint", remote.Key);
                    x.SetAttributeValue("controlAllowState", "1");
                }
            }

            var setpoint = DataManager.I.RequestRecordByColumn<OsiScadaSetpoint>(ScadaName, $"{scadaId} AVR SP1 Value", true);
            if (setpoint != null)
                x.SetAttributeValue("controlPoint", setpoint.Key);

            var voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Volts RY", true);
            if (voltage != null)
                x.SetAttributeValue("controlVoltageReference", voltage.Key);
            //TODO: this is a workaround for bad data
            else if ((voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Volts", true)) != null)
                x.SetAttributeValue("controlVoltageReference", voltage.Key);

            ParentGroup.AddGroupElement(x);
        }

        private XElement GenerateTransformerType()
        {
            XElement t = new XElement("element");
            t.Add(new XAttribute("type", "Transformer Type"));
            t.Add(new XAttribute("id", $"{Id}_type"));
            t.Add(new XAttribute("name", Name));
            t.Add(new XAttribute("kva", _kva));
            t.Add(new XAttribute("ratedKVA", _kva));
            t.Add(new XAttribute("percentResistance", _percresistance));
            t.Add(new XAttribute("percentReactance", _percreactance));
            if (_vGroup != "ZN")
            {
                t.Add(new XAttribute("maxTap", _maxTap));
                t.Add(new XAttribute("minTap", _minTap));
                t.Add(new XAttribute("phases", _phases));
                t.Add(new XAttribute("tapSteps", _tapSteps));
            }
            t.Add(new XAttribute("transformerType", _transformerTypeType));
            t.Add(new XAttribute("lowNeutralResistance", _nerResistance));
            return t;
        }
    }
}
