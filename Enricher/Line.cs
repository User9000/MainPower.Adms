using System;
using System.Drawing;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Line : Element
    {
        /// <summary>
        /// Represents a IDF Line object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public Line(XElement node, Group parent) : base(node, parent) { }

        /// <summary>
        /// Process the Line object
        /// </summary>
        internal override void Process()
        {
            try
            {
#if !nofixes
                //TODO: Backbort to GIS Extractor
                ParentGroup.AddMissingPhases(Node);
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_INSUBSTATION, IDF_FALSE);

                if (Node.Attribute(IDF_DEVICE_BASEKV).Value == "0.2300")
                {
                    Node.SetAttributeValue(IDF_DEVICE_BASEKV, "0.4000");
                    Debug("Overriding base voltage from 230V to 400V");
                }
#endif
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

                //TODO: Backport to GIS Extractor
                ParentGroup.AddDataAndFlowlink(Id);

                var geo = ParentGroup.GetLineGeometry(Id);
                //TODO: Backport to GIS Extractor
                if (!Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Line, geo))
                {
                    Error("Failed to add line to model -> deleted 😬");
                    Node.Remove();
                }

                //TODO: this should be determined by conductor type
                string voltage = Node.Attribute(IDF_DEVICE_BASEKV).Value;
                switch (voltage) 
                {
                    case "66":
                        Node.SetAttributeValue("ratedKV", "70");
                        ParentGroup.AddColorToLine(Id, Color.DarkBlue);
                        break;
                    case "33":
                        Node.SetAttributeValue("ratedKV", "40");
                        ParentGroup.AddColorToLine(Id, Color.DarkGreen);
                        break;
                    case "22":
                        Node.SetAttributeValue("ratedKV", "30");
                        ParentGroup.AddColorToLine(Id, Color.Orange);
                        break;
                    case "11":
                        Node.SetAttributeValue("ratedKV", "15");
                        ParentGroup.AddColorToLine(Id, Color.Red);
                        break;
                    case "0.4000":
                        Node.SetAttributeValue("ratedKV", "1");
                        ParentGroup.AddColorToLine(Id, Color.Yellow);
                        break;
                    default:
                        Node.SetAttributeValue("ratedKV", "100");
                        ParentGroup.AddColorToLine(Id, Color.Purple);
                        break;
                }

#if !nofixes
                ParentGroup.SetLayerFromVoltage(Id, Node.Attribute(IDF_DEVICE_BASEKV).Value, false);
#endif
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
