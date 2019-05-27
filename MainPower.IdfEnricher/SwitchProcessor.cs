using System;
using System.Collections.Generic;
using System.Data;
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
        private string _bidirectional = "";
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
                        ProcessDisconnector();
                        _ganged = FALSE;
                        _loadBreakCapable = FALSE;
                        break;
                    case @"MV Isolator\HV Fuse":
                        ProcessHVFuse();
                        break;
                    case @"MV Isolator\Circuit Breaker - Substation Feeder":
                        ProcessCircuitBreaker();
                        break;
                    case @"MV Isolator\Circuit Breaker - Substation General":
                        ProcessCircuitBReaker2();
                        break;
                    case @"MV Isolator\Air Break Switch":
                        ProcessDisconnector();
                        break;
                    case @"MV Isolator\MV Switch":
                        ProcessSwitch();
                        break;
                    case @"MV Isolator\Disconnector":
                        ProcessDisconnector();
                        break;
                    case @"MV Isolator\HV Fuse Switch":
                        ProcessRingMainFuseSwitch();
                        break;
                    //MV Line Switch
                    case @"MV Line Switch\Circuit Breaker - Line":
                        ProcessCircuitBreaker();
                        break;
                    case @"MV Line Switch\HV Link":
                        ProcessHVLinks();
                        break;
                    case @"MV Line Switch\HV Fuse":
                        ProcessHVFuse();
                        break;
                    case @"MV Line Switch\Automated LBS":
                        ProcessEntec();
                        break;
                    case @"MV Line Switch\Disconnector":
                        ProcessDisconnector();
                        break;
                    case @"MV Line Switch\Fuse Saver":
                        ProcessFuseSaver();
                        break;
                    case @"MV Line Switch\MV Gas Switch":
                        //TODO
                        break;
                    case @"MV Line Switch\HV Tri - Fuse":
                        ProcessHVFuse();
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
                GenerateScadaLinking();
                RemoveExtraAttributes();
                Debug("SWITCH",  ToString());
            }
            catch (Exception ex)
            {
                Debug(ERR_CAT_GENERAL,$"Uncaught exception in {nameof(Process)}: {ex.Message}");
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

        private void GenerateScadaLinking()
        {
            Debug(ERR_CAT_SCADA,  "Not implemented yet");
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

        private void ProcessCircuitBReaker2()
        {
            if (!string.IsNullOrEmpty(_t1assetno)) {
                var asset = Enricher.Singleton.GetT1HvCircuitBreakerByAssetNumber(_t1assetno);
                if (asset != null)
                {
                    ProcessCircuitBreaker();
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
            
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_HVFUSESWITCH);
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_SWITCH_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_SWITCH);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_SWITCH);
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
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

        private void ProcessHVLinks()
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
            Processor.SetSymbolName(_id, SYMBOL_LINKS);
        }

        private void ProcessDisconnector()
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_DISCONNECTOR);
            }
        }
     
        private void ProcessCircuitBreaker()
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
            var p = Enricher.Singleton.GetScadaStatusSwitchStatusInfoBySwitchNumber(_name);
            if (p != null)
            {
                if (p.QuadState)
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER_QUAD);
                else
                    Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
            }
            else
            {
                Processor.SetSymbolName(_id, SYMBOL_CIRCUITBREAKER);
            }
        }

        private void ProcessCircuitBreakerAdms()
        {
            if (Enricher.Singleton.GetAdmsSwitch(_name) is DataRow asset)
            {
                //TODO: RMU circuit breakers are generally not in the protection database... they use generic settings based on tx size.
                //how are we going to handle these?
                //TODO: validation on these?
                _forwardTripAmps = asset[ADMS_SWITCH_FORWARDTRIPAMPS] as string;
                _reverseTripAmps = asset[ADMS_SWITCH_REVERSETRIPAMPS] as string;
                _switchType = asset[ADMS_SWITCH_RECLOSER_ENABLED] as string == "Y" ? IDF_SWITCH_TYPE_RECLOSER : IDF_SWITCH_TYPE_BREAKER;
            }
            else
            {
                Warn(IDF_SWITCH_TYPE_BREAKER,  "Breaker not in Adms database");
                _switchType = IDF_SWITCH_TYPE_BREAKER;
            }
        }

        private void ProcessHVFuse()
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
            Processor.SetSymbolName(_id, SYMBOL_FUSE);
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
                    Error(ERR_CAT_GENERAL,  $"T1 switch number [{swno}:{string.Join(":", swnos)}] doesnt match GIS switch number {_name}");
                    return;
                }
            }
            Error(ERR_CAT_GENERAL, $"T1 switch number [{swno}] doesnt match GIS switch numbe {_name}");

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
            _log.Debug($"SWITCH,{code},{_id}/{_name},{message}");
        }

        protected override void Info(string code,  string message)
        {
            _log.Info($"SWITCH,{code},{_id}/{_name},{message}");
        }

        protected override void Warn(string code,  string message)
        {
            _log.Warn($"SWITCH,{code},{_id}/{_name},{message}");
        }

        protected override void Error(string code,  string message)
        {
            _log.Error($"SWITCH,{code},{_id}/{_name},{message}");
        }
        #endregion
    }
}
