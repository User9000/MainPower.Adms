using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// Represents the IDF Area element
    /// </summary>
    public class IdfArea : IdfElement
    {
        /// <summary>
        /// Creates a new Area
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="processor">The IDF Group that the Area belongs to</param>
        public IdfArea(XElement node, IdfGroup processor) : base(node, processor) { }

        /// <summary>
        /// Process custom logic for Areas
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

