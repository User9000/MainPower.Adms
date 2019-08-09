using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Line : Element
    {
        public Line(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                //TODO: Backport to GIS Extractor
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_INSUBSTATION, IDF_FALSE);

                ParentGroup.AddDatalink(Id);

                if (Node.Attribute("baseKV").Value == "0.2300")
                {
                    Node.SetAttributeValue("baseKV", "0.4000");
                    Debug("Overriding base voltage from 230V to 400V");
                }
                if (!Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Line))
                {
                    /*
                    UpdateId(Id + Util.RandomString(4));
                    Warn("Failed to add line to model, trying again with new id");
                    Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Line);
                    */
                    Warn("Failed to add line to model, imma just gonna delete it and hope for the best 😬");
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
