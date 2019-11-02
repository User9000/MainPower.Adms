using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// Represents the IDF Circuit element
    /// </summary>
    public class IdfCircuit : IdfElement
    {
        /// <summary>
        /// Creates a new Circuit
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="processor">The IDF Group that the Circuit belongs to</param>
        public IdfCircuit(XElement node, IdfGroup processor) : base(node, processor) { }

        /// <summary>
        /// Process custom logic for Circuits
        /// </summary>
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
