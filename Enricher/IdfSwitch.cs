using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    class IdfSwitch : IdfElement
    {

        #region Constants
        private const string SymbolCircuitBreaker = "Symbol 0";
        private const string SymbolCircuitBreakerQuad = "Symbol 14";
        private const string SymbolSwitch = "Symbol 3";
        private const string SymbolSwitchQuad = "Symbol 15";
        private const string SymbolFuse = "Symbol 2";
        private const string SymbolServiceFuse = "Symbol 2";
        private const string SymbolLinks = "Symbol 9";
        private const string SymbolDisconnector = "Symbol 3";
        private const string SymbolDisconnectorQuad = "Symbol 15";
        private const string SymbolEntecQuad = "Symbol 16";
        private const string SymbolEntec = "Symbol 6";
        private const string SymbolFuseSaver = "Symbol 10";
        private const string SymbolLVSwitch = "Symbol 3";
        private const string SymbolLVFuse = "Symbol 17";
        private const string SymbolHVFuseSwitch = "Symbol 17";
        private const string SymbolHVFuseSwitchQuad = "Symbol 17";
        private const string SymbolEarthSwitch = "Symbol 4";

        private const string T1FuseGanged = "Is Tri Fuse";
        private const string T1SwitchNumber = "Switch Number";
        private const string T1SwitchRatedAmps = "Rated Current";
        private const string T1SwitchMaxInterruptAmps = "Fault kA/sec";
        private const string T1SwitchLoadBreakRating = "Load Break Rating";
        private const string T1RmuSwitchSw1 = "SW 1";
        private const string T1RmuSwitchSw2 = "SW 2";
        private const string T1RmuSwitchSw3 = "SW 3";
        private const string T1RmuSwitchSw4 = "SW4";

        private const string T1FuseRatedVoltage = "Rat#Volt-Do Fuse";
        //private const string T1FuseOperatingVoltage = "Op#Volt-Do Fuse";

        private const string T1HvcbRatedVoltage = "Op#Volt-HV CircuitBr";
        //private const string T1HvcbOperatingVoltage = "Rat#Volt-HV Circuit";

        private const string T1RmuRatedVoltage = "Op#Volt-RMU";
        //private const string T1RmuOperatingVoltage = "Rate#Volt-RMU";

        private const string T1DisconnectorRatedVoltage = "Op#Volt-Disconnector";
        //private const string T1DisconnectorOperatingVoltage = "Rat#Volt-Disconnector";

        private const string IdfSwitchBidirectional = "bidirectional";
        private const string IdfSwitchForwardTripAmps = "forwardTripAmps";
        private const string IdfSwitchReverseTripAmps = "reverseTripAmps";
        private const string IdfSwitchGanged = "ganged";
        private const string IdfSwitchLoadBreakCapable = "loadBreakCapable";
        private const string IdfSwitchMaxInterruptAmps = "maxInterruptAmps";
        private const string IdfSwitchRatedAmps = "ratedAmps";
        private const string IdfSwitchRatedkV = "ratedKV";
        private const string IdfSwitchBasekV = "baseKV";
        private const string IdfSwitchType= "switchType";
        private const string IdfSwitchNominalUpstreamSide = "nominalUpstreamSide";

        private const string IdfSwitchTypeFuse = "Fuse";
        private const string IdfSwitchTypeBreaker = "Breaker";
        private const string IdfSwitchTypeSwitch = "Switch";
        private const string IdfSwitchTypeRecloser = "Recloser";
        private const string IdfSwitchTypeSectionaliser = "Sectionaliser";
        private const string IdfSwitchFaultProtectionAttrs = "faultProtectionAttributes";
        private const string AdmsSwitchForwardTripAmps = "ForwardTripAmps";
        private const string AdmsSwitchReverseTripAmps = "ReverseTripAmps";
        private const string AdmsSwitchRecloserEnabled = "Recloser";
        private const string AdmsSwitchNominalUpstreamSide = "NominalUpstreamSide";
        private const string AdmsSwitchNotifyUpstreamSide = "NotifyUpstreamSide";
        private const string AdmsSwitchBlockFeederPropagation = "BlockFeederPropagation";

        private const string IdfSwitchScadaP1State = "p1State";
        private const string IdfSwitchScadaP2State = "p2State";
        private const string IdfSwitchScadaP3State = "p3State";
        private const string IdfSwitchScadaS1P1Amps = "s1p1Amps";
        private const string IdfSwitchScadaS1P2Amps = "s1p2Amps";
        private const string IdfSwitchScadaS1P3Amps = "s1p3Amps";
        private const string IdfSwitchScadaS2P1Amps = "s2p1Amps";
        private const string IdfSwitchScadaS2P2Amps = "s2p2Amps";
        private const string IdfSwitchScadaS2P3Amps = "s2p3Amps";
        private const string IdfSwitchScadaS1Vref = "s1VoltageReference";
        private const string IdfSwitchScadaS1Vtype = "s1VoltageType";
        private const string IdfSwitchScadaS2Vref = "s2VoltageReference";
        private const string IdfSwitchScadaS2Vtype = "s2VoltageType";

        private const string IdfSwitchScadaS1P1kV = "s1p1KV";
        private const string IdfSwitchScadaS1P2kV = "s1p2KV";
        private const string IdfSwitchScadaS1P3kV = "s1p3KV";
        private const string IdfSwitchScadaS2P1kV = "s2p1KV";
        private const string IdfSwitchScadaS2P2kV = "s2p2KV";
        private const string IdfSwitchScadaS2P3kV = "s2p3KV";

        private const double IdfScaleGeographic = 2.0;
        private const double IdfScaleInternals = 0.2;
        private const double IdfSwitchZ = double.NaN;
        #endregion
     
        //temporary fields from GIS
        private string _gisswitchtype = "";
        private string _fuserating = "";
        
        //fields that should be set and validated by this class
        private string _baseKv = "";//GIS will set this to the operating voltage
        private string _bidirectional = IdfTrue;
        private string _forwardTripAmps = "";
        private string _ganged = IdfTrue;
        private string _loadBreakCapable = IdfTrue;
        private string _maxInterruptAmps = "";
        private string _ratedAmps = "";
        private string _ratedKv = "";
        private string _reverseTripAmps = "";
        private string _switchType = "";
        private string _nominalUpstreamSide = "";
        private string _faultProtectionAttrs = "";

        //others
        private string _symbol = SymbolSwitch;
        private DataType _t1Asset = null;
        private DataType _admsAsset = null;

        public IdfSwitch(XElement node, IdfGroup processor) : base(node, processor) { }
        
        public override void Process()
        {
            try
            {
                CheckPhases();

                var geo = ParentGroup.GetSymbolGeometry(Id);

                T1Id = Node.Attribute(GisT1Asset)?.Value;
                _gisswitchtype = Node.Attribute(GisSwitchType)?.Value ?? "";
                _fuserating = Node.Attribute(GisFuseRating)?.Value;
                _baseKv = Node.Attribute(IdfSwitchBasekV).Value;
                //not sure this is required
                _switchType = Node.Attribute(IdfSwitchType).Value;

                switch (_gisswitchtype)
                {
                    //MV Isolator
                    case @"MV Isolator\Knife Isolator":
                        //basically a non ganged disconnector e.g. links
                        ProcessDisconnector();
                        _ganged = IdfFalse;
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
                        _symbol = SymbolEarthSwitch;
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
                
                Node.SetAttributeValue(IdfSwitchBasekV, _baseKv);
                Node.SetAttributeValue(IdfSwitchBidirectional, _bidirectional);
                Node.SetAttributeValue(IdfSwitchForwardTripAmps, _forwardTripAmps);
                Node.SetAttributeValue(IdfSwitchGanged, _ganged);
                Node.SetAttributeValue(IdfSwitchLoadBreakCapable, _loadBreakCapable);
                Node.SetAttributeValue(IdfSwitchMaxInterruptAmps, _maxInterruptAmps);
                Node.SetAttributeValue(IdfSwitchRatedAmps, _ratedAmps);
                Node.SetAttributeValue(IdfSwitchRatedkV, _ratedKv);
                Node.SetAttributeValue(IdfSwitchReverseTripAmps, _reverseTripAmps);
                Node.SetAttributeValue(IdfSwitchType, _switchType);

                if (!string.IsNullOrWhiteSpace(_nominalUpstreamSide))
                    Node.SetAttributeValue(IdfSwitchNominalUpstreamSide, _nominalUpstreamSide);
                if (!string.IsNullOrWhiteSpace(_faultProtectionAttrs))
                    Node.SetAttributeValue(IdfSwitchFaultProtectionAttrs, _faultProtectionAttrs);

                    //need to do this before the SCADA linking otherwise the datalink will be replaced
                    ParentGroup.SetSymbolNameByDataLink(Id, _symbol, IdfScaleGeographic, IdfScaleInternals);

                //TODO tidy this up
                var scada = GenerateScadaLinking();
                if (scada.Item2 != null && !string.IsNullOrWhiteSpace(scada.Item1))
                {
                    ParentGroup.CreateDataLinkSymbol(Id);
                    ParentGroup.AddGroupElement(scada.Item2);
                    ParentGroup.AddDatalinkToText(Id);
                    ParentGroup.AddScadaDatalink(Id, scada.Item1);
                }
                List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
                items.Add(new KeyValuePair<string, string>("GIS Switch Type", _gisswitchtype.Length > 39 ? _gisswitchtype.Substring(0,39): _gisswitchtype));
                GenerateDeviceInfo(items);
                RemoveExtraAttributes();

                Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Switch, geo.geometry, geo.internals);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception in {nameof(Process)}: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GisFuseRating, null);
            Node.SetAttributeValue(GisSwitchType, null);
            Node.SetAttributeValue(GisT1Asset, null);
        }

        private (string,XElement) GenerateScadaLinking()
        {
            bool hasVolts = false;

            var status = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            
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

                var rAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Amps RØ");
                if (rAmps != null && phase1)
                {                        
                    x.SetAttributeValue($"s{us}p1Amps", rAmps.Key);
                    x.SetAttributeValue($"s{us}p1AmpsUCF", "1");
                }
                var yAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Amps YØ");
                if (yAmps != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2Amps", yAmps.Key);
                    x.SetAttributeValue($"s{us}p2AmpsUCF", "1");
                }
                var bAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Amps BØ");
                if (bAmps != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3Amps", bAmps.Key);
                    x.SetAttributeValue($"s{us}p3AmpsUCF", "1");
                }

                //when it comes to load telemetry, priority goes
                //1. Metering
                //2. Local kW/MW
                var kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Met MW");
                if (kw != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }
                else if ((kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} kW")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }
                else if ((kw = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} MW")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKW", kw.Key);
                    x.SetAttributeValue($"s{us}AggregateKWUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }

                var kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Met Mvar");
                if (kvar != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1000");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                }
                else if ((kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} kvar")) != null)
                {
                    x.SetAttributeValue($"s{us}AggregateKVAR", kvar.Key);
                    x.SetAttributeValue($"s{us}AggregateKVARUCF", "1");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                }
                else if ((kvar = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Mvar")) != null)
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
                var s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts R");
                if (s1RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts Y");
                if (s1YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts B");
                if (s1BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", bAmps.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts R2");
                if (s2RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{ds}p1KV", s2RYVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts Y2");
                if (s2YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{ds}p2KV", s2YBVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }
                var s2BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Volts B2");
                if (s2BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{ds}p3KV", s2BRVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    hasVolts = true;
                }

                var lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Lockout");
                if (lockout == null)
                    lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot Trip4 OC");
                if (lockout != null)
                {
                    if (phase1)
                        x.SetAttributeValue("p1TripFaultSignal", lockout.Key);
                    if (phase2)
                        x.SetAttributeValue("p2TripFaultSignal", lockout.Key);
                    if (phase3)
                        x.SetAttributeValue("p3TripFaultSignal", lockout.Key);
                }

                var ampsr = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Prot Trip Amps RØ");
                if (ampsr != null && phase1)
                {
                    x.SetAttributeValue("p1FaultCurrent", ampsr.Key);
                }
                var ampsy = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Prot Trip Amps YØ");
                if (ampsy != null && phase3)
                {
                    x.SetAttributeValue("p2FaultCurrent", ampsy.Key);
                }
                var ampsb = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{Name} Prot Trip Amps BØ");
                if (ampsb != null && phase3)
                {
                    x.SetAttributeValue("p3FaultCurrent", ampsb.Key);
                }

                //TODO: handle cases where there are two relays?
                var watchdog = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Relay Watchdog");

                //p1OCPMode = Watchdog 
                //OCPModeNormal = 1
                //p1OCPMode = Watchdog 
                //p1FaultCurrent = fault current
                //p1TripFaultSignal = lockout

                //p1FaultInd = for directional devices is the side 1 fault indication, otherwise non directional
                //p1FaultInd2 = for directional devices is the side 2 fault indication, otherwise non directional

                var p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot RØ Fault");
                if (p1Fault != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }
                else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot OC RØ Trip")) != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }
                else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot Trip1 OC")) != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }

                var p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot YØ Fault");
                if (p2Fault != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }
                else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot OC YØ Trip")) != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }
                else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot Trip2 OC")) != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }

                var p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot RØ Fault");
                if (p3Fault != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }
                else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot OC RØ Trip")) != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }
                else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot Trip3 OC")) != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }


                var hlt = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} WorkTag");
                if (hlt != null)
                    x.SetAttributeValue("hotLineTag", hlt.Key);

                var groundTripBlock = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Prot SEF");
                if (groundTripBlock != null)
                    x.SetAttributeValue("groundTripBlock", groundTripBlock.Key);

                var acr = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{Name} Auto Reclose");
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
            _bidirectional = IdfTrue;
            _ganged = IdfTrue;
            _loadBreakCapable = IdfTrue;
            _switchType = IdfSwitchTypeSwitch; //TODO

            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);

                if (_t1Asset != null)
                {
                    //TODO: validate  operating voltage
                    _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1RmuRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1RmuSwitchSw1], _t1Asset[T1RmuSwitchSw2], _t1Asset[T1RmuSwitchSw3], _t1Asset[T1RmuSwitchSw4]);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            SetSymbol(p, SymbolHVFuseSwitch, SymbolHVFuseSwitchQuad);
        }

        private void ProcessRingMainSwitch()
        {
            _bidirectional = IdfTrue;
            _ganged = IdfTrue;
            _loadBreakCapable = IdfTrue;
            _switchType = IdfSwitchTypeSwitch;
            
            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1RingMainUnit>(T1Id);

                if (_t1Asset != null)
                {
                    //TODO: validate operating voltage
                    _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                    //_maxInterruptAmps = "";
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1RmuRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1RmuSwitchSw1], _t1Asset[T1RmuSwitchSw2], _t1Asset[T1RmuSwitchSw3], _t1Asset[T1RmuSwitchSw4]);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            SetSymbol(p, SymbolSwitch, SymbolSwitchQuad);
        }

        private void ProcessLVSwitch()
        {
            if (!string.IsNullOrEmpty(T1Id))
                Warn($"T1 asset number [{T1Id}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = IdfFalse; //TODO check
            _loadBreakCapable = IdfTrue; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IdfSwitchTypeSwitch;
            _symbol = SymbolLVSwitch;
        }

        private void ProcessLVFuse()
        {
            if (!string.IsNullOrEmpty(T1Id))
                Warn($"T1 asset number [{T1Id}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = IdfFalse; //TODO check
            _loadBreakCapable = IdfTrue; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IdfSwitchTypeFuse;
            _symbol = SymbolLVFuse;
        }

        private void ProcessFuseSaver()
        {
            //TODO: need a way to link a fuse saver to the corresponding fuse
            //TODO: consider cases where there is no fuse involved?

            /*
            _forwardTripAmps = "";
            _reverseTripAmps = "";
            */

            _bidirectional = IdfTrue;
            _ganged = IdfTrue;
            _loadBreakCapable = IdfTrue;

            //ProcessCircuitBreakerAdms();

            _switchType = IdfSwitchTypeRecloser;
            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1HvCircuitBreaker>(T1Id);

                if (_t1Asset != null)
                {
                    //TODO: validate rated voltage
                    _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1HvcbRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            _symbol = SymbolFuseSaver;
        }

        private void ProcessEntec()
        {
            //_bidirectional //TODO
            //TODO: need to get sectionliser function from AdmsDatabase
            _ganged = IdfTrue;
            _switchType = IdfSwitchTypeSwitch;

            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (_t1Asset != null)
                {
                    //TODO: validate op voltage
                    _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                    //TODO
                    //_loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1DisconnectorRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }

            }
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            SetSymbol(p, SymbolEntec, SymbolEntecQuad);
        }

        private void ProcessHVLinks()
        {
            _ganged = IdfFalse;
            //TODO
            //_loadBreakCapable = IDF_FALSE;//TODO
            _ratedAmps = "300";//confirmed by robert
            _switchType = IdfSwitchTypeSwitch;

            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1Fuse>(T1Id);

                if (_t1Asset == null)
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }
                else
                {
                    //TODO: rated voltage always null here
                    ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
                    ValidateRatedVoltage(_baseKv, _t1Asset[T1FuseRatedVoltage]);
                }
            }
            
            _symbol = SymbolLinks;
        }

        private void ProcessDisconnector()
        {
            _ganged = IdfTrue;          
            _switchType = IdfSwitchTypeSwitch;

            if (string.IsNullOrWhiteSpace(T1Id))
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1Disconnector>(T1Id);

                if (_t1Asset != null)
                {
                    //TODO: validate rated voltage
                    _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                    //TODO
                    //_loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? IDF_FALSE : IDF_TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1DisconnectorRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
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
            
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            SetSymbol(p, SymbolDisconnector, SymbolDisconnectorQuad);
        }
        private void ProcessCircuitBreaker()
        {

            _bidirectional = IdfTrue;
            _ganged = IdfTrue;
            _loadBreakCapable = IdfTrue;

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

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, Name);
            SetSymbol(p, SymbolCircuitBreaker, SymbolCircuitBreakerQuad);
        }

        private void ProcessT1CircuitBreaker()
        {

            if (_t1Asset != null)
            {
                //TODO: validate op voltage
                _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps]);
                _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1HvcbRatedVoltage]);
                ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
            }
        }
        private void ProcessT1RingMainCb()
        {
            if (_t1Asset != null)
            {
                //TODO: validate operating voltage
                _ratedAmps = ValidatedRatedAmps(_t1Asset[T1SwitchRatedAmps], true);
                _maxInterruptAmps = ValidateMaxInterruptAmps(_t1Asset[T1SwitchMaxInterruptAmps]);
                _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1RmuRatedVoltage]);
                ValidateSwitchNumber(_t1Asset[T1RmuSwitchSw1], _t1Asset[T1RmuSwitchSw2], _t1Asset[T1RmuSwitchSw3], _t1Asset[T1RmuSwitchSw4]);
            }
        }
        private void ProcessCircuitBreakerAdms()
        {
            if (DataManager.I.RequestRecordById<AdmsSwitch>(Name) is DataType asset)
            {
                _admsAsset = asset;
                //TODO: RMU circuit breakers are generally not in the protection database... they use generic settings based on tx size.
                //how are we going to handle these?
                //TODO: validation on these?
                _nominalUpstreamSide = asset[AdmsSwitchNominalUpstreamSide];
                _forwardTripAmps = asset[AdmsSwitchForwardTripAmps];
                _reverseTripAmps = asset[AdmsSwitchReverseTripAmps];
                _switchType = asset[AdmsSwitchRecloserEnabled] == "Y" ? IdfSwitchTypeRecloser : IdfSwitchTypeBreaker;
                _faultProtectionAttrs = "faultprotectionattributes_default";
            }
            else
            {
                //all HV CBs should be in the ADMS db
                Warn("Breaker not in Adms database");
                _switchType = IdfSwitchTypeBreaker;
            }
        }
        private void ProcessHVFuse()
        {
            _bidirectional = IdfTrue;
            _forwardTripAmps = _reverseTripAmps = ValidateFuseTrip(_fuserating);
            _ganged = IdfFalse;
            //TODO
            //_loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "10000";//TODO check with sjw
            _ratedAmps = _forwardTripAmps == "" ? "" : (int.Parse(_forwardTripAmps) / 2).ToString();
            _switchType = IdfSwitchTypeFuse;

            if (T1Id == "")
            {
                Warn($"No T1 asset number assigned");
            }
            else
            {
                _t1Asset = DataManager.I.RequestRecordById<T1Fuse>(T1Id);

                if (_t1Asset != null)
                {
                    if (_t1Asset[T1FuseGanged] == "2")
                    {
                        _ganged = IdfTrue;
                    }
                    //TODO: fuse not have rated voltage?
                    //TODO: validate op voltage
                    _ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1FuseRatedVoltage]);
                    ValidateSwitchNumber(_t1Asset[T1SwitchNumber]);
                }
                else
                {
                    Warn($"T1 asset number [{T1Id}] wasn't in T1");
                }
            }
            
         _symbol = SymbolFuse;
        }
        private void ProcessServiceFuse()
        {
            _bidirectional = IdfTrue;
            _forwardTripAmps = _reverseTripAmps = "";
            _ganged = IdfFalse;
            //TODO
            //_loadBreakCapable = IDF_FALSE;
            _maxInterruptAmps = "";//TODO check with sjw
            _ratedAmps = "";//TODO?
            _switchType = "Fuse";
            _ratedKv = ValidateRatedVoltage(_baseKv, _baseKv, 1);
            _symbol = SymbolServiceFuse;
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
