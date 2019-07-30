using System;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
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
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                Node.SetAttributeValue(IDF_FEEDER_SOURCE, "");
                ParentGroup.SetSwitchInSubstation(Node.Attribute(IDF_FEEDER_DEVICE).Value, IDF_TRUE);
            }
            catch (Exception ex)
            {
                Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

