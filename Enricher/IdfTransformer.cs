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
        private const string SymbolTxUnknown = "Symbol 34";
        private const string SymbolTxDyn = "Symbol 21";
        private const string SymbolTxDynOltc = "Symbol 19";
        private const string SymbolTxDznOltc = "Symbol 32";
        private const string SymbolTxZn = "Symbol 8";
        private const string SymbolTxYyn = "Symbol 33";
        private const string SymbolTxYnyn = "Symbol 38";
        private const string SymbolTxYna = "Symbol 39";
        private const string SymbolTxIi0 = "Symbol 1";

        private const double SymbolTxScale = 0.3;
        private const double SymbolTxScaleInternals = 0.3;
        private const double SymbolTxRotation = 0;
        
        private const string T1TxPrimaryOperatingkV = "Op#Volt-Tx Power";            //the primary operating voltage
        private const string T1TxPrimaryRatedkV = "Rat#Volt-Tx Power";             //the rated primary voltage
        private const string T1TxImpedance = "Z @ Nom Tap & ONAN";          //the impedance at the base kva and base HV voltage(NOT THE ONAN rating)
        private const string T1TxLoadLoss = "Load Loss @ Nom Tap";          //the load loss in W
        private const string T1TxNoLoadLoss = "NoLoad Loss @ NomTap";       //the no load loss in W
        private const string T1TxMaxTap = "Max Tap";                         //the % increase in nom.voltage on the highest tap setting
        private const string T1TxMinTap = "Min Tap";                         //the % decrease in nom.voltage on the lowest tap setting
        private const string T1TxVectorGroup = "Vector Grouping";            //Dyn11, Dyn3, Yya0, Yyan0, Ii0, Ii6, single, 1PH
        private const string T1TxPhases = "No# of Phases";                   //
        private const string T1TxkVA = "Rated Power kVA";                   //maximum rated power
        private const string T1TxPrimaryAmps = "Rated Current (Prim)";         //
        private const string T1TxSecondaryAmps = "Rated Current (Sec)";          //
        private const string T1TxSecondaryRatedkV = "Rated Voltage (Sec)";          //the secondary operating voltage
        private const string T1TxSecondaryOperatingkV = "Op Voltage (Sec)";   //the base secondary voltage
        
        private const string AdmsTxBasekVA = "basekVA";                    //the base kVA to be used for calculation
        private const string AdmsTxS1BasekV = "s1basekV";                  //the primary side base kV used for calculation
        private const string AdmsTxS2BasekV = "s2basekV";                  //the secondary side base kV used for calculation
        private const string AdmsTxBandwidth = "Bandwidth";                // 
        private const string AdmsTxDesiredVoltage = "DesiredVoltage";      // 
        private const string AdmsTxRegulationType = "RegulationType";      // 
        private const string AdmsTxTapSide = "TapSide";                    // 
        private const string AdmsTxMaxTapLimit = "MaxTapControlLimit";     // 
        private const string AdmsTxMinTapLimit = "MinTapControlLimit";     // 
        private const string AdmsTxScadaId = "ScadaId";                    //the prefix used for SCADA e.g.BHL T1
        private const string AdmsTxParallelSet = "ParallelSet";
        private const string AdmsTxNerResistance = "NerResistance";
        private const string AdmsTxRegulatedSide = "RegulatedSide";
        private const string AdmsTxTransformerType = "TransformerType";

        private const string IdfTxBandwidth = "bandwidth";
        private const string IdfTxBidirectional = "bidirectional";
        private const string IdfTxControlPhase = "controlPhase";
        private const string IdfTxDesiredVoltage = "desiredVoltage";
        private const string IdfTxInitialTap1 = "initialTap1";
        private const string IdfTxInitialTap2 = "initialTap2";
        private const string IdfTxInitialTap3 = "initialTap3";
        private const string IdfTxMaxTapLimit = "maxTapControlLimit";
        private const string IdfTxMinTapLimit = "minTapControlLimit";
        private const string IdfTxNominalUpstreamSide = "nominalUpstreamSide";
        private const string IdfTxParallelSet = "parallelSet";
        private const string IdfTxRegulationType = "regulationType";
        private const string IdfTxS1BasekV = "s1baseKV";
        private const string IdfTxS1ConnectionType = "s1connectionType";
        private const string IdfTxS1RatedkV = "s1ratedKV";
        private const string IdfTxS2BasekV = "s2baseKV";
        private const string IdfTxS2ConnectionType = "s2connectionType";
        private const string IdfTxS2RatedkV = "s2ratedKV";
        private const string IdfTxRotation = "standardRotation";
        private const string IdfTxTapSide = "tapSide";
        private const string IdfTxType = "transformerType";

        private const string IdfTxWindingWye = "Wye";
        private const string IdfTxWindingZigZag = "Zig-Zag";
        //TODO: update later
        //private const string IdfTxWindingZigZagG = "Zig-Zag-G";
        private const string IdfTxWindingZigZagG = "Wye-G";
        private const string IdfTxWindingWyeG = "Wye-G";
        private const string IdfTxWindingDelta = "Delta";

        private const string IdfTxDefaultType = "transformerType_default";

        private const string GisTxTap = "mpwr_tap_position";
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
        private string _regulatedNode = "";
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
        private string _symbolName = SymbolTxUnknown;
        private bool _hasOltc = false;

        //transformer type fields
        private string _kva = "";
        private string _maxTap = "";
        private string _minTap = "";
        private string _percresistance = "";
        private string _percreactance = "";
        private string _tapSteps = "";
        private string _nerResistance = "";
        private string _transformerTypeType = "";
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
                _transformerType = IdfTxDefaultType;
                _s1BaseKv = Node.Attribute(IdfTxS1BasekV)?.Value;
                _s2BaseKv = Node.Attribute(IdfTxS2BasekV)?.Value;

                //TODO: need to check these arent empty, e.g. earthing transformer
                _s1kV = double.Parse(_s1BaseKv) * 1000;
                _s2kV = double.Parse(_s2BaseKv) * 1000;

                _bidirectional = IdfTrue;
                _controlPhase = "12";
                _nominalUpstreamSide = "1";
                _standardRotation = IdfTrue;
                _tapSide = "0";
                _transformerTypeType = "Fixed";

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
                        var bw = asset2[AdmsTxBandwidth];
                        if (!string.IsNullOrWhiteSpace(bw))
                            _bandwidth = bw;
                        var desiredVoltage = asset2[AdmsTxDesiredVoltage];
                        if (!string.IsNullOrWhiteSpace(desiredVoltage))
                            _desiredVoltage = desiredVoltage;
                        var tapside = asset2[AdmsTxTapSide];
                        if (!string.IsNullOrWhiteSpace(tapside))
                            _tapSide = tapside;
                        var maxtap = asset2[AdmsTxMaxTapLimit];
                        if (!string.IsNullOrWhiteSpace(maxtap))
                            _maxTapLimit= maxtap;
                        var mintap = asset2[AdmsTxMinTapLimit];
                        if (!string.IsNullOrWhiteSpace(mintap))
                            _minTapLimit = mintap;


                        var txtype = asset2[AdmsTxTransformerType];
                        if (!string.IsNullOrWhiteSpace(txtype))
                        {
                            _transformerTypeType = txtype;
                        }
                        else
                        {
                            _transformerTypeType = "LTC";
                        }
                        var regtype = asset2[AdmsTxRegulationType];
                        if (!string.IsNullOrWhiteSpace(regtype))
                        {
                            _regulationType = regtype;
                        }
                        else
                        {
                            _regulationType = "Automatic";
                        }
                        var regnode = asset2[AdmsTxRegulatedSide];
                        if (regnode == "1")
                        {
                            _regulatedNode = Node1Id;
                        }
                        else
                        {
                            _regulatedNode = Node2Id;
                        }
                        

                        double? ner = asset2.AsDouble(AdmsTxNerResistance);
                        if (ner != null)
                        {
                            _nerResistance = asset2[AdmsTxNerResistance];
                        }
                        _parallelSet = asset2[AdmsTxParallelSet];
                        if (!string.IsNullOrWhiteSpace(_parallelSet))
                        {
                            if (!ParallelSets.Contains(_parallelSet))
                            {
                                ParallelSets.Add(_parallelSet);
                            }
                            _parallelSet = $"parallelSet_{_parallelSet}";
                        }
                        string scadaPrefix = asset2[AdmsTxScadaId];
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
                if (!string.IsNullOrWhiteSpace(_bandwidth))
                    Node.SetAttributeValue(IdfTxBandwidth, _bandwidth);
                Node.SetAttributeValue(IdfTxBidirectional, _bidirectional);
                Node.SetAttributeValue(IdfTxControlPhase, _controlPhase);
                if (!string.IsNullOrWhiteSpace(_desiredVoltage))
                    Node.SetAttributeValue(IdfTxDesiredVoltage, _desiredVoltage);
                Node.SetAttributeValue(IdfTxMaxTapLimit, _maxTapLimit);
                Node.SetAttributeValue(IdfTxMinTapLimit, _minTapLimit);
                Node.SetAttributeValue(IdfTxParallelSet, _parallelSet);
                Node.SetAttributeValue(IdfTxRegulationType, _regulationType);

                Node.SetAttributeValue(IdfTxNominalUpstreamSide, _nominalUpstreamSide);
                Node.SetAttributeValue(IdfTxS1BasekV, _s1BaseKv);
                Node.SetAttributeValue(IdfTxS1ConnectionType, _s1ConnectionType);
                Node.SetAttributeValue(IdfTxS1RatedkV, _s1BaseKv);
                Node.SetAttributeValue(IdfTxS2BasekV, _s2BaseKv);
                Node.SetAttributeValue(IdfTxS2ConnectionType, _s2ConnectionType);
                Node.SetAttributeValue(IdfTxS2RatedkV, _s2BaseKv);
                Node.SetAttributeValue(IdfTxRotation, _standardRotation);
                Node.SetAttributeValue(IdfTxTapSide, _tapSide);
                Node.SetAttributeValue(IdfTxType, _transformerType);

                //TODO: these are invalid fields according to maestro
                //Node.SetAttributeValue(IDF_TX_INITIALTAP1, _initialTap1);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP2, _initialTap2);
                //Node.SetAttributeValue(IDF_TX_INITIALTAP3, _initialTap3);

                //we can remove a bunch of s2 parameters for Zig-Zag transformers
                if (_vGroup == "ZN")
                {
                    Node.SetAttributeValue(IdfTxS2BasekV, null);
                    Node.SetAttributeValue(IdfTxS2ConnectionType, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId1, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId2, null);
                    Node.SetAttributeValue(IdfDeviceS2PhaseId3, null);
                    Node.SetAttributeValue(IdfDeviceS2Node, null);
                    Node.SetAttributeValue(IdfTxS2RatedkV, "1");
                }

                ParentGroup.SetSymbolNameByDataLink(Id, _symbolName, SymbolTxScale, SymbolTxScaleInternals, SymbolTxRotation);

                List<KeyValuePair<string, string>> kvps = new List<KeyValuePair<string, string>>();
                kvps.Add(new KeyValuePair<string, string>("Vector Group", _vGroup));
                
                GenerateDeviceInfo(kvps);
                RemoveExtraAttributes();

                //TODO: fix this
                if (_vGroup != "ZN")
                {
                    Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Transformer, geo.geometry, geo.internals, _phaseshift, _dkva);
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
            double? v = asset.AsDouble(T1TxkVA);
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
            string v = _vGroup = asset[T1TxVectorGroup];
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
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = _hasOltc ? SymbolTxDynOltc : SymbolTxDyn;
                    _phaseshift = 3;
                    return;
                case "Dyn11":
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = _hasOltc ? SymbolTxDynOltc : SymbolTxDyn;
                    _phaseshift = 11;
                    return;
                case "Dzn2":
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingZigZagG;
                    _symbolName = SymbolTxDznOltc;
                    _phaseshift = 2;
                    return;
                case "Yyn0":
                    _s1ConnectionType = IdfTxWindingWye;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = SymbolTxYyn;
                    _phaseshift = 0;
                    return;
                case "Yna0":
                    _s1ConnectionType = IdfTxWindingWyeG;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = SymbolTxYna;
                    _phaseshift = 0;
                    return;
                case "YNyn0":
                    _s1ConnectionType = IdfTxWindingWyeG;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = SymbolTxYnyn;
                    _phaseshift = 0;
                    return;
                case "Single":
                case "Ii0":
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = SymbolTxIi0;
                    //phase shift is -30 for non SWER, but 0 for SWER.
                    if (S1Phases == 1)
                        _phaseshift = 0;
                    else
                        _phaseshift = 11;
                    return;
                case "Ii6":
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    _symbolName = SymbolTxIi0;
                    //phase shift is +150 for non SWER, but 180 for SWER
                    if (S1Phases == 1)
                        _phaseshift = 6;
                    else
                        _phaseshift = 5;
                    return;
                case "ZN":
                    _s1ConnectionType = IdfTxWindingZigZag;
                    _s2ConnectionType = "";
                    _symbolName = SymbolTxZn;
                    _phaseshift = 0;
                    break;
                default:
                    Warn($"Couldn't parse vector group [{v}], assuming Dyn11");
                    _s1ConnectionType = IdfTxWindingDelta;
                    _s2ConnectionType = IdfTxWindingWyeG;
                    break;
            }

        }
        #endregion
       
        private void CalculateTransformerImpedances(DataType asset)
        {
            double? zpu = asset.AsDouble(T1TxImpedance);
            double? loadlossW = asset.AsDouble(T1TxLoadLoss);
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
            //if (Id == "m_st_hs_ms_zwei_wickl_trafo11323724")
                //Debugger.Break();
            double? v1 = asset.AsDouble(T1TxMinTap);
            double? v2 = asset.AsDouble(T1TxMaxTap);

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
            //tech1 stores the ratio difference, not the voltage difference (which is the opposite)
            //hence the minus sign in these calculations
            double minTap = (1 - tapLow / 100);
            double maxTap = (1 - tapHigh / 100);
            _maxTap = maxTap.ToString();
            _minTap = minTap.ToString();
            _initialTap1 = _initialTap2 = _initialTap3 = ((maxTap - 1) / ((maxTap - minTap) / (_numTaps - 1)) + 1).ToString();
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GisT1Asset, null);
            Node.SetAttributeValue(GisTxTap, null);
        }

        private void GenerateScadaLinking(string scadaId)
        {
            var tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Tap Position");

            //if we don't have the tap position, then assume we don't have any other telemtry either
            //TODO this assumption needs to be documented
            if (tap == null)
                return;
            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            x.SetAttributeValue("tapPosition", tap.Key);

            //this is actually auto/manual not remote/local
            var remote = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} AVR Control Mode");
            if (remote != null)
            {
                x.SetAttributeValue("remoteLocalPoint", remote.Key);
                x.SetAttributeValue("controlAllowState", "0");
            }
            else
            {
                x.SetAttributeValue("remoteLocalPoint", "");
            }

            var setpoint = DataManager.I.RequestRecordByColumn<OsiScadaSetpoint>(ScadaName, $"{scadaId} AVR SP1 Value");
            if (setpoint != null)
                x.SetAttributeValue("controlPoint", setpoint.Key);

            //var voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Volts RY", true);
            //if (voltage != null)
                x.SetAttributeValue("controlVoltageReference", _s2BaseKv);
            //TODO: this is a workaround for bad data
            //else if ((voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} Volts", true)) != null)
                //x.SetAttributeValue("controlVoltageReference", voltage.Key);

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
