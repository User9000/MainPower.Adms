using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Feeder : Element
    {
        public Feeder(XElement node, Group processor) : base(node, processor) { }

        private const string IDF_FEEDER_SOURCE = "source";
        private const string IDF_FEEDER_DEVICE = "primary";

        public override void Process()
        {
            try
            {
                var dev = Node.Attribute(IDF_FEEDER_DEVICE)?.Value;
                if (!string.IsNullOrWhiteSpace(dev))
                {
                    Enricher.I.Model.AddFeeder(Id, dev, Name, ParentGroup.Id);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

