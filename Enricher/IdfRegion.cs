using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfRegion : IdfElement
    {
        public IdfRegion(XElement node, IdfGroup processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

