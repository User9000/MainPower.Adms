using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Capacitor : Element
    {
        private const string SYMBOL_CAPACITOR = "Symbol 12";

        /// <summary>
        /// Represents a IDF Capacitor object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public Capacitor(XElement node, Group parent) : base(node, parent) { }

        /// <summary>
        /// Process the Capacitor object
        /// </summary>
        public override void Process()
        {
            try
            {
                SetAllNominalStates();
                var geo = ParentGroup.GetSymbolGeometry(Id);
                Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.ShuntCapacitor, geo);
                ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_CAPACITOR);
                Node.SetAttributeValue(IDF_DEVICE_RATEDKV, Node.Attribute(IDF_DEVICE_BASEKV)?.Value);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
