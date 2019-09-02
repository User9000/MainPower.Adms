using System;
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
                ParentGroup.AddMissingPhases(Node);

                //TODO: Backbort to GIS Extractor
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                
                //TODO: Backport to GIS Extractor
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_INSUBSTATION, IDF_FALSE);

                //TODO: Backport to GIS Extractor
                ParentGroup.AddDatalink(Id);

                var geo = ParentGroup.GetLineGeometry(Id);

                //TODO: Backport to GIS Extractor
                if (Node.Attribute(IDF_DEVICE_BASEKV).Value == "0.2300")
                {
                    Node.SetAttributeValue(IDF_DEVICE_BASEKV, "0.4000");
                    Debug("Overriding base voltage from 230V to 400V");
                }
                if (!Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Line, geo))
                {
                    Error("Failed to add line to model -> deleted 😬");
                    Node.Remove();
                }
            }
            catch (Exception ex)
            {
                Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
