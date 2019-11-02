using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfFeeder : IdfElement
    {
        public IdfFeeder(XElement node, IdfGroup processor) : base(node, processor) { }

        private const string IDF_FEEDER_SOURCE = "source";
        private const string IDF_FEEDER_DEVICE = "primary";

        public override void Process()
        {
            try
            {
                var dev = Node.Attribute(IDF_FEEDER_DEVICE)?.Value;
                if (!string.IsNullOrWhiteSpace(dev))
                {
                    Enricher.I.Model.AddFeeder(Id, Name, dev, ParentGroup.Id);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

