using System;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Area : Element
    {
        public Area(XElement node, Group processor) : base(node, processor) { }

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

