using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class IdfRegulator : IdfElement
    {
        private const string IdfRegulatorSymbol = "Symbol 7";
        private const string RegulatorDefaultType = "transformerType_regulator_default";
        private const string IdfTransformerType = "transformerType";

        public IdfRegulator(XElement node, IdfGroup processor) : base(node, processor) { }


        public override void Process()
        {
            try
            {
                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV).Value);
                Node.SetAttributeValue(IdfTransformerType, RegulatorDefaultType);
                Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Regulator, IdfRegulatorSymbol);
                RemoveExtraAttributes();
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GisT1Asset, null);
        }
    }
}
