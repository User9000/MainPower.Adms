using System;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    class IdfSwitch : IdfElement
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
        private const string SYMBOL_EARTH_SWITCH = "Symbol 4";

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

        private const double IDF_SCALE_GEOGRAPHIC = 2.0;
        private const double IDF_SCALE_INTERNALS = 0.2;
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
        private string _ganged = IDF_TRUE;
        private string _loadBreakCapable = IDF_TRUE;
        private string _maxInterruptAmps = "";
        private string _ratedAmps = "";
        private string _ratedKv = "";
        private string _reverseTripAmps = "";
        private string _switchType = "";
        private string _nominalUpstreamSide = "";

        //others
        private string _symbol = SYMBOL_SWITCH;
        private DataType _t1Asset = null;
        private DataType _admsAsset = null;

        public IdfSwitch(XElement node, IdfGroup processor) : base(node, processor) { }
        
        public override void Process()
        {
            try
            {
                CheckPhases();

                var geo = ParentGroup.GetSymbolGeometry(Id);

                T1Id = Node.Attribute(GIS_T1_ASSET)?.Value;
                _gisswitchtype = Node.Attribute(GIS_SWITCH_TYPE)?.Value ?? "";
                _fuserating = Node.Attribute(GIS_FUSE_RATING)?.Value;
                _baseKv = Node.Attribute(IDF_SWITCH_BASEKV).Value;
                //not sure this is required
                _switchType = Node.Attribute(IDF_SWITCH_SWITCHTYPE).Value;

                switch (_gisswitchtype)
                {
                    //MV Isolator
                    case @"MV Isolator\Knife Isolator":
                        //basically a non ganged disconnector e.g. links
                        ProcessDisconnector();
                        _ganged = IDF_FALSE;
                        //TODO
                        //_loadBreakCapable = IDF_FALSE;
                        break;
                    //TODO: this could be a ring main switch?
                    case @"MV Isolator\MV Switch":
                    case @"MV Isolator\Air Break Switch":
                    case @"MV Isolator\Disconnector":
                    case @"MV Line Switch\Disconnector":
                    case @"MV Line Switch\MV Gas Switch":
                        ProcessDisconnector();
                        break;

                    case @"MV Isolator\Circuit Breaker - Substation Feeder":
                    case @"MV Isolator\Circuit Breaker - Substation General":
                    case @"MV Isolator\Circuit Breaker - Line":
                    case @"MV Line Switch\Recloser":
                    case @"MV Line Switch\Circuit Breaker - Line":
                        ProcessCircuitBreaker();
                        break;
                    case @"MV Isolator\Earth Switch":
                        //ProcessEarthSwitch();
                        _symbol = SYMBOL_EARTH_SWITCH;
                        break;
                    case @"MV Isolator\HV Fuse Switch":
                        ProcessRingMainFuseSwitch();
                        break;
                    case @"MV Isolator\HV Link":
                    case @"MV Line Switch\HV Link":
                        ProcessHVLinks();
                        break;
                    case @"MV Line Switch\HV Fuse":
                    case @"MV Isolator\HV Fuse":
                    case @"MV Line Switch\HV Tri - Fuse":
                        ProcessHVFuse();
                        break;
                    case @"MV Line Switch\Sectionaliser"://TODO: should process this one differently
                    case @"MV Line Switch\Automated LBS":
                        ProcessEntec();
                        break;
                    case @"MV Line Switch\Fuse Saver":
                        ProcessFuseSaver();
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
                        Warn($"Gis Switch Type is not set");
                        break;
                    default:
                        Warn($"Unrecognised GisSwitchType [{_gisswitchtype}]");
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
                
                //need to do this before the SCADA linking otherwise the datalink will be replaced
                ParentGroup.SetSymbolNameByDataLink(Id, _symbol, IDF_SCALE_GEOGRAPHIC, IDF_SCALE_INTERNALS);

                //TODO tidy this up
                var scada = GenerateScadaLinking();
                if (scada.Item2 != null && !string.IsNullOrWhiteSpace(scada.Item1))
                {
                    ParentGroup.CreateDataLinkSymbol(Id);
                    ParentGroup.AddGroupElement(scada.Item2);
                    ParentGroup.AddScadaCommand(Id, scada.Item1);
                }
                if (_baseKv != "0.4")
                    GenerateDeviceInfo();
                RemoveExtraAttributes();

                Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Switch, geo);
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
            //TODO this assumption needs to be documented
            if (status == null)
                return ("", null);
            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            if (status["Type"] == "T_I&C")
                x.SetAttributeValue("gangedControlPoint", status.Key);

            bool phase1, phase2, phase3;

            phase1 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value);
            phase2 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value);
            phase3 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value);

            if (phase1)
                x.SetAttributeValue("p1State", status.Key);
            if (phase2)
                x.SetAttributeValue("p2State", status.Key);
            if (phase3)
                x.SetAttributeValue("p3State", status.Key);

            //we can't assign any of this telemtry without knowing what the upstream side of the switch is
            //TODO emit a warning if not set
            //TODO if upstream side changes we need to run enricher again to get the change?
            if (_nominalUpstreamSide == "1" || _nominalUpstreamSide == "2")
            {
                //set the upstream and downstream nodes
                string us = _nominalUpstreamSide;
                string ds = us == "1" ? "2" : "1";

                var rAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps RØ");
                if (rAmps != null && phase1)
                {                        
                    x.SetAttributeValue($"s{us}p1Amps", rAmps.Key);
                    x.SetAttributeValue($"s{us}p1AmpsUCF", "1");
                }
                var yAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps YØ");
                if (yAmps != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2Amps", yAmps.Key);
                    x.SetAttributeValue($"s{us}p2AmpsUCF", "1");
                }
                var bAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Amps BØ");
                if (bAmps != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3Amps", bAmps.Key);
                    x.SetAttributeValue($"s{us}p3AmpsUCF", "1");
                }

                //when it comes to load telemetry, priority goes
                //1. Metering
                //2. Local kW/MW
                var kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Met MW");
                if (kw != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }
                else if ((kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} kW")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }
                else if ((kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} MW")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }

                var kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Met Mvar");
                if (kvar != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                }
                else if ((kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} kvar")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                }
                else if ((kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Mvar")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
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
                if (s1RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts Y");
                if (s1YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts B");
                if (s1BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", bAmps.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts R2");
                if (s2RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{ds}p1KV", s2RYVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts Y2");
                if (s2YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{ds}p2KV", s2YBVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Volts B2");
                if (s2BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{ds}p3KV", s2BRVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }

                var lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Lockout");
                if (lockout == null)
                    lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot Trip4 OC");
                if (lockout != null)
                {
                    if (phase1)
                        x.SetAttributeValue("p1TripFaultSignal", lockout.Key);
                    if (phase2)
                        x.SetAttributeValue("p2TripFaultSignal", lockout.Key);
                    if (phase3)
                        x.SetAttributeValue("p3TripFaultSignal", lockout.Key);
                }

                var ampsr = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Prot Trip Amps RØ");
                if (ampsr != null && phase1)
                {
                    x.SetAttributeValue("p1FaultCurrent", ampsr.Key);
                }
                var ampsy = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Prot Trip Amps YØ");
                if (ampsy != null && phase3)
                {
                    x.SetAttributeValue("p2FaultCurrent", ampsy.Key);
                }
                var ampsb = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(SCADA_NAME, $"{Name} Prot Trip Amps BØ");
                if (ampsb != null && phase3)
                {
                    x.SetAttributeValue("p3FaultCurrent", ampsb.Key);
                }

                //TODO: handle cases where there are two relays?
                var watchdog = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Relay Watchdog");

                //p1OCPMode = Watchdog 
                //OCPModeNormal = 1
                //p1OCPMode = Watchdog 
                //p1FaultCurrent = fault current
                //p1TripFaultSignal = lockout

                //p1FaultInd = for directional devices is the side 1 fault indication, otherwise non directional
                //p1FaultInd2 = for directional devices is the side 2 fault indication, otherwise non directional

                var p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot RØ Fault");
                if (p1Fault != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }
                else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot OC RØ Trip")) != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }
                else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot Trip1 OC")) != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }

                var p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot YØ Fault");
                if (p2Fault != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }
                else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot OC YØ Trip")) != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }
                else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot Trip2 OC")) != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }

                var p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot RØ Fault");
                if (p3Fault != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }
                else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot OC RØ Trip")) != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }
                else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot Trip3 OC")) != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }


                var hlt = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} WorkTag");
                if (hlt != null)
                    x.SetAttributeValue("hotLineTag", hlt.Key);

                var groundTripBlock = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Prot SEF");
                if (groundTripBlock != null)
                    x.SetAttributeValue("groundTripBlock", groundTripBlock.Key);

                var acr = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, $"{Name} Auto Reclose");
                if (acr != null)
                    x.SetAttributeValue("reclosing", acr.Key);

                x.SetAttributeValue("faultIndType", "Directionless");


            }
            if (hasVolts)
            {
                //TODO what does this do again?
                x.SetAttributeValue("s1VoltageReference", _baseKv);
            }

            return (status.Key, x);
                
        }

#region Switch Type Processing
        
    
        private void SetSymbol(OsiScadaStatus p, string normal, string quad)
        {
            if (p != null)
            {
                if (p.QuadState)
                    _symbol = quad;
                else
                    _symbol = normal;
            }
            else
            {
                _symbol = normal;
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
                Warn($"No T1 asset number assigned");
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
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            SetSymbol(p, SYMBOL_HVFUSESWITCH, SYMBOL_HVFUSESWITCH_QUAD);
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
                Warn($"No T1 asset number assigned");
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
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            SetSymbol(p, SYMBOL_SWITCH, SYMBOL_SWITCH_QUAD);
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
            _symbol = SYMBOL_LVSWITCH;
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
            _symbol = SYMBOL_LVFUSE;
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
                Warn($"No T1 asset number assigned");
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
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            _symbol = SYMBOL_FUSESAVER;
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
                Warn($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (asset != null)
                {
                    //TODO: validate op voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    //TODO
                    //_loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_DISCO_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            SetSymbol(p, SYMBOL_ENTEC, SYMBOL_ENTEC_QUAD);
        }

        private void ProcessHVLinks()
        {
            _ganged = IDF_FALSE;
            //TODO
            //_loadBreakCapable = IDF_FALSE;//TODO
            _ratedAmps = "300";//confirmed by robert
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataType asset = null;
            if (T1Id == "")
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Fuse>(T1Id);

                if (asset == null)
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }
                else
                {
                    //TODO: validate op voltage
                    //TODO: rated voltage always null here
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                    ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_FUSE_RATED_VOLTAGE] as string);
                }
            }
            
            _symbol = SYMBOL_LINKS;
        }

        private void ProcessDisconnector()
        {
            _ganged = IDF_TRUE;          
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataType asset = null;
            if (T1Id == "")
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (asset != null)
                {
                    //TODO: validate rated voltage
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    //TODO
                    //_loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_DISCO_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    _t1Asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);
                    if (_t1Asset != null)
                    {
                    }
                    else
                    {
                        Warn($"T1 asset number [{T1Id}] wasn't in T1");
                    }
                }

            }
            
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            SetSymbol(p, SYMBOL_DISCONNECTOR, SYMBOL_DISCONNECTOR_QUAD);
        }
        private void ProcessCircuitBreaker()
        {

            _bidirectional = IDF_TRUE;
            _ganged = IDF_TRUE;
            _loadBreakCapable = IDF_TRUE;

            if (!string.IsNullOrEmpty(T1Id))
            {
                _t1Asset = DataManager.I.RequestRecordById<T1HvCircuitBreaker>(T1Id);
                if (_t1Asset != null)
                {
                    ProcessT1CircuitBreaker();
                }
                else
                {
                    _t1Asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);
                    if (_t1Asset != null)
                    {
                        ProcessT1RingMainCb();
                    }
                    else
                    {
                        Warn($"T1 asset number [{T1Id}] did not match HV Breaker or RMU asset");
                    }
                }
            }
            else
            {
                Warn("T1 asset number not set");
            }

            ProcessCircuitBreakerAdms();

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(SCADA_NAME, Name);
            SetSymbol(p, SYMBOL_CIRCUITBREAKER, SYMBOL_CIRCUITBREAKER_QUAD);
        }

        private void ProcessT1CircuitBreaker()
        {

            if (_t1Asset != null)
            {
                //TODO: validate op voltage
                _ratedAmps = ValidatedRatedAmps(_t1Asset[T1_SWITCH_RATED_AMPS] as string);
                _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1_SWITCH_HVCB_RATED_VOLTAGE] as string);
                ValidateSwitchNumber(_t1Asset[T1_SWITCH_SWNUMBER] as string);
            }
        }
        private void ProcessT1RingMainCb()
        {
            if (_t1Asset != null)
            {
                //TODO: validate operating voltage
                _ratedAmps = ValidatedRatedAmps(_t1Asset[T1_SWITCH_RATED_AMPS] as string, true);
                _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1_SWITCH_RMU_RATED_VOLTAGE] as string);
                ValidateSwitchNumber(_t1Asset[T1_SWITCH_SW1] as string, _t1Asset[T1_SWITCH_SW2] as string, _t1Asset[T1_SWITCH_SW3] as string, _t1Asset[T1_SWITCH_SW4] as string);
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
        private void ProcessHVFuse()
        {
            _bidirectional = IDF_TRUE;
            _forwardTripAmps = _reverseTripAmps = ValidateFuseTrip(_fuserating);
            _ganged = IDF_FALSE;
            //TODO
            //_loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "10000";//TODO check with sjw
            _ratedAmps = _forwardTripAmps == "" ? "" : (int.Parse(_forwardTripAmps) / 2).ToString();
            _switchType = IDF_SWITCH_TYPE_FUSE;

            DataType asset = null;
            if (T1Id == "")
            {
                Warn($"No T1 asset number assigned");
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
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }
            }
            
         _symbol = SYMBOL_FUSE;
        }
        private void ProcessServiceFuse()
        {
            _bidirectional = IDF_TRUE;
            _forwardTripAmps = _reverseTripAmps = "";
            _ganged = IDF_FALSE;
            //TODO
            //_loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "";//TODO check with sjw
            _ratedAmps = "";//TODO?
            _switchType = "Fuse";
            _ratedKv = ValidateRatedVoltage(_baseKv, _baseKv, 1);
            _symbol = SYMBOL_SERVICE_FUSE;
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
                if (string.IsNullOrEmpty(ratedVoltage) || ratedVoltage == "99")
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
                        Debug($"Rated voltage [{iNewValue}] == operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                    else
                    {
                        Warn($"Rated voltage [{iNewValue}] is less than operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                }
                else
                {
                    Warn($"Could not parse rated voltage [{ratedVoltage}], setting to 110% of operating voltage");
                }
                return (iOpVoltage * 1.1).ToString();
            }
            catch
            {
                Warn($"Operating voltage [{opVoltage}] is not a valid float");
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
