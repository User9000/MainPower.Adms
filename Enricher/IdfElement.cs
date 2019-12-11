using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// TODO: subclass IdfDevice to remove all the extra stuff that isn't relevant to groups etc
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
        /// The Node1 Id
        /// </summary>
        public string Node1Id { get; private set; }

        /// <summary>
        /// The Node2 Id
        /// </summary>
        public string Node2Id { get; private set; }


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
        protected const string IdfDeviceNomState1 = "nominalState1";
        protected const string IdfDeviceNomState2 = "nominalState2";
        protected const string IdfDeviceNomState3 = "nominalState3";
        protected const string IdfDeviceInSubstation = "inSubstation";
        protected const string IdfDeviceBasekV = "baseKV";
        protected const string IdfDeviceRatedkV = "ratedKV";
        protected const string IdfDeviceVoltageType = "voltageType";
        protected const string IdfDeviceVoltageReference = "voltageReference";
        protected const string IdfEl = "element";
        protected const string IdfElementType = "type";
        protected const string IdfElementTypeScada = "SCADA";
        protected const string IdfDeviceP1kV = "p1KV";
        protected const string IdfDeviceP2kV = "p2KV";
        protected const string IdfDeviceP3kV = "p3KV";
        protected const string IdfDeviceS1PhaseId1 = "s1phaseID1";
        protected const string IdfDeviceS1PhaseId2 = "s1phaseID2";
        protected const string IdfDeviceS1PhaseId3 = "s1phaseID3";
        protected const string IdfDeviceS2PhaseId1 = "s2phaseID1";
        protected const string IdfDeviceS2PhaseId2 = "s2phaseID2";
        protected const string IdfDeviceS2PhaseId3 = "s2phaseID3";
        protected const string IdfDeviceS1Node = "s1node";
        protected const string IdfDeviceS2Node = "s2node";
        protected const string IdfViolations = "violations";
        protected const string IdfLimits = "limits";

        protected const string GisT1Asset = "mpwr_t1_asset_nbr";
        protected const string GisSwitchType = "mpwr_gis_switch_type";
        protected const string GisFuseRating = "mpwr_fuse_rating";

        protected const string IdfTrue = "True";
        protected const string IdfFalse = "False";
        protected const string AorDefault = "2";
        protected const string ScadaName = "Name";

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
            T1Id = Node.Attribute(GisT1Asset)?.Value;
            Node1Id = Node.Attribute(IdfDeviceS1Node)?.Value ?? "";
            Node2Id = Node.Attribute(IdfDeviceS1Node)?.Value ?? "";

            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId1)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId2)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId3)?.Value)) S1Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS2PhaseId1)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS2PhaseId2)?.Value)) S2Phases++;
            if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS2PhaseId3)?.Value)) S2Phases++;

            //TODO: to be removed
            if (parentGroup != null && !(this is IdfSource))
            {
                node.SetAttributeValue(IdfElementAorGroup, AorDefault);
                node.SetAttributeValue(IdfViolations, IdfTrue);
                //TODO: change this depending on voltage, either HV or LV
                node.SetAttributeValue(IdfLimits, "Limits,System");


            }
        }

        /*
        /// <summary>
        /// Check that at least one phase is set on side 1
        /// TODO: to be removed
        /// </summary>
        protected void CheckPhasesSide1Only()
        {
            if (S1Phases == 0)
            {
                Err("All side 1 phases are null, defaulted to 3 phase");
                Node.SetAttributeValue(IdfDeviceS1PhaseId1, "1");
                Node.SetAttributeValue(IdfDeviceS1PhaseId2, "2");
                Node.SetAttributeValue(IdfDeviceS1PhaseId3, "3");
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
                Err("All side 2 phases are null, defaulted to 3 phase");
                Node.SetAttributeValue(IdfDeviceS2PhaseId1, "1");
                Node.SetAttributeValue(IdfDeviceS2PhaseId2, "2");
                Node.SetAttributeValue(IdfDeviceS2PhaseId3, "3");
            }
        }

        /// <summary>
        /// Set all the nominal state attribues to True
        /// TODO: to be removed
        /// </summary>
        protected void SetAllNominalStates()
        {
            Node.SetAttributeValue(IdfDeviceNomState1, IdfTrue);
            Node.SetAttributeValue(IdfDeviceNomState2, IdfTrue);
            Node.SetAttributeValue(IdfDeviceNomState3, IdfTrue);
        }
        */

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
        protected void GenerateDeviceInfo(List<KeyValuePair<string, string>> items = null)
        {
            //TODO: contantize these strings?
            XElement dinfo = new XElement("element");
            dinfo.SetAttributeValue("type", "Device Info");
            dinfo.SetAttributeValue("id", $"{Id}_deviceInfo");
            dinfo.SetAttributeValue("key1", "Id");
            dinfo.SetAttributeValue("value1", Id);
            dinfo.SetAttributeValue("key2", "Group Id");
            dinfo.SetAttributeValue("value2", ParentGroup.Id);
            dinfo.SetAttributeValue("key3", "T1 Asset Id");
            dinfo.SetAttributeValue("value3", T1Id ?? "unknown");
            if (items != null)
            {
                for (int i = 0; i < items.Count && i < 2; i++)
                {
                    dinfo.SetAttributeValue($"key{i+4}", items[i].Key);
                    dinfo.SetAttributeValue($"value{i+4}", items[i].Value);
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
        protected override void Err(string message, [CallerMemberName]string caller = "")
        {
            Err(message, Id, $"{Name}:{T1Id}", caller);
        }
        protected override void Fatal(string message, [CallerMemberName]string caller = "")
        {
            Fatal(message, Id, $"{Name}:{T1Id}", caller);
        }
        #endregion
    }

}
