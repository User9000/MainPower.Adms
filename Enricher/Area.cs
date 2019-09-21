using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// Represents the IDF Area element
    /// </summary>
    internal class Area : Element
    {
        /// <summary>
        /// Creates a new Area
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="processor">The IDF Group that the Area belongs to</param>
        public Area(XElement node, Group processor) : base(node, processor) { }

        /// <summary>
        /// Process custom logic for Areas
        /// </summary>
        internal override void Process()
        {
            try
            {
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

