using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// 
    /// </summary>
    abstract public class IdfElement : ErrorReporter
    {
        public XElement Node { get; private set; }
        public IdfGroup ParentGroup { get; private set; }
        public string Id { get; private set; }
        public string Name { get; protected set; }
        public string T1Id { get; protected set; }
        public int S1Phases { get; protected set; } = 0;
        public int S2Phases { get; protected set; } = 0;
        
        #region Common IDF attributes
        protected const string IDF_ELEMENT_NAME = "name";
        protected const string IDF_ELEMENT_ID = "id";
        protected const string IDF_ELEMENT_AOR_GROUP = "aorGroup";
        protected const string IDF_DEVICE_NOMSTATE1 = "nominalState1";
        protected const string IDF_DEVICE_NOMSTATE2 = "nominalState2";
        protected const string IDF_DEVICE_NOMSTATE3 = "nominalState3";
        protected const string IDF_DEVICE_INSUBSTATION = "inSubstation";
        protected const string IDF_DEVICE_BASEKV = "baseKV";
        protected const string IDF_DEVICE_RATEDKV = "ratedKV";

        protected const string IDF_DEVICE_S1_PHASEID1 = "s1phaseID1";
        protected const string IDF_DEVICE_S1_PHASEID2 = "s1phaseID2";
        protected const string IDF_DEVICE_S1_PHASEID3 = "s1phaseID3";
        protected const string IDF_DEVICE_S2_PHASEID1 = "s2phaseID1";
        protected const string IDF_DEVICE_S2_PHASEID2 = "s2phaseID2";
        protected const string IDF_DEVICE_S2_PHASEID3 = "s2phaseID3";
        protected const string IDF_DEVICE_S1_NODE = "s1node";
        protected const string IDF_DEVICE_S2_NODE = "s2node";

        protected const string GIS_T1_ASSET = "mpwr_t1_asset_nbr";
        protected const string GIS_SWITCH_TYPE = "mpwr_gis_switch_type";
        protected const string GIS_FUSE_RATING = "mpwr_fuse_rating";

        protected const string IDF_TRUE = "True";
        protected const string IDF_FALSE = "False";

        protected const string AOR_DEFAULT = "1";

        protected const string SCADA_NAME = "Name";

        #endregion

        public IdfElement(XElement node, IdfGroup processor)
        {
            Node = node;
            ParentGroup = processor;
            Id = node.Attribute(IDF_ELEMENT_ID).Value;
            Name = node.Attribute(IDF_ELEMENT_NAME).Value;
            T1Id = Node.Attribute(GIS_T1_ASSET)?.Value;

            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID1)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID2)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID3)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID1)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID2)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID3)?.Value)) S2Phases++;

            //TODO
            node.SetAttributeValue("aorGroup", "1");
        }
        protected void CheckPhasesSide1Only()
        {
            if (S1Phases == 0)
            {
                Error("All side 1 phases are null, defaulted to 3 phase");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "1");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "3");
            }
        }

        protected void CheckPhases()
        {
            CheckPhasesSide1Only();
            if (S2Phases == 0)
            {
                Error("All side 2 phases are null, defaulted to 3 phase");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "1");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "2");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "3");
            }
        }

        protected void SetAllNominalStates()
        {
            Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, "True");
            Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, "True");
            Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, "True");
        }

        protected void UpdateId(string id)
        {
            Node.SetAttributeValue(IDF_ELEMENT_ID, id);
            Id = id;
        }

        protected void GenerateDeviceInfo()
        {
            XElement dinfo = new XElement("element");
            dinfo.SetAttributeValue("type", "Device Info");
            dinfo.SetAttributeValue("id", $"{Id}_deviceInfo");
            dinfo.SetAttributeValue("key1", "T1 Asset Id");
            dinfo.SetAttributeValue("value1", T1Id ?? "unknown");

            ParentGroup.AddGroupElement(dinfo);

            Node.SetAttributeValue("deviceInfo", $"{Id}_deviceInfo");
        }

        abstract public void Process();

        #region Logging Overrides
        protected override void Debug(string message, [CallerMemberName]string caller = "")
        {
            Debug(message, Id, $"{Name}:{T1Id}", caller);
        }
        protected override void Info(string message, [CallerMemberName]string caller = "")
        {
            Info(message, Id, $"{Name}:{T1Id}", caller);
        }
        protected override void Warn(string message, [CallerMemberName]string caller = "")
        {
            Warn(message, Id, $"{Name}:{T1Id}", caller);
        }
        protected override void Error(string message, [CallerMemberName]string caller = "")
        {
            Error(message, Id, $"{Name}:{T1Id}", caller);
        }
        protected override void Fatal(string message, [CallerMemberName]string caller = "")
        {
            Fatal(message, Id, $"{Name}:{T1Id}", caller);
        }
        #endregion
    }

}
