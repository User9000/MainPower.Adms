﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
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
        private const string AdmsSwitchScadaId = "ScadaId";
        private const string AdmsSwitchOrientation = "Orientation";
        private const string AdmsSwitchSkipScada = "NoAutoScadaLinking";
        private const string AdmsSwitchBypassScadaStatus = "BypassScadaStatusCheck";
        private const string AdmsSwitchVoltageId = "VoltageLinkingScadaId";
        private const string AdmsSwitchInvertScadaStatus = "InvertScadaStatus";

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

        public const double IdfScaleGeographicHV = 17.0;
        public const double IdfScaleGeographicLV = 3.0;
        public const double IdfScaleInternals = 0.2;
        public const double IdfSwitchZ = double.NaN;
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
        private SymbolPlacement _orientation = SymbolPlacement.Left;

        //others
        private string _symbol = SymbolSwitch;
        private string _scadaName = "";
        private SearchMode _scadaSearchMode = SearchMode.EndsWith;
        private DataType _t1Asset = null;
        private DataType _admsAsset = null;
        private bool _scadaControllable = false;

        public IdfSwitch(XElement node, IdfGroup processor) : base(node, processor) { }
        
        public override void Process()
        {
            try
            {
                //the leading space is important.  By default, the name of the switch will be used for finding related SCADA points
                //the leading space assumes that the switch type (DIS/CB/LBS etc) precedes the switch number, hence there will be a space
                //the space is required to prevent false matches against similar numbers, e.g. W115 vs SW115
                //TODO: long term I think we should probably force exact matches via the ScadaId field on the AdmsSwitch dataset
                _scadaName = $" {Name}";

                CheckPhases();

                //var geo = ParentGroup.GetSymbolGeometry(Id);

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
                        //_ganged = IdfFalse;
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
                    case @"MV Line Switch\Circuit Breaker - Substation Feeder":
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
                    case @"LV Line Switch\LV Link":
                    case @"LV Line Switch\LV Switch":
                    case @"LV Line Switch\"://TODO: seem to be streetlight switches
                    case @"LV Line Switch\Knife Isolator":
                    case @"LV Isolator\LV Circuit Breaker"://TODO
                    case @"LV Isolator\LV Switch":
                        ProcessLVSwitch();
                        break;
                    case @"LV Line Switch\LV Fuse": //pole fuse
                    case @"LV Line Switch\Streetlight - Relay": //streetlight
                    case @"LV Line Switch\Streetlight - Photocell": //streetlight
                    case @"LV Line Switch\OH LV Fuse": //streetlight
                    case @"LV Isolator\LV Fuse"://TODO //indoor panel
                    case @"LV Isolator\LV Switch Fuse"://TODO //indoor panel
                    case @"LV Isolator\LV Switch Link"://TODO //indoor pabel
                    case @"LV Isolator\LV Link"://TODO //pole fuse link
                    case @"LV Isolator\LV Direct Connector": //direct connection
                    case @"LV Isolator\Streetlight - Relay": //streetlight
                    case @"LV Isolator\": //streetlight
                        ProcessLVFuse();
                        break;
                    //Others
                    case @"LV Line Switch\Service Fuse":
                    case @"LV Isolator\Service Fuse":
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

                if (string.IsNullOrWhiteSpace(_maxInterruptAmps))
                    _maxInterruptAmps = "15007";
                if (string.IsNullOrWhiteSpace(_ratedAmps))
                    _ratedAmps = "507";

                Node.SetAttributeValue(IdfSwitchBasekV, _baseKv);
                Node.SetAttributeValue(IdfSwitchBidirectional, _bidirectional);
                Node.SetAttributeValue(IdfSwitchForwardTripAmps, _forwardTripAmps);
                Node.SetAttributeValue(IdfSwitchGanged, _ganged);
                Node.SetAttributeValue(IdfSwitchLoadBreakCapable, _loadBreakCapable);
                Node.SetAttributeValue(IdfSwitchMaxInterruptAmps, _maxInterruptAmps);
                Node.SetAttributeValue(IdfSwitchRatedAmps, _ratedAmps);
                //TODO formalise this
                Node.SetAttributeValue(IdfSwitchRatedkV, _baseKv);
                Node.SetAttributeValue(IdfSwitchReverseTripAmps, _reverseTripAmps);
                Node.SetAttributeValue(IdfSwitchType, _switchType);

                if (!string.IsNullOrWhiteSpace(_nominalUpstreamSide))
                    Node.SetAttributeValue(IdfSwitchNominalUpstreamSide, _nominalUpstreamSide);
                if (!string.IsNullOrWhiteSpace(_faultProtectionAttrs))
                    Node.SetAttributeValue(IdfSwitchFaultProtectionAttrs, _faultProtectionAttrs);               

                //TODO tidy this up
                //TODO make db constants the same as IdfConstants
                if (_admsAsset?[AdmsSwitchSkipScada] != IdfTrue)
                {
                    var scada = GenerateScadaLinking();
                    
                    if (scada != null)
                    {
                        ParentGroup.AddGroupElement(scada);
                    }
                }
                else
                {
                    Info("Skipping automatic SCADA linking");
                }
                List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
                items.Add(new KeyValuePair<string, string>("GIS Switch Type", _gisswitchtype.Length > 39 ? _gisswitchtype.Substring(0,39): _gisswitchtype));
                GenerateDeviceInfo(items);
                RemoveExtraAttributes();

                var device = Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Switch, _symbol);
                if (device != null)
                    device.Flags |= 0x04;
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
        
        //TODO: this return value is gross
        private XElement GenerateScadaLinking()
        {
            bool hasVoltsus = false, hasVoltsds = false;

            string voltageName = string.IsNullOrWhiteSpace(_admsAsset?[AdmsSwitchVoltageId]) ? _scadaName : _admsAsset?[AdmsSwitchVoltageId];

            var status = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, _scadaName, _scadaSearchMode);
            if (status != null)
            {
                //TODO: can we get the type?  i.e. T_I&C or T_IND?
                _scadaControllable = true;
            }
            //if we don't have the switch status, then assume we don't have any other telemtry either
            //TODO this assumption needs to be documented
            //TODO: dirty hack because we don't have the status of these CBs
            if (status == null && _admsAsset?[AdmsSwitchBypassScadaStatus] != IdfTrue)
                return null;
            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            bool phase1, phase2, phase3;

            phase1 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value);
            phase2 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value);
            phase3 = !string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value);

            if (status != null)
            {
                if (status["Type"] == "T_I&C")
                    x.SetAttributeValue("gangedControlPoint", status.Key);

                if (phase1)
                    x.SetAttributeValue("p1State", status.Key);
                if (phase2)
                    x.SetAttributeValue("p2State", status.Key);
                if (phase3)
                    x.SetAttributeValue("p3State", status.Key);
            }

            //we can't assign any of this telemtry without knowing what the upstream side of the switch is
            //TODO emit a warning if not set
            //TODO if upstream side changes we need to run enricher again to get the change?
            if (_nominalUpstreamSide == "1" || _nominalUpstreamSide == "2")
            {
                //set the upstream and downstream nodes
                string us = _nominalUpstreamSide;
                string ds = us == "1" ? "2" : "1";

                //link the amps to side 1, the dpf engine won't use it unless power factor is also linked
                //we don't want dpf to use amps, because our amps are unsigned, and not guaranteed to be on the correct side
                //we are linking them to make it easy to display the data in tab. viewer
                var rAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Amps RØ", _scadaSearchMode);
                if (rAmps != null && phase1)
                {                        
                    x.SetAttributeValue($"s1p1Amps", rAmps.Key);
                    x.SetAttributeValue($"s1p1AmpsUCF", "1");
                }
                var yAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Amps YØ", _scadaSearchMode);
                if (yAmps != null && phase2)
                {
                    x.SetAttributeValue($"s1p2Amps", yAmps.Key);
                    x.SetAttributeValue($"s1p2AmpsUCF", "1");
                }
                var bAmps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Amps BØ", _scadaSearchMode);
                if (bAmps != null && phase3)
                {
                    x.SetAttributeValue($"s1p3Amps", bAmps.Key);
                    x.SetAttributeValue($"s1p3AmpsUCF", "1");
                }

                //TODO: this can be removed after a few runs
                /*
                x.SetAttributeValue($"s{us}p1Amps", "");
                x.SetAttributeValue($"s{us}p1AmpsUCF", "");
                x.SetAttributeValue($"s{us}p2Amps", "");
                x.SetAttributeValue($"s{us}p2AmpsUCF", "");
                x.SetAttributeValue($"s{us}p3Amps", "");
                x.SetAttributeValue($"s{us}p3AmpsUCF", "");

                x.SetAttributeValue($"s{ds}p1Amps", "");
                x.SetAttributeValue($"s{ds}p1AmpsUCF", "");
                x.SetAttributeValue($"s{ds}p2Amps", "");
                x.SetAttributeValue($"s{ds}p2AmpsUCF", "");
                x.SetAttributeValue($"s{ds}p3Amps", "");
                x.SetAttributeValue($"s{ds}p3AmpsUCF", "");
                */

                //when it comes to load telemetry, priority goes
                //1. Per phase metering MW, then kW
                //2. Aggregate metering MW, then kw
                //3. Per phase local kW/MW
                //4. Aggregate local kW/MW
                //if using per phase telemtry, all present phases must have telemetry

                //look for per phase metering data
                (OsiScadaAnalog Point, string Ucf) red = (null,""), yellow= (null, ""), blue = (null, ""), aggregate = (null, "");
                bool havePerPhase = true;

                //TODO: remove temp matching for Red/Yellow/Blue
                #region KW SCADA LINKING LOGIC EWW

                //1. Check for per phase metering
                if (phase1)
                {
                    red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW RØ", _scadaSearchMode), "1000");
                    if (red.Point == null)
                        red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW Red", _scadaSearchMode), "1000");
                    if (red.Point == null)
                        red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kW RØ", _scadaSearchMode), "1");
                    if (red.Point == null)
                        havePerPhase = false;
                }
                if (phase2 && havePerPhase)
                {
                    yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW YØ", _scadaSearchMode), "1000");
                    if (yellow.Point == null)
                        yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW Yellow", _scadaSearchMode), "1000");

                    if (yellow.Point == null)
                        yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kW YØ", _scadaSearchMode), "1");
                    if (yellow.Point == null)
                        havePerPhase = false;
                }
                if (phase3 && havePerPhase)
                {
                    blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW BØ", _scadaSearchMode), "1000");
                    if (blue.Point == null)
                        blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW Blue", _scadaSearchMode), "1000");
                    if (blue.Point == null)
                        blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kW BØ", _scadaSearchMode), "1");
                    if (blue.Point == null)
                        havePerPhase = false;
                }
                if (havePerPhase)
                {
                    //write out per phase attributes
                    //we will assume that switches to not change number of phases throughout their lifetime, this is an extreme edge case
                    //that can be fixed manually
                    if (phase1)
                    {
                        x.SetAttributeValue($"s{us}p1KW", red.Point.Key);
                        x.SetAttributeValue($"s{us}p1KWUCF", red.Ucf);
                        x.SetAttributeValue($"s{ds}p1KW", "");
                    }
                    if (phase2)
                    {
                        x.SetAttributeValue($"s{us}p2KW", yellow.Point.Key);
                        x.SetAttributeValue($"s{us}p2KWUCF", yellow.Ucf);
                        x.SetAttributeValue($"s{ds}p2KW", "");
                    }
                    if (phase3)
                    {
                        x.SetAttributeValue($"s{us}p3KW", blue.Point.Key);
                        x.SetAttributeValue($"s{us}p3KWUCF", blue.Ucf);
                        x.SetAttributeValue($"s{ds}p3KW", "");
                    }
                    //we also have to null the aggregate values in case they were used previously
                    x.SetAttributeValue($"s{us}AggregateKW", "");
                    x.SetAttributeValue($"s{ds}AggregateKW", "");
                }
                else
                {
                    //2. look for aggregate metering
                    aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met MW", _scadaSearchMode), "1000");
                    if (aggregate.Point == null)
                        aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kW", _scadaSearchMode), "1");
                    if (aggregate.Point != null)
                    {
                        //write out aggregate attributes
                        x.SetAttributeValue($"s{us}AggregateKW", aggregate.Point.Key);
                        x.SetAttributeValue($"s{us}AggregateKWUCF", aggregate.Ucf);
                        x.SetAttributeValue($"s{ds}AggregateKW", "");
                        //we also have to null the phase values in case it changed from single phase to three phase
                        x.SetAttributeValue($"s{us}p1KW", "");
                        x.SetAttributeValue($"s{ds}p1KW", "");
                        x.SetAttributeValue($"s{us}p2KW", "");
                        x.SetAttributeValue($"s{ds}p2KW", "");
                        x.SetAttributeValue($"s{us}p3KW", "");
                        x.SetAttributeValue($"s{ds}p3KW", "");
                    }
                    else
                    {
                        //3. look for per phase local values
                        //reset variables
                        havePerPhase = true;
                        if (phase1)
                        {
                            red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} MW RØ", _scadaSearchMode), "1000");
                            if (red.Point == null)
                                red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kW RØ", _scadaSearchMode), "1");
                            if (red.Point == null)
                                havePerPhase = false;
                        }
                        if (phase2 && havePerPhase)
                        {
                            yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} MW YØ", _scadaSearchMode), "1000");
                            if (yellow.Point == null)
                                yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kW YØ", _scadaSearchMode), "1");
                            if (yellow.Point == null)
                                havePerPhase = false;
                        }
                        if (phase3 && havePerPhase)
                        {
                            blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} MW BØ", _scadaSearchMode), "1000");
                            if (blue.Point == null)
                                blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kW BØ", _scadaSearchMode), "1");
                            if (blue.Point == null)
                                havePerPhase = false;
                        }
                        if (havePerPhase)
                        {
                            //write out per phase attributes
                            //we will assume that switches to not change number of phases throughout their lifetime, this is an extreme edge case
                            //that can be fixed manually
                            if (phase1)
                            {
                                x.SetAttributeValue($"s{us}p1KW", red.Point.Key);
                                x.SetAttributeValue($"s{us}p1KWUCF", red.Ucf);
                                x.SetAttributeValue($"s{ds}p1KW", "");
                            }
                            if (phase2)
                            {
                                x.SetAttributeValue($"s{us}p2KW", yellow.Point.Key);
                                x.SetAttributeValue($"s{us}p2KWUCF", yellow.Ucf);
                                x.SetAttributeValue($"s{ds}p2KW", "");
                            }
                            if (phase3)
                            {
                                x.SetAttributeValue($"s{us}p3KW", blue.Point.Key);
                                x.SetAttributeValue($"s{us}p3KWUCF", blue.Ucf);
                                x.SetAttributeValue($"s{ds}p3KW", "");
                            }
                            //we also have to null the aggregate values in case they were used previously
                            x.SetAttributeValue($"s{us}AggregateKW", "");
                            x.SetAttributeValue($"s{ds}AggregateKW", "");
                        }
                        else
                        {
                            //look for aggregate metering
                            aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} MW", _scadaSearchMode), "1000");
                            if (aggregate.Point == null)
                                aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kW", _scadaSearchMode), "1");
                            if (aggregate.Point != null)
                            {
                                //write out aggregate attributes
                                x.SetAttributeValue($"s{us}AggregateKW", aggregate.Point.Key);
                                x.SetAttributeValue($"s{us}AggregateKWUCF", aggregate.Ucf);
                                x.SetAttributeValue($"s{ds}AggregateKW", "");
                                //we also have to null the phase values in case it changed from single phase to three phase
                                x.SetAttributeValue($"s{us}p1KW", "");
                                x.SetAttributeValue($"s{ds}p1KW", "");
                                x.SetAttributeValue($"s{us}p2KW", "");
                                x.SetAttributeValue($"s{ds}p2KW", "");
                                x.SetAttributeValue($"s{us}p3KW", "");
                                x.SetAttributeValue($"s{ds}p3KW", "");
                            }
                        }
                    }
                }
                #endregion

                #region KVAR SCADA LINKING LOGIC ALSO EWW
                //1. Check for per phase metering
                if (phase1)
                {
                    red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met Mvar RØ", _scadaSearchMode), "1000");
                    if (red.Point == null)
                        red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kvar RØ", _scadaSearchMode), "1");
                    if (red.Point == null)
                        havePerPhase = false;
                }
                if (phase2 && havePerPhase)
                {
                    yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met Mvar YØ", _scadaSearchMode), "1000");
                    if (yellow.Point == null)
                        yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kvar YØ", _scadaSearchMode), "1");
                    if (yellow.Point == null)
                        havePerPhase = false;
                }
                if (phase3 && havePerPhase)
                {
                    blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met Mvar BØ", _scadaSearchMode), "1000");
                    if (blue.Point == null)
                        blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kvar BØ", _scadaSearchMode), "1");
                    if (blue.Point == null)
                        havePerPhase = false;
                }
                if (havePerPhase)
                {
                    //write out per phase attributes
                    //we will assume that switches to not change number of phases throughout their lifetime, this is an extreme edge case
                    //that can be fixed manually
                    if (phase1)
                    {
                        x.SetAttributeValue($"s{us}p1KVAR", red.Point.Key);
                        x.SetAttributeValue($"s{us}p1KVARUCF", red.Ucf);
                        x.SetAttributeValue($"s{ds}p1KVAR", "");
                    }
                    if (phase2)
                    {
                        x.SetAttributeValue($"s{us}p2KVAR", yellow.Point.Key);
                        x.SetAttributeValue($"s{us}p2KVARUCF", yellow.Ucf);
                        x.SetAttributeValue($"s{ds}p2KVAR", "");
                    }
                    if (phase3)
                    {
                        x.SetAttributeValue($"s{us}p3KVAR", blue.Point.Key);
                        x.SetAttributeValue($"s{us}p3KVARUCF", blue.Ucf);
                        x.SetAttributeValue($"s{ds}p3KVAR", "");
                    }
                    //we also have to null the aggregate values in case they were used previously
                    x.SetAttributeValue($"s{us}AggregateKVAR", "");
                    x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                }
                else
                {
                    //2. look for aggregate metering
                    aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met Mvar", _scadaSearchMode), "1000");
                    if (aggregate.Point == null)
                        aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Met kvar", _scadaSearchMode), "1");
                    if (aggregate.Point != null)
                    {
                        //write out aggregate attributes
                        x.SetAttributeValue($"s{us}AggregateKVAR", aggregate.Point.Key);
                        x.SetAttributeValue($"s{us}AggregateKVARUCF", aggregate.Ucf);
                        x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                        //we also have to null the phase values in case it changed from single phase to three phase
                        x.SetAttributeValue($"s{us}p1KVAR", "");
                        x.SetAttributeValue($"s{ds}p1KVAR", "");
                        x.SetAttributeValue($"s{us}p2KVAR", "");
                        x.SetAttributeValue($"s{ds}p2KVAR", "");
                        x.SetAttributeValue($"s{us}p3KVAR", "");
                        x.SetAttributeValue($"s{ds}p3KVAR", "");
                    }
                    else
                    {
                        //3. look for per phase local values
                        //reset variables
                        havePerPhase = true;
                        if (phase1)
                        {
                            red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Mvar RØ", _scadaSearchMode), "1000");
                            if (red.Point == null)
                                red = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kvar RØ", _scadaSearchMode), "1");
                            if (red.Point == null)
                                havePerPhase = false;
                        }
                        if (phase2 && havePerPhase)
                        {
                            yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Mvar YØ", _scadaSearchMode), "1000");
                            if (yellow.Point == null)
                                yellow = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kvar YØ", _scadaSearchMode), "1");
                            if (yellow.Point == null)
                                havePerPhase = false;
                        }
                        if (phase3 && havePerPhase)
                        {
                            blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Mvar BØ", _scadaSearchMode), "1000");
                            if (blue.Point == null)
                                blue = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kvar BØ", _scadaSearchMode), "1");
                            if (blue.Point == null)
                                havePerPhase = false;
                        }
                        if (havePerPhase)
                        {
                            //write out per phase attributes
                            //we will assume that switches to not change number of phases throughout their lifetime, this is an extreme edge case
                            //that can be fixed manually
                            if (phase1)
                            {
                                x.SetAttributeValue($"s{us}p1KVAR", red.Point.Key);
                                x.SetAttributeValue($"s{us}p1KVARUCF", red.Ucf);
                                x.SetAttributeValue($"s{ds}p1KVAR", "");
                            }
                            if (phase2)
                            {
                                x.SetAttributeValue($"s{us}p2KVAR", yellow.Point.Key);
                                x.SetAttributeValue($"s{us}p2KVARUCF", yellow.Ucf);
                                x.SetAttributeValue($"s{ds}p2KVAR", "");
                            }
                            if (phase3)
                            {
                                x.SetAttributeValue($"s{us}p3KVAR", blue.Point.Key);
                                x.SetAttributeValue($"s{us}p3KVARUCF", blue.Ucf);
                                x.SetAttributeValue($"s{ds}p3KVAR", "");
                            }
                            //we also have to null the aggregate values in case they were used previously
                            x.SetAttributeValue($"s{us}AggregateKVAR", "");
                            x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                        }
                        else
                        {
                            //look for aggregate metering
                            aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Mvar", _scadaSearchMode), "1000");
                            if (aggregate.Point == null)
                                aggregate = (DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} kvar", _scadaSearchMode), "1");
                            if (aggregate.Point != null)
                            {
                                //write out aggregate attributes
                                x.SetAttributeValue($"s{us}AggregateKVAR", aggregate.Point.Key);
                                x.SetAttributeValue($"s{us}AggregateKVARUCF", aggregate.Ucf);
                                x.SetAttributeValue($"s{ds}AggregateKVAR", "");
                                //we also have to null the phase values in case it changed from single phase to three phase
                                x.SetAttributeValue($"s{us}p1KVAR", "");
                                x.SetAttributeValue($"s{ds}p1KVAR", "");
                                x.SetAttributeValue($"s{us}p2KVAR", "");
                                x.SetAttributeValue($"s{ds}p2KVAR", "");
                                x.SetAttributeValue($"s{us}p3KVAR", "");
                                x.SetAttributeValue($"s{ds}p3KVAR", "");
                            }
                        }
                    }
                }
                #endregion

                #region VOLT SCADA LINKING LOGIC

                //clear whatever might have been there previously
                x.SetAttributeValue("s1p1KV","");
                x.SetAttributeValue("s1p2KV", "");
                x.SetAttributeValue("s1p3KV", "");
                x.SetAttributeValue("s2p1KV", "");
                x.SetAttributeValue("s2p2KV", "");
                x.SetAttributeValue("s2p3KV", "");

                //we must only set phase, or line voltages on each side - lets keep track of that
                var usPhase = false;
                var usLine = false;
                var dsPhase = false;
                var dsLine = false;

                var s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts RØ", _scadaSearchMode);
                if (s1RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts RØ", _scadaSearchMode)) != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts RY", _scadaSearchMode)) != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    usLine = true;
                }
                else if ((s1RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts RY", _scadaSearchMode)) != null && phase1)
                {
                    x.SetAttributeValue($"s{us}p1KV", s1RYVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    usLine = true;
                }
                
                var s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts YØ", _scadaSearchMode);
                if (s1YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts YØ", _scadaSearchMode)) != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts YB", _scadaSearchMode)) != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    usPhase = true;
                }
                else if ((s1YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts YB", _scadaSearchMode)) != null && phase2)
                {
                    x.SetAttributeValue($"s{us}p2KV", s1YBVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    usLine = true;
                }
                var s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts BØ", _scadaSearchMode);
                if (s1BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", s1BRVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts BØ", _scadaSearchMode)) != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", s1BRVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LG");
                    usPhase = true;
                }
                else if ((s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Met Volts BR", _scadaSearchMode)) != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", s1BRVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    usLine = true;
                }
                else if ((s1BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts BR", _scadaSearchMode)) != null && phase3)
                {
                    x.SetAttributeValue($"s{us}p3KV", s1BRVolts.Key);
                    x.SetAttributeValue($"s{us}VoltageType", "LL");
                    hasVoltsus = true;
                }
                var s2RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts RØ2", _scadaSearchMode);
                if (s2RYVolts != null && phase1)
                {
                    x.SetAttributeValue($"s{ds}p1KV", s2RYVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    dsPhase = true;
                }
                else if ((s2RYVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts RY2", _scadaSearchMode)) != null && phase1)
                {
                    x.SetAttributeValue($"s{ds}p1KV", s2RYVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LL");
                    dsLine = true;
                }
                var s2YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts YØ2", _scadaSearchMode);
                if (s2YBVolts != null && phase2)
                {
                    x.SetAttributeValue($"s{ds}p2KV", s2YBVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    dsPhase = true;
                }
                else if ((s2YBVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts YB2", _scadaSearchMode)) != null && phase2)
                {
                    x.SetAttributeValue($"s{ds}p2KV", s2YBVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LL");
                    dsLine = true;
                }
                var s2BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts BØ2", _scadaSearchMode);
                if (s2BRVolts != null && phase3)
                {
                    x.SetAttributeValue($"s{ds}p3KV", s2BRVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LG");
                    dsPhase = true;
                }
                else if ((s2BRVolts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{voltageName} Volts BR2", _scadaSearchMode)) != null && phase3)
                {
                    x.SetAttributeValue($"s{ds}p3KV", s2BRVolts.Key);
                    x.SetAttributeValue($"s{ds}VoltageType", "LL");
                    dsLine = true;
                }
                #endregion

                var lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Lockout", _scadaSearchMode);
                //if (lockout == null)
                //    lockout = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Trip4 OC", _scadaSearchMode);
                if (lockout != null)
                {
                    if (phase1)
                        x.SetAttributeValue("p1TripFaultSignal", lockout.Key);
                    if (phase2)
                        x.SetAttributeValue("p2TripFaultSignal", lockout.Key);
                    if (phase3)
                        x.SetAttributeValue("p3TripFaultSignal", lockout.Key);
                }

                var ampsr = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Prot Trip Amps RØ", _scadaSearchMode);
                if (ampsr != null && phase1)
                {
                    x.SetAttributeValue("p1FaultCurrent", ampsr.Key);
                }
                var ampsy = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Prot Trip Amps YØ", _scadaSearchMode);
                if (ampsy != null && phase3)
                {
                    x.SetAttributeValue("p2FaultCurrent", ampsy.Key);
                }
                var ampsb = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{_scadaName} Prot Trip Amps BØ", _scadaSearchMode);
                if (ampsb != null && phase3)
                {
                    x.SetAttributeValue("p3FaultCurrent", ampsb.Key);
                }

                //TODO: handle cases where there are two relays?
                var watchdog = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Relay Watchdog", _scadaSearchMode);

                //p1OCPMode = Watchdog 
                //OCPModeNormal = 1
                //p1OCPMode = Watchdog 
                //p1FaultCurrent = fault current
                //p1TripFaultSignal = lockout - or should it be the per phase trip flags?
                //p1FaultInd = for directional devices is the side 1 fault indication, otherwise non directional
                //p1FaultInd2 = for directional devices is the side 2 fault indication, otherwise not required

                var p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Fault RØ", _scadaSearchMode);
                if (p1Fault != null)
                {
                    x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                }
                else
                {
                    x.SetAttributeValue("p1FaultInd", "");
                }
                //else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot OC RØ Trip", _scadaSearchMode)) != null)
                //{
                    //x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                //}
                //else if ((p1Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Trip1 OC", _scadaSearchMode)) != null)
                //{
                    //x.SetAttributeValue("p1FaultInd", p1Fault.Key);
                //}

                var p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Fault YØ", _scadaSearchMode);
                if (p2Fault != null)
                {
                    x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                }
                else
                {
                    x.SetAttributeValue("p2FaultInd", "");
                }
                //else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot OC YØ Trip", _scadaSearchMode)) != null)
                //{
                //x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                //}
                //else if ((p2Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Trip2 OC", _scadaSearchMode)) != null)
                //{
                //x.SetAttributeValue("p2FaultInd", p2Fault.Key);
                //}

                var p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Fault BØ", _scadaSearchMode);
                if (p3Fault != null)
                {
                    x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                }
                else
                {
                    x.SetAttributeValue("p3FaultInd", "");
                }
                //else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot OC RØ Trip", _scadaSearchMode)) != null)
                //{
                //x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                //}
                //else if ((p3Fault = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot Trip3 OC", _scadaSearchMode)) != null)
                //{
                //x.SetAttributeValue("p3FaultInd", p3Fault.Key);
                //}


                var hlt = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} WorkTag", _scadaSearchMode);
                if (hlt != null)
                    x.SetAttributeValue("hotLineTag", hlt.Key);

                var groundTripBlock = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Prot SEF", _scadaSearchMode);
                if (groundTripBlock != null)
                    x.SetAttributeValue("groundTripBlock", groundTripBlock.Key);

                var acr = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{_scadaName} Auto Reclose", _scadaSearchMode);
                if (acr != null)
                    x.SetAttributeValue("reclosing", acr.Key);

                x.SetAttributeValue("faultIndType", "Directionless");

                //reference voltage must be set for correct processing of violations
                var d = double.Parse(_baseKv) / Math.Sqrt(3);
                if (usPhase) 
                    x.SetAttributeValue($"s{us}VoltageReference", d.ToString("F2"));
                else if (usLine)
                    x.SetAttributeValue($"s{us}VoltageReference", _baseKv);
                if (dsPhase)
                    x.SetAttributeValue($"s{ds}VoltageReference", d.ToString("F2"));
                else if (dsLine)
                    x.SetAttributeValue($"s{ds}VoltageReference", _baseKv);

            }
            
            return x;
                
        }

#region Switch Type Processing
        
        private void SetSymbol(OsiScadaStatus p, string normal, string quad)
        {
            _symbol = normal;
            return;
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
                    //_ratedKv = ValidateRatedVoltage(_baseKv, _t1Asset[T1RmuRatedVoltage]);
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
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, _scadaName, _scadaSearchMode);
            SetSymbol(p, SymbolSwitch, SymbolSwitchQuad);
        }

        private void ProcessLVSwitch()
        {
            if (!string.IsNullOrEmpty(T1Id))
                Warn($"T1 asset number [{T1Id}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            //_ganged = IdfFalse; //TODO check
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
            //_ganged = IdfFalse; //TODO check
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
            ProcessSwitchAdms();
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
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, _scadaName, _scadaSearchMode);
            SetSymbol(p, SymbolEntec, SymbolEntecQuad);
        }

        private void ProcessHVLinks()
        {
            //_ganged = IdfFalse;
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

            ProcessSwitchAdms();
            
            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, _scadaName, _scadaSearchMode);
            SetSymbol(p, SymbolDisconnector, SymbolDisconnectorQuad);
        }
        private void ProcessSwitchAdms()
        {
            if (DataManager.I.RequestRecordById<AdmsSwitch>(Name) is DataType asset)
            {
                _admsAsset = asset;
                _nominalUpstreamSide = _admsAsset[AdmsSwitchNominalUpstreamSide];
                var scadaId = _admsAsset[AdmsSwitchScadaId];
                if (!string.IsNullOrWhiteSpace(scadaId))
                {
                    _scadaName = scadaId;
                    _scadaSearchMode = SearchMode.Exact;
                }
            }
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

            var p = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, _scadaName, _scadaSearchMode);
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
            ProcessSwitchAdms();
            if (_admsAsset != null)
            {
                //TODO: RMU circuit breakers are generally not in the protection database... they use generic settings based on tx size.
                //how are we going to handle these?
                //TODO: validation on these?
                _nominalUpstreamSide = _admsAsset[AdmsSwitchNominalUpstreamSide];
                _forwardTripAmps = _admsAsset[AdmsSwitchForwardTripAmps];
                _reverseTripAmps = _admsAsset[AdmsSwitchReverseTripAmps];
                _switchType = _admsAsset[AdmsSwitchRecloserEnabled] == "Y" ? IdfSwitchTypeRecloser : IdfSwitchTypeBreaker;
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
            if (_fuserating == "L")
            {
                Warn("GIS switch type is fuse, but fuse rating says links");
                ProcessHVLinks();
                return;
            }
            _bidirectional = IdfTrue;
            _forwardTripAmps = _reverseTripAmps = ValidateFuseTrip(_fuserating);
            //_ganged = IdfFalse;
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
            //_ganged = IdfFalse;
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
            //bit lazy, match all numbers and decimals and let double.parse do the work
            Regex r = new Regex("([0-9]|\\.)+");
            var match = r.Match(amps);
            if (match.Success)
            {
                if (double.TryParse(match.Value, out double result))
                {
                    return (result * 1000).ToString();
                }
            }                
            Warn( $"Could not parse T1 max interrupt amps [{amps}]");
            return "";
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
            ///TODO: get a regex onto this
            //handle the simple case
            if (string.IsNullOrEmpty(amps))
            {
                Info("T1 rated amps is unset");
                return "";
            }
            if (amps == "630/400" || amps == "LB")
            {
                Warn($"Dirty hack deployed to parse rated amps [{amps}]");
                amps = "630";
            }
            if (amps.EndsWith("A"))
                amps = amps[0..^1];

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
            //sometimes the fuse type isnt included
            //Regex r = new Regex("[0-9]+(?=(T|K|F))");
            Regex r = new Regex("[0-9]+");
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
