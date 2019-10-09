using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Regulator : Element
    {
        private const string IDF_REGULATOR_SYMBOL = "Symbol 7";

        public Regulator(XElement node, Group processor) : base(node, processor) { }


        internal override void Process()
        {
            try
            {
#if !nofixes
                //TOOD: Backport to GIS Extractor
                ParentGroup.AddMissingPhases(Node);
                
                Node.SetAttributeValue(IDF_ELEMENT_AOR_GROUP, AOR_DEFAULT);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE1, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE2, IDF_TRUE);
                Node.SetAttributeValue(IDF_DEVICE_NOMSTATE3, IDF_TRUE);
                Node.SetAttributeValue("ratedKV", "12");
                Node.SetAttributeValue(GIS_T1_ASSET, null);
                ParentGroup.SetLayerFromVoltage(Id, Node.Attribute(IDF_DEVICE_BASEKV).Value, false);
#endif
                //TODO
                ParentGroup.SetSymbolNameByDataLink(Id, IDF_REGULATOR_SYMBOL, double.NaN, double.NaN, 2);
                var geo = ParentGroup.GetSymbolGeometry(Id);
                Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Regulator, geo);               
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
