using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Feeder : Element
    {
        public Feeder(XElement node, Group processor) : base(node, processor) { }

        private const string IDF_FEEDER_SOURCE = "source";
        private const string IDF_FEEDER_DEVICE = "primary";

        internal override void Process()
        {
            try
            {
#if !nofixes
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                Node.SetAttributeValue(IDF_FEEDER_SOURCE, "");
                Node.SetAttributeValue("substationCircuit", "");
#endif
                Node.SetAttributeValue("substationCircuit", "");
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

