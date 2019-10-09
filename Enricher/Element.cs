using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// 
    /// </summary>
    abstract internal class Element : ErrorReporter
    {

        public XElement Node { get; private set; }
        public Group ParentGroup { get; private set; }
        public string Id { get; private set; }
        public string Name { get; protected set; }
        public string T1Id { get; protected set; }

        protected const string IDF_ELEMENT_NAME = "name";
        protected const string IDF_ELEMENT_ID = "id";
        protected const string IDF_ELEMENT_AOR_GROUP = "aorGroup";
        protected const string IDF_DEVICE_NOMSTATE1 = "nominalState1";
        protected const string IDF_DEVICE_NOMSTATE2 = "nominalState2";
        protected const string IDF_DEVICE_NOMSTATE3 = "nominalState3";
        protected const string IDF_DEVICE_INSUBSTATION = "inSubstation";
        protected const string IDF_DEVICE_BASEKV = "baseKV";

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

        internal Element(XElement node, Group processor)
        {
            Node = node;
            ParentGroup = processor;
            Id = node.Attribute(IDF_ELEMENT_ID).Value;
            Name = node.Attribute(IDF_ELEMENT_NAME).Value;
            //TODO
            node.SetAttributeValue("aorGroup", "1");
        }

        protected void CheckPhasesSide1Only()
        {
            if (string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID1)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID2)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID3)?.Value))
            {
                Error("All phases are belong to null, now it 3 phase");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "1");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "3");
            }
        }

        protected void CheckPhases()
        {
            if (string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID1)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID2)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID3)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID1)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID2)?.Value) &&
                string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID3)?.Value))
            {
                Error("All phases are belong to null, now it 3 phase");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "1");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "3");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "1");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "2");
                Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "3");
            }
        }


        protected void UpdateId(string id)
        {
            Node.SetAttributeValue(IDF_ELEMENT_ID, id);
            Id = id;
        }

        abstract internal void Process();

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
    }

}
