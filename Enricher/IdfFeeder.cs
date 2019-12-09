using System;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class IdfFeeder : IdfElement
    {
        public IdfFeeder(XElement node, IdfGroup processor) : base(node, processor) { }

        private const string IdfFeederSource = "source";
        private const string IdfFeederDevice = "primary";

        public override void Process()
        {
            try
            {
                var dev = Node.Attribute(IdfFeederDevice)?.Value;
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

