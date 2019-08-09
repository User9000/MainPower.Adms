using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Region : Element
    {
        public Region(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

