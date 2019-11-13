using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfRegulator : IdfElement
    {
        private const string IDF_REGULATOR_SYMBOL = "Symbol 7";
        private const string REGULATOR_DEFAULT_TYPE = "transformerType_regulator_default";
        private const string IDF_TRANSFORMER_TYPE = "transformerType";

        public IdfRegulator(XElement node, IdfGroup processor) : base(node, processor) { }


        public override void Process()
        {
            try
            {
                //TODO
                SetAllNominalStates();


                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV).Value);
                Node.SetAttributeValue(IDF_TRANSFORMER_TYPE, REGULATOR_DEFAULT_TYPE);
                ParentGroup.SetSymbolNameByDataLink(Id, IDF_REGULATOR_SYMBOL, double.NaN, double.NaN, 2);
                var geo = ParentGroup.GetSymbolGeometry(Id);
                Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Regulator, geo.geometry, geo.internals);
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
