using System;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Load : Element
    {
        private const string LOAD_SL_SYMBOL = "Symbol 23";

        public Load(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                if (Name.StartsWith("Streetlight"))
                {
                    ParentGroup.SetSymbolNameByDataLink(Id, LOAD_SL_SYMBOL);
                    ParentGroup.RemoveDataLinksFromSymbols(Id);
                    Node.Remove();
                }
                else
                {
                    Node.SetAttributeValue("aorGroup", "1");
                    Node.SetAttributeValue("nominalState1", "True");
                    Node.SetAttributeValue("nominalState2", "True");
                    Node.SetAttributeValue("nominalState3", "True");
                    Node.SetAttributeValue("ratedKV", "0.4000");
                    Node.SetAttributeValue("secondaryBaseKV", "0.4000");

                    var basekv = Node.Attribute("baseKV")?.Value;
                    if (string.IsNullOrWhiteSpace(basekv))
                    {
                        Node.SetAttributeValue("baseKV", "0.2300");
                    }
                    //if (basekv = "0.4000)
                    double load = Enricher.I.GetIcpLoad(Node.Attribute("name").Value);
                    if (load.Equals(double.NaN))
                    {
                        Warn($"ICP was not found in the ICP database, assigning default load of 7.5kW");
                        load = 7.5;
                    }
                    else
                    {
                        load = load / 72;
                    }
                    //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                    //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                    //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                    Node.SetAttributeValue("nominalKWAggregate", load.ToString("N1"));
                    ParentGroup.SetSymbolNameByDataLink(Id, "Symbol 13", 2.0);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
