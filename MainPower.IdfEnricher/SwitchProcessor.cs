using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
    class SwitchProcessor : DeviceProcessor
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

        private const string TRUE = "True";
        private const string FALSE = "False";

        private const string T1_FUSE_GANGED = "Is Tri Fuse";
        private const string T1_SWITCH_SWNUMBER = "Switch Number";
        private const string T1_SWITCH_RATED_VOLTAGE = "Rated Voltage";
        private const string T1_SWITCH_RATED_AMPS = "Rated Current";
        private const string T1_SWITCH_MAX_INTERRUPT_AMPS = "Fault kA/sec";
        private const string T1_SWITCH_LOAD_BREAK_RATING = "Load Break Rating";
        private const string T1_SWITCH_SW1 = "SW 1";
        private const string T1_SWITCH_SW2 = "SW 2";
        private const string T1_SWITCH_SW3 = "SW 3";
        private const string T1_SWITCH_SW4 = "SW 4";

        private const string GIS_SWITCH_TYPE = "mpwr_gis_switch_type";
        private const string GIS_T1_ASSET = "mpwr_t1_asset_nbr";
        private const string GIS_FUSE_RATING = "mpwr_fuse_rating";

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
        private const string IDF_SWITCH_NAME = "name";
        private const string IDF_SWITCH_ID = "id";

        private const string IDF_SWITCH_TYPE_FUSE = "Fuse";
        private const string IDF_SWITCH_TYPE_BREAKER = "Breaker";
        private const string IDF_SWITCH_TYPE_SWITCH = "Switch";
        private const string IDF_SWITCH_TYPE_RECLOSER = "Recloser";
        private const string IDF_SWITCH_TYPE_SECTIONALISER = "Sectionaliser";

        private const string ERR_CAT_FUSE = "FUSE";
        private const string ERR_CAT_BREAKER = "BREAKER";
        private const string ERR_CAT_DISCONNECTOR = "DISCONNECTOR";
        private const string ERR_CAT_LINKS = "LINKS";
        private const string ERR_CAT_FUSESAVER = "FUSESAVER";
        private const string ERR_CAT_RINGMAINCB = "RMU CB";
        private const string ERR_CAT_ENTEC = "ENTEC";
        private const string ERR_CAT_RINGMAINSWITCH = "RMU SWITCH";
        private const string ERR_CAT_RINGMAINFUSESWITCH = "RMU FUSE SWITCH";
        private const string ERR_CAT_LVFUSE = "LV FUSE";
        private const string ERR_CAT_LVSWITCH = "LV SWITCH";
        private const string ERR_CAT_SWITCH = "SWITCH";
        private const string ERR_CAT_SCADA = "SCADA";
        private const string ERR_CAT_GENERAL = "GENERAL";

        private const string ADMS_SWITCH_FORWARDTRIPAMPS = "forwardTripAmps";
        private const string ADMS_SWITCH_REVERSETRIPAMPS = "reverseTripAmps";
        private const string ADMS_SWITCH_RECLOSER_ENABLED = "Recloser";

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
        private const double IDF_SCALE_INTERNALS = 0.5;

        #endregion

        //compulsory fields
        private string _name = "";
        private string _id = "";

        //temporary fields from GIS
        private string _t1assetno = "";
        private string _gisswitchtype = "";
        private string _fuserating = "";
        
        //fields that should be set and validated by this class
        private string _baseKv = "";//GIS will set this to the operating voltage
        private string _bidirectional = FALSE;
        private string _forwardTripAmps = "";
        private string _ganged = "";
        private string _loadBreakCapable = "";
        private string _maxInterruptAmps = "";
        private string _ratedAmps = "";
        private string _ratedKv = "";
        private string _reverseTripAmps = "";
        private string _switchType = "";

        internal SwitchProcessor(XmlElement node, GroupProcessor processor) : base(node, processor) { }
        
        internal override void Process()
        {
            try
            {                
                _id = Node.Attributes[IDF_SWITCH_ID].InnerText;
                _name = Node.Attributes[IDF_SWITCH_NAME].InnerText;
                if (Node.HasAttribute(GIS_T1_ASSET))
                    _t1assetno = Node.Attributes[GIS_T1_ASSET].InnerText;
                if (Node.HasAttribute(GIS_SWITCH_TYPE))
                    _gisswitchtype = Node.Attributes[GIS_SWITCH_TYPE].InnerText;
                if (Node.HasAttribute(GIS_FUSE_RATING))
                    _fuserating = Node.Attributes[GIS_FUSE_RATING].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_BIDIRECTIONAL))
                    _bidirectional = Node.Attributes[IDF_SWITCH_BIDIRECTIONAL].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_FORWARDTRIPAMPS))
                    _forwardTripAmps = Node.Attributes[IDF_SWITCH_FORWARDTRIPAMPS].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_REVERSETRIPAMPS))
                    _reverseTripAmps = Node.Attributes[IDF_SWITCH_REVERSETRIPAMPS].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_GANGED))
                    _ganged = Node.Attributes[IDF_SWITCH_GANGED].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_LOADBREAKCAPABLE))
                    _loadBreakCapable = Node.Attributes[IDF_SWITCH_LOADBREAKCAPABLE].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_MAXINTERRUPTAMPS))
                    _maxInterruptAmps = Node.Attributes[IDF_SWITCH_MAXINTERRUPTAMPS].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_RATEDAMPS))
                    _ratedAmps = Node.Attributes[IDF_SWITCH_RATEDAMPS].InnerText;
                if (Node.HasAttribute(IDF_SWITCH_RATEDKV))
                    _ratedKv = Node.Attributes[IDF_SWITCH_RATEDKV].InnerText;

                _switchType = Node.Attributes[IDF_SWITCH_SWITCHTYPE].InnerText;
                _baseKv = Node.Attributes[IDF_SWITCH_BASEKV].InnerText;

                switch (_gisswitchtype)
                {
                    //MV Isolator
                    case @"MV Isolator\Knife Isolator":
                        //basically a non ganged disconnector e.g. links
                        ProcessDisconnector(true);
                        _ganged = FALSE;
                        _loadBreakCapable = FALSE;
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
                        ProcessSwitch();
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
                        ProcessLVSwitch();
                        break;
                    case @"LV Line Switch\LV Fuse":
                        ProcessLVFuse();
                        break;
                    //Others
                    case @"Service Fuse":
                        ProcessServiceFuse();
                        break;
                    case "":
                        Error("GEN", $"Gis Switch Type is not set");
                        break;
                    default:
                        Error("GEN", $"Unrecognised GisSwitchType [{_gisswitchtype}]");
                        break;
                }
                
                Node.SetAttribute(IDF_SWITCH_BASEKV, _baseKv);
                Node.SetAttribute(IDF_SWITCH_BIDIRECTIONAL, _bidirectional);
                Node.SetAttribute(IDF_SWITCH_FORWARDTRIPAMPS, _forwardTripAmps);
                Node.SetAttribute(IDF_SWITCH_GANGED, _ganged);
                Node.SetAttribute(IDF_SWITCH_LOADBREAKCAPABLE, _loadBreakCapable);
                Node.SetAttribute(IDF_SWITCH_MAXINTERRUPTAMPS, _maxInterruptAmps);
                Node.SetAttribute(IDF_SWITCH_RATEDAMPS, _ratedAmps);
                Node.SetAttribute(IDF_SWITCH_RATEDKV, _ratedKv);
                Node.SetAttribute(IDF_SWITCH_REVERSETRIPAMPS, _reverseTripAmps);
                Node.SetAttribute(IDF_SWITCH_SWITCHTYPE, _switchType);
                var scada = GenerateScadaLinking();
                if (!string.IsNullOrWhiteSpace(scada))
                {
                    Processor.AddGroupElement(scada);
                }
                RemoveExtraAttributes();
                //Debug("SWITCH",  ToString());
            }
            catch (Exception ex)
            {
                Error(ERR_CAT_GENERAL,$"Uncaught exception in {nameof(Process)}: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            if (Node.HasAttribute(GIS_FUSE_RATING))
                Node.RemoveAttribute(GIS_FUSE_RATING);
            if (Node.HasAttribute(GIS_SWITCH_TYPE))
                Node.RemoveAttribute(GIS_SWITCH_TYPE);
            if (Node.HasAttribute(GIS_T1_ASSET))
                Node.RemoveAttribute(GIS_T1_ASSET);
        }

        private string GenerateScadaLinking()
        {
            var status = Enricher.Singleton.GetScadaStatusPointInfo(_name);

            if (status == null)
                return "";

            var rAmps = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Amps RØ");
            var yAmps = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Amps YØ");
            var bAmps = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Amps BØ");
            var kw = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} kW");
            var pf = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} PF");
            var s1RYVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts RY");
            var s1YBVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts YB");
            var s1BRVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts BR");
            var s2RYVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts RY2");
            var s2YBVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts YB2");
            var s2BRVolts = Enricher.Singleton.GetScadaAnalogPointInfo($"{_name} Volts BR2");

            return  $"<element type=\"SCADA\" id=\"{_id}\" p1State=\"{status?.Key}\" p2State=\"{status?.Key}\" p3State=\"{status?.Key}\" preventUICtrlTag=\"\" tripFaultSuppress=\"\" OCPModeNormal=\"\" OCPModeNormalState=\"\" p1TripFaultSignal=\"\" p2TripFaultSignal=\"\" p3TripFaultSignal=\"\" p1FaultInd=\"\" p2FaultInd=\"\" p3FaultInd=\"\" p1FaultInd2=\"\" p2FaultInd2=\"\" p3FaultInd2=\"\" s1p1KV=\"{s1RYVolts?.Key}\" s1p2KV=\"{s1YBVolts?.Key}\" s1p3KV=\"{s1BRVolts?.Key}\" s2p1KV=\"{s2RYVolts?.Key}\" s2p2KV=\"{s2YBVolts?.Key}\" s2p3KV=\"{s2BRVolts?.Key}\" s1p1KW=\"\" s1p2KW=\"\" s1p3KW=\"\" s1AggregateKW=\"\" s1AggregateKWUCF=\"1\" s2p1KW=\"\" s2p2KW=\"\" s2p3KW=\"\" s2AggregateKW=\"\" s1p1KVAR=\"\" s1p2KVAR=\"\" s1p3KVAR=\"\" s1AggregateKVAR=\"\" s1AggregateKVARUCF=\"1\" s2p1KVAR=\"\" s2p2KVAR=\"\" s2p3KVAR=\"\" s2AggregateKVAR=\"\" s1p1KVA=\"\" s1p2KVA=\"\" s1p3KVA=\"\" s1AggregateKVA=\"\" s2p1KVA=\"\" s2p2KVA=\"\" s2p3KVA=\"\" s2AggregateKVA=\"\" s1p1PF=\"\" s1p2PF=\"\" s1p3PF=\"\" s1AggregatePF=\"\" s2p1PF=\"\" s2p2PF=\"\" s2p3PF=\"\" s2AggregatePF=\"\" s1p1Amps=\"{rAmps?.Key}\" s1p2Amps=\"{yAmps?.Key}\" s1p3Amps=\"{bAmps?.Key}\" s1AggregateAmps=\"\" s2p1Amps=\"\" s2p2Amps=\"\" s2p3Amps=\"\" s2AggregateAmps=\"\" p1FaultCurrent=\"\" p2FaultCurrent=\"\" p3FaultCurrent=\"\" s1p1Angle=\"\" s1p2Angle=\"\" s1p3Angle=\"\" s2p1Angle=\"\" s2p2Angle=\"\" s2p3Angle=\"\" s1VoltageReference=\"{_baseKv}\" s1p1AmpsUCF=\"1\" s1p2AmpsUCF=\"1\" s1p3AmpsUCF=\"1\" s2p1AmpsUCF=\"1\" s2p2AmpsUCF=\"1\" s2p3AmpsUCF=\"1\" />";
                
        }

        #region Switch Type Processing

        private void ProcessSwitch()
        {
            /*
            _baseKv = "";
            _bidirectional = "";
            _forwardTripAmps = "";
            _ganged = "";
            _loadBreakCapable = "";
            _maxInterruptAmps = "";
            _ratedAmps = "";
            _ratedKv = "";
            _reverseTripAmps = "";
            _switchType = "";
            */
            Warn(ERR_CAT_SWITCH,  "Wasn't expecting this function to be used");
        }

        private void ProcessCircuitBreaker2(bool internals)
        {
            if (!string.IsNullOrEmpty(_t1assetno)) {
                var asset = Enricher.Singleton.GetT1HvCircuitBreakerByAssetNumber(_t1assetno);
                if (asset != null)
                {
                    ProcessCircuitBreaker(internals);
                    return;
                }
                asset = Enricher.Singleton.GetT1RingMainUnitByT1AssetNumber(_t1assetno);
                if (asset != null)
                {
                    ProcessRingMainCb();
                    return;
                }
                Error(ERR_CAT_BREAKER, $"T1 asset number [{_t1assetno}] did not match HV Breaker or RMU asset");
            }
            else
            {
                Error(ERR_CAT_BREAKER, "T1 asset number not set");
            }

            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;

            ProcessCircuitBreakerAdms();

            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;

            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD, scale);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, scale);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, scale);
            }
        }

        private void ProcessRingMainFuseSwitch()
        {
            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH; //TODO

            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_RINGMAINFUSESWITCH, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1RingMainUnitByT1AssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error(ERR_CAT_RINGMAINFUSESWITCH, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }

            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH_QUAD, IDF_SCALE_INTERNALS);
                else
                    Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH, IDF_SCALE_INTERNALS);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH, IDF_SCALE_INTERNALS);
            }
        }

        private void ProcessRingMainSwitch()
        {
            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH;
            
            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_RINGMAINSWITCH, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1RingMainUnitByT1AssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    //_maxInterruptAmps = "";
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error(ERR_CAT_RINGMAINSWITCH, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }
            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_SWITCH_QUAD, IDF_SCALE_INTERNALS);
                else
                    Processor.SetSymbolName(_id, SYMBOL_SWITCH, IDF_SCALE_INTERNALS);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_SWITCH, IDF_SCALE_INTERNALS);
            }
        }
        
        private void ProcessRingMainCb()
        {
            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;

            //TODO: generally these won't be in the protection spreadsheet, will have generic settings
            //TODO: how to link with the tx size?
            ProcessCircuitBreakerAdms();

            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_RINGMAINCB, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1RingMainUnitByT1AssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string, true);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SW1] as string, asset[T1_SWITCH_SW2] as string, asset[T1_SWITCH_SW3] as string, asset[T1_SWITCH_SW4] as string);
                }
                else
                {
                    Error(ERR_CAT_RINGMAINCB, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }
            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD, IDF_SCALE_INTERNALS);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, IDF_SCALE_INTERNALS);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, IDF_SCALE_INTERNALS);
            }
        }

        private void ProcessLVSwitch()
        {
            if (!string.IsNullOrEmpty(_t1assetno))
                Warn(ERR_CAT_LVSWITCH, $"T1 asset number [{_t1assetno}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = FALSE; //TODO check
            _loadBreakCapable = TRUE; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IDF_SWITCH_TYPE_SWITCH;
            Processor.SetSymbolName(_id, SYMBOL_LVSWITCH);
        }

        private void ProcessLVFuse()
        {
            if (!string.IsNullOrEmpty(_t1assetno))
                Warn(ERR_CAT_LVFUSE,  $"T1 asset number [{_t1assetno}] is not unset");
            //_bidirectional = "";
            //_forwardTripAmps = "";
            _ganged = FALSE; //TODO check
            _loadBreakCapable = TRUE; //TODO check
            //_maxInterruptAmps = "";
            //_ratedAmps = "";
            //_ratedKv = "";
            //_reverseTripAmps = "";
            _switchType = IDF_SWITCH_TYPE_FUSE;
            Processor.SetSymbolName(_id, SYMBOL_LVFUSE);
        }

        private void ProcessFuseSaver()
        {
            //TODO: need a way to link a fuse saver to the corresponding fuse
            //TODO: consider cases where there is no fuse involved?

            /*
            _forwardTripAmps = "";
            _reverseTripAmps = "";
            */

            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;

            //ProcessCircuitBreakerAdms();

            _switchType = IDF_SWITCH_TYPE_RECLOSER;
            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_BREAKER, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1HvCircuitBreakerByAssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error(ERR_CAT_BREAKER, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }
            Processor.SetSymbolName(_id, SYMBOL_FUSESAVER);
        }

        private void ProcessEntec()
        {
            //_bidirectional //TODO
            //TODO: need to get sectionliser function from AdmsDatabase
            _ganged = TRUE;
            _switchType = IDF_SWITCH_TYPE_SWITCH;
            
            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_ENTEC, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1DisconnectorByAssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? FALSE : TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error(ERR_CAT_ENTEC,  $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }
            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_ENTEC_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_ENTEC);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_ENTEC);
            }
        }

        private void ProcessHVLinks(bool internals)
        {
            _ganged = FALSE;
            _loadBreakCapable = FALSE;//TODO
            _ratedAmps = "300";//confirmed by robert
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_LINKS, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1FuseByAssetNumber(_t1assetno);

                if (asset == null)
                {
                    Error(ERR_CAT_LINKS, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }
                else
                {
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                    ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                }
            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            Processor.SetSymbolName(_id, SYMBOL_LINKS, scale);
        }

        private void ProcessDisconnector(bool internals)
        {
            _ganged = TRUE;          
            _switchType = IDF_SWITCH_TYPE_SWITCH;

            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_DISCONNECTOR, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1DisconnectorByAssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _loadBreakCapable = ValidateLoadBreakRating(asset[T1_SWITCH_LOAD_BREAK_RATING] as string) == "" ? FALSE : TRUE;
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error(ERR_CAT_DISCONNECTOR, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR_QUAD, scale);
                else
                    Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR, scale);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR, scale);
            }
        }
     
        private void ProcessCircuitBreaker(bool internals)
        {
            _bidirectional = TRUE;
            _ganged = TRUE;
            _loadBreakCapable = TRUE;

            ProcessCircuitBreakerAdms();
            
            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_BREAKER,  $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1HvCircuitBreakerByAssetNumber(_t1assetno);

                if (asset != null)
                {
                    _ratedAmps = ValidatedRatedAmps(asset[T1_SWITCH_RATED_AMPS] as string);
                    _maxInterruptAmps = ValidateMaxInterruptAmps(asset[T1_SWITCH_MAX_INTERRUPT_AMPS] as string);
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error(ERR_CAT_BREAKER, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }

            }

            var scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            var p = Enricher.Singleton.GetScadaStatusPointInfo(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD, scale);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, scale);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER, scale);
            }
        }

        private void ProcessCircuitBreakerAdms()
        {
            if (Enricher.Singleton.GetAdmsSwitch(_name) is DataRow asset)
            {
                //TODO: RMU circuit breakers are generally not in the protection database... they use generic settings based on tx size.
                //how are we going to handle these?
                //TODO: validation on these?
                if (_name == "P45")
                    Debugger.Break();
                _forwardTripAmps = (asset[ADMS_SWITCH_FORWARDTRIPAMPS] as int?).ToString();
                _reverseTripAmps = (asset[ADMS_SWITCH_REVERSETRIPAMPS] as int?).ToString();
                _switchType = asset[ADMS_SWITCH_RECLOSER_ENABLED] as string == "Y" ? IDF_SWITCH_TYPE_RECLOSER : IDF_SWITCH_TYPE_BREAKER;
            }
            else
            {
                Warn(IDF_SWITCH_TYPE_BREAKER,  "Breaker not in Adms database");
                _switchType = IDF_SWITCH_TYPE_BREAKER;
            }
        }

        private void ProcessHVFuse(bool internals)
        {
            _bidirectional = TRUE;
            _forwardTripAmps = _reverseTripAmps = ValidateFuseTrip(_fuserating);
            _ganged = FALSE;
            _loadBreakCapable = FALSE;
            _maxInterruptAmps = "10000";//TODO check with sjw
            _ratedAmps = _forwardTripAmps == "" ? "" : (int.Parse(_forwardTripAmps) / 2).ToString();
            _switchType = IDF_SWITCH_TYPE_FUSE;

            DataRow asset = null;
            if (_t1assetno == "")
            {
                Error(ERR_CAT_FUSE, $"No T1 asset number assigned");
            }
            else
            {
                asset = Enricher.Singleton.GetT1FuseByAssetNumber(_t1assetno);

                if (asset != null)
                {
                    if (asset[T1_FUSE_GANGED] as string == "2")
                    {
                        _ganged = TRUE;
                    }
                    _ratedKv = ValidateRatedVoltage(_baseKv, asset[T1_SWITCH_RATED_VOLTAGE] as string);
                    ValidateSwitchNumber(asset[T1_SWITCH_SWNUMBER] as string);
                }
                else
                {
                    Error(ERR_CAT_FUSE, $"T1 asset number [{_t1assetno}] wasn't in T1");
                }
            }
            double scale = internals ? IDF_SCALE_INTERNALS : IDF_SCALE_GEOGRAPHIC;
            Processor.SetSymbolName(_id, SYMBOL_FUSE, scale);
        }

        private void ProcessServiceFuse()
        {
            _bidirectional = TRUE;
            _forwardTripAmps = _reverseTripAmps = "";
            _ganged = FALSE;
            _loadBreakCapable = FALSE;
            _maxInterruptAmps = "";//TODO check with sjw
            _ratedAmps = "";//TODO?
            _switchType = "Fuse";
            _ratedKv = ValidateRatedVoltage(_baseKv, _ratedKv as string);
            Processor.SetSymbolName(_id, SYMBOL_SERVICE_FUSE);
        }
        #endregion

        #region Validation Routines
        /// <summary>
        /// Checks the rated voltage is greater than the operating voltage, and returns the validated rated voltage
        /// </summary>
        /// <param name="opVoltage"></param>
        /// <param name="ratedVoltage"></param>
        /// <returns></returns>
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
                        Error(ERR_CAT_GENERAL, $"Rated voltage [{ratedVoltage}] is less the operating voltage [{opVoltage}], setting to 110% of operating voltage");
                    }
                }
                else
                {
                    Error(ERR_CAT_GENERAL,$"Could not parse rated voltage [{ratedVoltage}], setting to 110% of operating voltage");
                }
                return (iOpVoltage * 1.1).ToString();
            }
            catch
            {
                Error(ERR_CAT_GENERAL,$"Operating voltage [{opVoltage}] is not a valid float");
                return opVoltage;
            }
        }

        private string ValidateLoadBreakRating(string amps)
        {
            if (string.IsNullOrEmpty(amps))
            {
                Info(ERR_CAT_GENERAL, "T1 load break rating is unset");
                return "";
            }
            if (int.TryParse(amps, out var res))
            {
                return res.ToString();
            }
            else
            {
                Warn(ERR_CAT_GENERAL, "Couldn't parse T1 load break rating");
                return "";
            }
        }

        private string ValidateMaxInterruptAmps(string amps)
        {
            if (string.IsNullOrEmpty(amps))
            {
                Info(ERR_CAT_GENERAL, $"T1 max interrupt amps is unset");
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
                Warn(ERR_CAT_GENERAL,  $"Could not parse T1 max interrupt amps [{amps}]");
                return "";
            }
        }

        private void ValidateSwitchNumber(string swno, params string[] swnos)
        {
            if (swno == _name)
                return;
            if (swnos != null)
            {
                if (swnos.Length > 0)
                {
                    foreach (var sw in swnos)
                    {
                        if (sw == _name)
                            return;
                    }
                    Error(ERR_CAT_GENERAL,  $"T1 switch number [{swno}:{string.Join(":", swnos)}] doesn't match GIS switch number [{_name}]");
                    return;
                }
            }
            Error(ERR_CAT_GENERAL, $"T1 switch number [{swno}] doesn't match GIS switch number [{_name}]");

        }

        private string ValidatedRatedAmps(string amps, bool breaker = false)
        {
            //format of rated amps is either an integer, or two integers i1/i2 for breaker/switch ratings of RMUs

            //handle the simple case
            if (string.IsNullOrEmpty(amps))
            {
                Info(ERR_CAT_GENERAL, "T1 rated amps is unset");
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
                Warn(ERR_CAT_GENERAL, $"Could not parse T1 rated amps [{amps}]");
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
                Info(ERR_CAT_FUSE,  "GIS fuse rating is unset");
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
                Warn(ERR_CAT_FUSE,  $"Could not parse GIS fuse rating [{fuserating}]");
                return "";
            }
        }
        #endregion

        #region OVerrides
        public override string ToString()
        {
            return Node.OuterXml;
        }

        protected override void Debug(string code,  string message)
        {
            _log.Debug($"SWITCH\\{code},{_id},{_name},\"{message}\"");
        }

        protected override void Info(string code,  string message)
        {
            _log.Info($"SWITCH\\{code},{_id},{_name},\"{message}\"");
        }

        protected override void Warn(string code,  string message)
        {
            _log.Warn($"SWITCH\\{code},{_id},{_name},\"{message}\"");
        }

        protected override void Error(string code,  string message)
        {
            _log.Error($"SWITCH\\{code},{_id},{_name},\"{message}\"");
        }
        #endregion
    }
}
