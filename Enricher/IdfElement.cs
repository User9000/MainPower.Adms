using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// 
    /// </summary>
    abstract public class IdfElement : ErrorReporter
    {
        /// <summary>
        /// The underlying IDF XElement
        /// </summary>
        public XElement Node { get; private set; }

        /// <summary>
        /// The IDF group that this IdfElement belongs to
        /// </summary>
        public IdfGroup ParentGroup { get; private set; }

        /// <summary>
        /// The IDF id
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The IDF name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The Tech1 asset number
        /// </summary>
        public string T1Id { get; protected set; }

        /// <summary>
        /// The number of phases connected on side 1
        /// </summary>
        public int S1Phases { get; protected set; } = 0;

        /// <summary>
        /// The number of phases connected on side 2
        /// </summary>
        public int S2Phases { get; protected set; } = 0;
        
        #region Common IDF attributes
        protected const string IdfElementName = "name";
        protected const string IdfElementId = "id";
        protected const string IdfElementAorGroup = "aorGroup";
        protected const string IdfDeviceNomState = "nominalState1";
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

        /// <summary>
        /// Extracts attributes common to all idf elements
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parentGroup"></param>
        public IdfElement(XElement node, IdfGroup parentGroup)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            ParentGroup = parentGroup;
            Id = node.Attribute(IdfElementId).Value;
            Name = node.Attribute(IdfElementName).Value;
            T1Id = Node.Attribute(GIS_T1_ASSET)?.Value;

            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID1)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID2)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID3)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID1)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID2)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID3)?.Value)) S2Phases++;

            //TODO: to be removed
            node.SetAttributeValue("aorGroup", "1");
        }

        /// <summary>
        /// Check that at least one phase is set on side 1
        /// TODO: to be removed
        /// </summary>
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

        /// <summary>
        /// Check that all at least one phase is set
        /// TODO: to be removed
        /// </summary>
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

        /// <summary>
        /// Set all the nominal state attribues to True
        /// TODO: to be removed
        /// </summary>
        protected void SetAllNominalStates()
        {
            Node.SetAttributeValue(IdfDeviceNomState, "True");
            Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, "True");
            Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, "True");
        }

        /// <summary>
        /// Updates the id attribute
        /// </summary>
        /// <param name="id"></param>
        protected void UpdateId(string id)
        {
            Node.SetAttributeValue(IdfElementId, id);
            Id = id;
        }

        /// <summary>
        /// Updates the name attribute
        /// </summary>
        /// <param name="name"></param>
        protected void UpdateName(string name)
        {
            Node.SetAttributeValue(IdfElementName, name);
            Name = name;
        }

        /// <summary>
        /// Generates a Device Info idf element for this device
        /// </summary>
        /// <param name="items">A list of (string, string) tuples with additional key-value pairs to add.  Up to three additional kvps can be added.</param>
        protected void GenerateDeviceInfo(List<(string key, string value)> items = null)
        {
            XElement dinfo = new XElement("element");
            dinfo.SetAttributeValue("type", "Device Info");
            dinfo.SetAttributeValue("id", $"{Id}_deviceInfo");
            dinfo.SetAttributeValue("key1", "Id");
            dinfo.SetAttributeValue("value1", Id);
            dinfo.SetAttributeValue("key2", "T1 Asset Id");
            dinfo.SetAttributeValue("value2", T1Id ?? "unknown");
            if (items != null)
            {
                for (int i = 0; i < items.Count && i < 3; i++)
                {
                    dinfo.SetAttributeValue($"key{i+3}", items[i].key);
                    dinfo.SetAttributeValue($"value{i+3}", items[i].value);
                }
            }
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
