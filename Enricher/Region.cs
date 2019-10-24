using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Region : Element
    {
        public Region(XElement node, Group processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {
#if !nofixes
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
#endif
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

