using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Capacitor : Element
    {
        /// <summary>
        /// Represents a IDF Capacitor object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public Capacitor(XElement node, Group parent) : base(node, parent) { }

        /// <summary>
        /// Process the Capacitor object
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

                var geo = ParentGroup.GetSymbolGeometry(Id);

                //TODO: Backport to GIS Extractor
                if (Node.Attribute(IDF_DEVICE_BASEKV).Value == "0.2300")
                {
                    Node.SetAttributeValue(IDF_DEVICE_BASEKV, "0.4000");
                    Debug("Overriding base voltage from 230V to 400V");
                }

                Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.ShuntCapacitor, geo);

                ParentGroup.SetLayerFromVoltage(Id, Node.Attribute(IDF_DEVICE_BASEKV).Value);
            }
            catch (Exception ex)
            {
                Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
