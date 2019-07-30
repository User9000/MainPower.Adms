using System;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
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
            }
            catch (Exception ex)
            {
                Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
