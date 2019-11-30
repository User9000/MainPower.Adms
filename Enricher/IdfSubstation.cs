using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfSubstation : IdfElement
    {
        public IdfSubstation(XElement node, IdfGroup processor) : base(node, processor) { }

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

