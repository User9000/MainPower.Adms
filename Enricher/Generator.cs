using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Generator : Element
    {
        private const string SYMBOL_GENERATOR = "Symbol 37";
        private const string GEN_DEFAULT_MACHINE = "machineType_default";

        /// <summary>
        /// Represents a IDF Generator object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public Generator(XElement node, Group parent) : base(node, parent) { }

        /// <summary>
        /// Process the Generator object
        /// </summary>
        public override void Process()
        {
            try
            {
                //Node.SetAttributeValue(IDF_DEVICE_BASEKV, "0.400");
                Node.SetAttributeValue("ratedKV", "0.500");
                Node.SetAttributeValue("nominalKW", "1000");
                Node.SetAttributeValue("nominalKVAR", "50");
                Node.SetAttributeValue("machineType", GEN_DEFAULT_MACHINE);
                Node.SetAttributeValue("connectionType", "Wye-G");
                Node.SetAttributeValue("lowVoltageLimit", "6.351");
                Node.SetAttributeValue("highVoltageLimit", "6.697");
                Node.SetAttributeValue("generatorType", "Generic");
                //ParentGroup.SetSymbolNameByDataLink(Id, "Symbol 37", 1.0, 1.0, 0);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
