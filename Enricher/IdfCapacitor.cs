using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfCapacitor : IdfElement
    {
        private const string SYMBOL_CAPACITOR = "Symbol 12";

        /// <summary>
        /// Represents a IDF Capacitor object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public IdfCapacitor(XElement node, IdfGroup parent) : base(node, parent) { }

        /// <summary>
        /// Process the Capacitor object
        /// </summary>
        public override void Process()
        {
            try
            {
                SetAllNominalStates();
                var geo = ParentGroup.GetSymbolGeometry(Id);
                Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.ShuntCapacitor, geo.geometry, geo.internals);
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CAPACITOR);
                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV)?.Value);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
