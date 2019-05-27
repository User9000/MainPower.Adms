using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const string T1_TX_PHASES= "No.of Phases";                    //
        private const string T1_TX_KVA = "Rated Power kVA";                   //maximum rated power
        private const string T1_TX_PRI_AMPS = "Rated Current (Prim)";         //
        private const string T1_TX_SEC_AMPS = "Rated Current (Sec)";          //
        private const string T1_TX_SEC_RATEDKV = "Op Voltage (Sec)";          //the secondary operating voltage
        private const string T1_TX_SEC_OPERATINGKV = "Rated Voltage (Sec)";   //the base secondary voltage

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
        private const string IDF_TX_INITIaLTAP2 = "initialTap2";
        private const string IDF_TX_INITIaLTAP3 = "initialTap3";
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
                        //_initialTap1 = _initialTap2 = _initialTap3
                        //_s1ConnectionType
                        //_s2ConnectionType
                        //_s1RatedKv
                        //_s2RatedKv
                        //GenerateTransformerType();
                    }
                    asset = Enricher.Singleton.GetAdmsTransformerByAssetNumber(_t1assetno);
                    if (asset != null)//not being in the adms database is not an error
                    {
                        //TODO process data from adms database
                        //_bandwidth = ValidateBandwidth(asset[ADMS_TX_BANDWIDTH] as string);
                        //_desiredVoltage
                        //_maxTapLimit
                        //_minTapLimit
                        //_parallelSet
                        //_regulationType
                        //_s1BaseKv;
                        //_s2BaseKv;
                        //GenerateScadaLinking();
                    }
                }
                

                Node.SetAttribute(IDF_TX_BANDWIDTH, _bandwidth);
                Node.SetAttribute(IDF_TX_BIDIRECTIONAL, _bidirectional);
                Node.SetAttribute(IDF_TX_CONTROLPHASE, _controlPhase);
                Node.SetAttribute(IDF_TX_DESIREDVOLTAGE, _desiredVoltage);
                Node.SetAttribute(IDF_TX_INITIALTAP1, _initialTap1);
                Node.SetAttribute(IDF_TX_INITIaLTAP2, _initialTap2);
                Node.SetAttribute(IDF_TX_INITIaLTAP3, _initialTap3);
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
                Debug(ERR_CAT_GENERAL, $"Uncaught exception in {nameof(Process)}: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            //throw new NotImplementedException();
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
