using System;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    class Switch : Element
    {

        #region Constants
        private const string SYMBOL_CIRCUITBREAKER = "Symbol 0";
        private const string SYMBOL_CIRCUITBREAKER_QUAD = "Symbol 14";
        private const string SYMBOL_SWITCH = "Symbol 3";
        private const string SYMBOL_SWITCH_QUAD = "Symbol 15";
        private const string SYMBOL_LBS = "Symbol 6";
        private const string SYMBOL_LBS_QUAD = "Symbol 16";
        private const string SYMBOL_FUSE = "Symbol 2";
        private const string SYMBOL_SERVICE_FUSE = "Symbol 2";
        private const string SYMBOL_LINKS = "Symbol 9";
        private const string SYMBOL_DISCONNECTOR = "Symbol 3";
        private const string SYMBOL_DISCONNECTOR_QUAD = "Symbol 15";
        private const string SYMBOL_ENTEC_QUAD = "Symbol 16";
        private const string SYMBOL_ENTEC = "Symbol 6";
        private const string SYMBOL_FUSESAVER = "Symbol 10";
        private const string SYMBOL_LVSWITCH = "Symbol 3";
        private const string SYMBOL_LVFUSE = "Symbol 17";
        private const string SYMBOL_HVFUSESWITCH = "Symbol 17";
        private const string SYMBOL_HVFUSESWITCH_QUAD = "Symbol 17";

        private const string T1_FUSE_GANGED = "Is Tri Fuse";
        private const string T1_SWITCH_SWNUMBER = "Switch Number";
        private const string T1_SWITCH_RATED_VOLTAGE = "Rated Voltage";
        private const string T1_SWITCH_RATED_AMPS = "Rated Current";
        private const string T1_SWITCH_MAX_INTERRUPT_AMPS = "Fault kA/sec";
        private const string T1_SWITCH_LOAD_BREAK_RATING = "Load Break Rating";
        private const string T1_SWITCH_SW1 = "SW 1";
        private const string T1_SWITCH_SW2 = "SW 2";
        private const string T1_SWITCH_SW3 = "SW 3";
        private const string T1_SWITCH_SW4 = "SW4";

        private const string T1_SWITCH_FUSE_RATED_VOLTAGE = "Rat#Volt-Do Fuse";
        private const string T1_SWITCH_FUSE_OP_VOLTAGE = "Op#Volt-Do Fuse";

        private const string T1_SWITCH_HVCB_RATED_VOLTAGE = "Op#Volt-HV CircuitBr";
        private const string T1_SWITCH_HVCB_OP_VOLTAGE = "Rat#Volt-HV Circuit";

        private const string T1_SWITCH_RMU_RATED_VOLTAGE = "Op#Volt-RMU";
        private const string T1_SWITCH_RMU_OP_VOLTAGE = "Rate#Volt-RMU";

        private const string T1_SWITCH_DISCO_RATED_VOLTAGE = "Op#Volt-Disconnector";
        private const string T1_SWITCH_DISCO_OP_VOLTAGE = "Rat#Volt-Disconnector";

        private const string IDF_SWITCH_BIDIRECTIONAL = "bidirectional";
        private const string IDF_SWITCH_FORWARDTRIPAMPS = "forwardTripAmps";
        private const string IDF_SWITCH_REVERSETRIPAMPS = "reverseTripAmps";
        private const string IDF_SWITCH_GANGED = "ganged";
        private const string IDF_SWITCH_LOADBREAKCAPABLE = "loadBreakCapable";
        private const string IDF_SWITCH_MAXINTERRUPTAMPS = "maxInterruptAmps";
        private const string IDF_SWITCH_RATEDAMPS = "ratedAmps";
        private const string IDF_SWITCH_RATEDKV = "ratedKV";
        private const string IDF_SWITCH_BASEKV = "baseKV";
        private const string IDF_SWITCH_SWITCHTYPE = "switchType";
        private const string IDF_SWITCH_NOMINALUPSTREAMSIDE = "nominalUpstreamSide";

        private const string IDF_SWITCH_TYPE_FUSE = "Fuse";
        private const string IDF_SWITCH_TYPE_BREAKER = "Breaker";
        private const string IDF_SWITCH_TYPE_SWITCH = "Switch";
        private const string IDF_SWITCH_TYPE_RECLOSER = "Recloser";
        private const string IDF_SWITCH_TYPE_SECTIONALISER = "Sectionaliser";

        private const string ADMS_SWITCH_FORWARDTRIPAMPS = "forwardTripAmps";
        private const string ADMS_SWITCH_REVERSETRIPAMPS = "reverseTripAmps";
        private const string ADMS_SWITCH_RECLOSER_ENABLED = "Recloser";
        private const string ADMS_SWITCH_NOMINALUPSTREAMSIDE = "NominalUpstreamSide";
        private const string ADMS_SWITCH_NOTIFYUPSTREAMSIDE = "NotofyUpstreamSide";

        private const string IDF_SWITCH_SCADA_P1_STATE = "p1State";
        private const string IDF_SWITCH_SCADA_P2_STATE = "p2State";
        private const string IDF_SWITCH_SCADA_P3_STATE = "p3State";
        private const string IDF_SWITCH_SCADA_S1_R_AMPS = "s1p1Amps";
        private const string IDF_SWITCH_SCADA_S1_Y_AMPS = "s1p2Amps";
        private const string IDF_SWITCH_SCADA_S1_B_AMPS = "s1p3Amps";
        private const string IDF_SWITCH_SCADA_S2_R_AMPS = "s2p1Amps";
        private const string IDF_SWITCH_SCADA_S2_Y_AMPS = "s2p2Amps";
        private const string IDF_SWITCH_SCADA_S2_B_AMPS = "s2p3Amps";
        private const string IDF_SWITCH_SCADA_S1_VREF = "s1VoltageReference";
        private const string IDF_SWITCH_SCADA_S1_VTYPE = "s1VoltageType";
        private const string IDF_SWITCH_SCADA_S2_VREF = "s2VoltageReference";
        private const string IDF_SWITCH_SCADA_S2_VTYPE = "s2VoltageType";

        private const string IDF_SWITCH_SCADA_S1_RY_VOLTS = "s1p1KV";
        private const string IDF_SWITCH_SCADA_S1_YB_VOLTS = "s1p2KV";
        private const string IDF_SWITCH_SCADA_S1_BR_VOLTS = "s1p3KV";
        private const string IDF_SWITCH_SCADA_S2_RY_VOLTS = "s2p1KV";
        private const string IDF_SWITCH_SCADA_S2_YB_VOLTS = "s2p2KV";
        private const string IDF_SWITCH_SCADA_S2_BR_VOLTS = "s2p3KV";

        private const double IDF_SCALE_GEOGRAPHIC = 1.0;
        private const double IDF_SCALE_INTERNALS = 0.1;
        private const double IDF_SWITCH_Z = double.NaN;
        #endregion
     
        //temporary fields from GIS
        //private string T1Id = "";
        private string _gisswitchtype = "";
        private string _fuserating = "";
        
        //fields that should be set and validated by this class
        private string _baseKv = "";//GIS will set this to the operating voltage
        private string _bidirectional = IDF_TRUE;
        private string _forwardTripAmps = "";
        private string _ganged = IDF_FALSE;
        private string _loadBreakCapable = IDF_FALSE;
        private string _maxInterruptAmps = "";
        private string _ratedAmps = "";
        private string _ratedKv = "";
        private string _reverseTripAmps = "";
        private string _switchType = "";
        private string _nominalUpstreamSide = "";

        internal Switch(XElement node, Group processor) : base(node, processor) { }
        
        internal override void Process()
        {
            try
            {
                ParentGroup.AddMissingPhases(Node);

                var geo = ParentGroup.GetSymbolGeometry(Id);

                if (Node.Attribute(GIS_T1_ASSET)!= null)
                    T1Id = Node.Attribute(GIS_T1_ASSET).Value;
                if (Node.Attribute(GIS_SWITCH_TYPE) != null)
                    _gisswitchtype = Node.Attribute(GIS_SWITCH_TYPE).Value;
                if (Node.Attribute(GIS_FUSE_RATING)!= null)
                    _fuserating = Node.Attribute(GIS_FUSE_RATING).Value;
                if (Node.Attribute(IDF_SWITCH_BIDIRECTIONAL) != null)
                    _bidirectional = Node.Attribute(IDF_SWITCH_BIDIRECTIONAL).Value;
                if (Node.Attribute(IDF_SWITCH_FORWARDTRIPAMPS) != null)
                    _forwardTripAmps = Node.Attribute(IDF_SWITCH_FORWARDTRIPAMPS).Value;
                if (Node.Attribute(IDF_SWITCH_REVERSETRIPAMPS) != null)
                    _reverseTripAmps = Node.Attribute(IDF_SWITCH_REVERSETRIPAMPS).Value;
                if (Node.Attribute(IDF_SWITCH_GANGED) != null)
                    _ganged = Node.Attribute(IDF_SWITCH_GANGED).Value;
                if (Node.Attribute(IDF_SWITCH_LOADBREAKCAPABLE) != null)
                    _loadBreakCapable = Node.Attribute(IDF_SWITCH_LOADBREAKCAPABLE).Value;
                if (Node.Attribute(IDF_SWITCH_MAXINTERRUPTAMPS) != null)
                    _maxInterruptAmps = Node.Attribute(IDF_SWITCH_MAXINTERRUPTAMPS).Value;
                if (Node.Attribute(IDF_SWITCH_RATEDAMPS) != null)
                    _ratedAmps = Node.Attribute(IDF_SWITCH_RATEDAMPS).Value;
                if (Node.Attribute(IDF_SWITCH_RATEDKV) != null)
                    _ratedKv = Node.Attribute(IDF_SWITCH_RATEDKV).Value;
                
                //TODO: backport into GIS
                //TODO: IDF bug?
                Node.SetAttributeValue("inSubstation", null);

                _switchType = Node.Attribute(IDF_SWITCH_SWITCHTYPE).Value;

                if (Node.Attribute("baseKV").Value == "0.2300")
                {
                    Node.SetAttributeValue("baseKV", "0.4000");
                    Debug("Overriding base voltage from 230V to 400V");
                }
                _baseKv = Node.Attribute(IDF_SWITCH_BASEKV).Value;

                switch (_gisswitchtype)
                {
                    //MV Isolator
                    case @"MV Isolator\Knife Isolator":
                        //basically a non ganged disconnector e.g. links
                        ProcessDisconnector(true);
                        _ganged = IDF_FALSE;
                        _loadBreakCapable = IDF_FALSE;
                        break;
                    case @"MV Isolator\HV Fuse":
                        ProcessHVFuse(true);
                        break;
                    case @"MV Isolator\Circuit Breaker - Substation Feeder":
                        ProcessCircuitBreaker(true);
                        break;
                    case @"MV Isolator\Circuit Breaker - Substation General":
                        ProcessCircuitBreaker2(true);
                        break;
                    case @"MV Isolator\Air Break Switch":
                        ProcessDisconnector(true);
                        break;
                    case @"MV Isolator\MV Switch":
                        Warn("Not sure what to do with switch type [MV Isolator\\MV Switch]");
                        break;
                    case @"MV Isolator\Earth Switch":

                        break;
                    case @"MV Isolator\Disconnector":
                        ProcessDisconnector(true);
                        break;
                    case @"MV Isolator\HV Fuse Switch":
                        ProcessRingMainFuseSwitch();
                        break;
                    //MV Line Switch
                    case @"MV Line Switch\Circuit Breaker - Line":
                        ProcessCircuitBreaker(false);
                        break;
                    case @"MV Line Switch\HV Link":
                        ProcessHVLinks(false);
                        break;
                    case @"MV Line Switch\HV Fuse":
                        ProcessHVFuse(false);
                        break;
                    case @"MV Line Switch\Automated LBS":
                        ProcessEntec();
                        break;
                    case @"MV Line Switch\Disconnector":
                        ProcessDisconnector(false);
                        break;
                    case @"MV Line Switch\Fuse Saver":
                        ProcessFuseSaver();
                        break;
                    case @"MV Line Switch\MV Gas Switch":
                        //TODO
                        break;
                    case @"MV Line Switch\HV Tri - Fuse":
                        ProcessHVFuse(false);
                        break;
                    //LV Line Switch
                    case @"LV Line Switch\OH LV Open Point":
                    case @"LV Isolator\LV Circuit Breaker"://TODO
                        ProcessLVSwitch();
                        break;
                    case @"LV Line Switch\LV Fuse": //pole fuse
                    case @"LV Isolator\LV Fuse"://TODO //indoor panel
                    case @"LV Isolator\LV Switch Fuse"://TODO //indoor panel
                    case @"LV Isolator\LV Switch Link"://TODO //indoor pabel
                    case @"LV Isolator\LV Link"://TODO //pole fuse link
                    case @"LV Isolator\LV Direct Connector": //direct connection
                    case @"LV Isolator\Streetlight - Relay":
                        ProcessLVFuse();
                        break;
                    //Others
                    case @"Service Fuse":
                        ProcessServiceFuse();
                        break;
                    case "":
                        Error($"Gis Switch Type is not set");
                        break;
                    default:
                        Error($"Unrecognised GisSwitchType [{_gisswitchtype}]");
                        break;
                }
                
                Node.SetAttributeValue(IDF_SWITCH_BASEKV, _baseKv);
                Node.SetAttributeValue(IDF_SWITCH_BIDIRECTIONAL, _bidirectional);
                Node.SetAttributeValue(IDF_SWITCH_FORWARDTRIPAMPS, _forwardTripAmps);
                Node.SetAttributeValue(IDF_SWITCH_GANGED, _ganged);
                Node.SetAttributeValue(IDF_SWITCH_LOADBREAKCAPABLE, _loadBreakCapable);
                Node.SetAttributeValue(IDF_SWITCH_MAXINTERRUPTAMPS, _maxInterruptAmps);
                Node.SetAttributeValue(IDF_SWITCH_RATEDAMPS, _ratedAmps);
                Node.SetAttributeValue(IDF_SWITCH_RATEDKV, _ratedKv);
                Node.SetAttributeValue(IDF_SWITCH_REVERSETRIPAMPS, _reverseTripAmps);
                Node.SetAttributeValue(IDF_SWITCH_SWITCHTYPE, _switchType);
                if (!string.IsNullOrWhiteSpace(_nominalUpstreamSide))
                    Node.SetAttributeValue(IDF_SWITCH_NOMINALUPSTREAMSIDE, _nominalUpstreamSide);

                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                var scada = GenerateScadaLinking();
                if (scada.Item2 != null && !string.IsNullOrWhiteSpace(scada.Item1))
                {
                    ParentGroup.AddGroupElement(scada.Item2);
                    ParentGroup.AddScadaCommand(Id, scada.Item1);
                }
                RemoveExtraAttributes();

                Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Switch, geo);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception in {nameof(Process)}: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GIS_FUSE_RATING, null);
            Node.SetAttributeValue(GIS_SWITCH_TYPE, null);
            Node.SetAttributeValue(GIS_T1_ASSET, null);
        }

        private (string,XElement) GenerateScadaLinking()
        {
            //if (Name == "P45")
            //    Debugger.Break();
            bool hasVolts = false;

            var status = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            
            //if we don't have the switch status, then assume we don't have any other telemtry either
            if (status == null)
                return ("", null);
            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            x.SetAttributeValue("p1State", status.Key);
            x.SetAttributeValue("p2State", status.Key);
            x.SetAttributeValue("p3State", status.Key);

            //we can't assign any of this telemtry without knowing what the upstream side of the switch is
            if (_nominalUpstreamSide == "1" || _nominalUpstreamSide == "2")
            {
                //set the upstream and downstream nodes
                string us = _nominalUpstreamSide;
                string ds = us == "1" ? "2" : "1";

                var rAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps RØ");
                if (rAmps != null)
                {
                    x.SetAttributeValue($"s{us}p1Amps", rAmps.Key);
                    x.SetAttributeValue($"s{us}p1AmpsUCF", "1");
                }
                var yAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps YØ");
                if (yAmps != null)
                {
                    x.SetAttributeValue($"s{us}p2Amps", yAmps.Key);
                    x.SetAttributeValue($"s{us}p2AmpsUCF", "1");
                }
                var bAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps BØ");
                if (bAmps != null)
                {
                    x.SetAttributeValue($"s{us}p3Amps", bAmps.Key);
                    x.SetAttributeValue($"s{us}p3AmpsUCF", "1");
                }

                var kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} kW");
                if (kw != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1");

                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                    x.SetAttributeValue($"s{ds}AggregateKWUCF", "");
                }
                else if ((kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} MW")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1000");
                }

                var kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} kVar");
                if (kvar != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1");

                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                    x.SetAttributeValue($"s{ds}AggregateKVARUCF", "");
                }
                else if ((kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} MVar")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1000");
                }
                /*
                var pf = Enricher.Singleton.GetScadaAnalogPointInfo($"{Name} PF");

                if (bAmps != null)
                {
                    x.SetAttributeValue("s1p3Amps", bAmps.Key);
                    x.SetAttributeValue("s1p3AmpsUCF", "1");
                }
                */
                var s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts R");
                if (s1RYVolts != null)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts Y");
                if (s1YBVolts != null)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts B");
                if (s1BRVolts != null)
                {
                    x.SetAttributeValue($"s{us}p3KV", bAmps.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts R2");
                if (s2RYVolts != null)
                {
                    x.SetAttributeValue($"s{ds}p1KV", s2RYVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts Y2");
                if (s2YBVolts != null)
                {
                    x.SetAttributeValue($"s{ds}p2KV", s2YBVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts B2");
                if (s2BRVolts != null)
                {
                    x.SetAttributeValue($"s{ds}p3KV", s2BRVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
            }
            if (hasVolts)
            {
                //TODO what does this do again?
                x.SetAttributeValue("s1VoltageReference", _baseKv);
            }

            return (status.Key, x);
                
        }

        #region Switch Type Processing
        
        private void ProcessCircuitBreaker2(bool internals)
        {
            if (!string.IsNullOrEmpty(T1Id)) {
                DataType asset = DataManager.I.RequestRecordById<T1HvCircuitBreaker>(T1Id);
                if (asset != null)
                {
                    ProcessCircuitBreaker(internals);
                    return;
                }
                asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);
                if (asset != null)
                {
                    ProcessRingMainCb();
                    return;
                }
                Error($"T1 asset number [{T1Id}] did not match HV Breaker or RMU asset");
            }
            else
            {
                Error("T1 asset number not set");
            }

            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;

            ProcessCircuitBreakerAdms();

            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER_QUAD, scale, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, scale, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, scale, double.NaN, IDF_SWITCH_Z);
            }
        }

        private void ProcessRingMainFuseSwitch()
        {
            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH; //TODO

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);

                if (asset != null)
                {
                    //TODO: validate  operating voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RMU_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_HVFUSESWITCH_QUAD, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_HVFUSESWITCH, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_HVFUSESWITCH, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
        }

        private void ProcessRingMainSwitch()
        {
            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH;
            
            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);

                if (asset != null)
                {
                    //TODO: validate operating voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    //_maxInterruptAmps = "";
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RMU_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_SWITCH_QUAD, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_SWITCH, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_SWITCH, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
        }
        
        private void ProcessRingMainCb()
        {
            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;

            //TODO: generally these won't be in the protection spreadsheet, will have generic settings
            //TODO: how to link with the tx size?
            ProcessCircuitBreakerAdms();

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);

                if (asset != null)
                {
                    //TODO: validate operating voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string, true);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RMU_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER_QUAD, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, IDF_SCALE_INTERNALS, double.NaN, IDF_SWITCH_Z);
            }
        }

        private void ProcessLVSwitch()
        {
            if (!string.IsNullOrEmpty(T1Id))
                Warn($"T1 asset number [{T1Id}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = IDF_FALSE; //TODO check
            _loadBreakCapable = IDF_TRUE; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IDF_SWITCH_TYPE_SWITCH;
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LVSWITCH, double.NaN, IDF_SWITCH_Z);
        }

        private void ProcessLVFuse()
        {
            if (!string.IsNullOrEmpty(T1Id))
                Warn($"T1 asset number [{T1Id}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = IDF_FALSE; //TODO check
            _loadBreakCapable = IDF_TRUE; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IDF_SWITCH_TYPE_FUSE;
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LVFUSE, double.NaN, IDF_SWITCH_Z);
        }

        private void ProcessFuseSaver()
        {
            //TODO: need a way to link a fuse saver to the corresponding fuse
            //TODO: consider cases where there is no fuse involved?

            /*
            _forwardTripAmps = "";
            _reverseTripAmps = "";
            */

            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;

            //ProcessCircuitBreakerAdms();

            _switchType = IDF_SWITCH_TYPE_RECLOSER;
            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1HvCircuitBreaker>(T1Id);

                if (asset != null)
                {
                    //TODO: validate rated voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_HVCB_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_FUSESAVER, double.NaN, IDF_SWITCH_Z);
        }

        private void ProcessEntec()
        {
            //_bidirectional //TODO
            //TODO: need to get sectionliser function from AdmsDatabase
            _ganged = IDF_TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (asset != null)
                {
                    //TODO: validate op voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_DISCO_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_ENTEC_QUAD, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_ENTEC, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_ENTEC, double.NaN, IDF_SWITCH_Z);
            }
        }

        private void ProcessHVLinks(bool internals)
        {
            _ganged = IDF_FALSE;
            _loadBreakCapable = IDF_FALSE;//TODO
            _ratedAmps = "300";//confirmed by robert
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Fuse>(T1Id);

                if (asset == null)
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }
                else
                {
                    //TODO: validate op voltage
                    //TODO: rated voltage always null here
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                    ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_FUSE_RATED_VOLTAGE] as string);
                }
            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LINKS, scale, double.NaN, IDF_SWITCH_Z);
        }

        private void ProcessDisconnector(bool internals)
        {
            _ganged = IDF_TRUE;          
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (asset != null)
                {
                    //TODO: validate rated voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_DISCO_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_DISCONNECTOR_QUAD, scale, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_DISCONNECTOR, scale, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_DISCONNECTOR, scale, double.NaN, IDF_SWITCH_Z);
            }
        }
     
        private void ProcessCircuitBreaker(bool internals)
        {
            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;

            ProcessCircuitBreakerAdms();

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1HvCircuitBreaker>(T1Id);

                if (asset != null)
                {
                    //TODO: validate op voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_HVCB_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }

            var scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            if (p != null)
            {
                if (p.QuadState)
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER_QUAD, scale, double.NaN, IDF_SWITCH_Z);
                else
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, scale, double.NaN, IDF_SWITCH_Z);
            }
            else
            {
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CIRCUITBREAKER, scale, double.NaN, IDF_SWITCH_Z);
            }
        }

        private void ProcessCircuitBreakerAdms()
        {
            if (DataManager.I.RequestRecordById<AdmsSwitch>(Name) is DataType asset)
            {
                //TODO: RMU circuit breakers are generally not in the protection database... they use generic settings based on tx size.
                //how are we going to handle these?
                //TODO: validation on these?
                //if (_name == "P45")
                //    Debugger.Break();
                _nominalUpstreamSide = asset[ADMS_SWITCH_NOMINALUPSTREAMSIDE];
                _forwardTripAmps = asset[ADMS_SWITCH_FORWARDTRIPAMPS];
                _reverseTripAmps = asset[ADMS_SWITCH_REVERSETRIPAMPS];
                _switchType = asset[ADMS_SWITCH_RECLOSER_ENABLED] as string == "Y" ? IDF_SWITCH_TYPE_RECLOSER : IDF_SWITCH_TYPE_BREAKER;
            }
            else
            {
                Warn("Breaker not in Adms database");
                _switchType = IDF_SWITCH_TYPE_BREAKER;
            }
        }

        private void ProcessHVFuse(bool internals)
        {
            _bidirectional = IDF_TRUE;
            _forwardTripAmps = _reverseTripAmps = ValidateFuseTrip(_fuserating);
            _ganged = IDF_FALSE;
            _loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "10000";//TODO check with sjw
            _ratedAmps = _forwardTripAmps == "" ? "" : (int.Parse(_forwardTripAmps) / 2).ToString();
            _switchType = IDF_SWITCH_TYPE_FUSE;

            DataType asset = null;
            if (T1Id == "")
            {
                Error($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Fuse>(T1Id);

                if (asset != null)
                {
                    if (asset[T1_FUSE_GANGED] as string == "2")
                    {
                        _ganged = IDF_TRUE;
                    }
                    //TODO: fuse not have rated voltage?
                    //TODO: validate op voltage
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_FUSE_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error($"T1 asset number [{T1Id}] wasn't in T1");
                }
            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_FUSE, scale, double.NaN, IDF_SWITCH_Z);
        }

        private void ProcessServiceFuse()
        {
            _bidirectional = IDF_TRUE;
            _forwardTripAmps = _reverseTripAmps = "";
            _ganged = IDF_FALSE;
            _loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "";//TODO check with sjw
            _ratedAmps = "";//TODO?
            _switchType = "Fuse";
            _ratedKv = ValidateRatedVoltage(_baseKv, _baseKv, 1);
            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_SERVICE_FUSE, double.NaN, IDF_SWITCH_Z);
        }
        #endregion

        #region Validation Routines
        /// <summary>
        /// Checks the rated voltage is greater than the operating voltage, and returns the validated rated voltage
        /// </summary>
        /// <param name="opVoltage"></param>
        /// <param name="ratedVoltage"></param>
        /// <returns></returns>
        private string ValidateRatedVoltage(string opVoltage, string ratedVoltage, float ratedScale = 1000)
        {
            //TODO voltages should be line to line, but what about single phase?
            try
            {
                var iOpVoltage = float.Parse(opVoltage);
                if (string.IsNullOrEmpty(ratedVoltage))
                    return (iOpVoltage * 1.1).ToString();


                if (float.TryParse(ratedVoltage, out var iNewValue))
                {
                    iNewValue /= ratedScale;

                    if (iNewValue > iOpVoltage)
                    {
                        return iNewValue.ToString();
                    }
                    else if (iNewValue == iOpVoltage)
                    {
                        Info($"Rated voltage [{iNewValue}] == operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                    else
                    {
                        Error($"Rated voltage [{iNewValue}] is less than operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                }
                else
                {
                    Error($"Could not parse rated voltage [{ratedVoltage}], setting to 110% of operating voltage");
                }
                return (iOpVoltage * 1.1).ToString();
            }
            catch
            {
                Error($"Operating voltage [{opVoltage}] is not a valid float");
                return opVoltage;
            }
        }

        private string ValidateLoadBreakRating(string amps)
        {
            if (string.IsNullOrEmpty(amps))
            {
                Info("T1 load break rating is unset");
                return "";
            }
            if (int.TryParse(amps, out var res))
            {
                return res.ToString();
            }
            else
            {
                Warn("Couldn't parse T1 load break rating");
                return "";
            }
        }

        private string ValidateMaxInterruptAmps(string amps)
        {
            if (string.IsNullOrEmpty(amps))
            {
                Info($"T1 max interrupt amps is unset");
                return "";
            }
            Regex r = new Regex("[0-9]+(?=kA)");
            var match = r.Match(amps);
            if (match.Success)
            {
                return (int.Parse(match.Value) * 1000).ToString();
            }
            else
            {
                Warn( $"Could not parse T1 max interrupt amps [{amps}]");
                return "";
            }
        }

        private void ValidateSwitchNumber(string swno, params string[] swnos)
        {
            if (swno == Name)
                return;
            if (swnos != null)
            {
                if (swnos.Length > 0)
                {
                    foreach (var sw in swnos)
                    {
                        if (sw == Name)
                            return;
                    }
                    Warn( $"T1 switch number [{swno}:{string.Join(":", swnos)}] doesn't match GIS switch number [{Name}]");
                    return;
                }
            }
            Warn($"T1 switch number [{swno}] doesn't match GIS switch number [{Name}]");

        }

        private string ValidatedRatedAmps(string amps, bool breaker = false)
        {
            //format of rated amps is either an integer, or two integers i1/i2 for breaker/switch ratings of RMUs

            //handle the simple case
            if (string.IsNullOrEmpty(amps))
            {
                Info("T1 rated amps is unset");
                return "";
            }
            if (int.TryParse(amps, out var res))
            {
                return amps;
            }
            //handle the dual rating case
            else
            {
                var arr = amps.Split(new char[] { '/' });
                if (arr.Length == 2)
                {
                    if (breaker)
                    {
                        if (int.TryParse(arr[0], out res))
                        {
                            return arr[0];
                        }
                        else if (int.TryParse(arr[1], out res))
                        {
                            return arr[1];
                        }
                    }
                }
                Warn($"Could not parse T1 rated amps [{amps}]");
                return "";
            }
        }

        /// <summary>
        /// Parses the fuse rating, and returns the trip value in amps
        /// Fuse pickups are typically twice the value of the fuse rating.
        /// </summary>
        /// <param name="fuserating"></param>
        /// <returns></returns>
        private string ValidateFuseTrip(string fuserating)
        {
            if (fuserating == "")
            {
                Info( "GIS fuse rating is unset");
                return "";
            }
            Regex r = new Regex("[0-9]+(?=(T|K))");
            var match = r.Match(fuserating);
            if (match.Success)
            {
                return (int.Parse(match.Value) *2).ToString();
            }
            else
            {
                Warn( $"Could not parse GIS fuse rating [{fuserating}]");
                return "";
            }
        }
        #endregion

    }
}
