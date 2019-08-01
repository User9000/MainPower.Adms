using System;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Load : Element
    {
        private const string LOAD_SL_SYMBOL = "Symbol 24";

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
                    Node.SetAttributeValue("ratedKV", "0.4400");
                    Node.SetAttributeValue("secondaryBaseKV", "0.4000");
                    Node.SetAttributeValue("baseKV", "0.4000");

                    int phases = 0;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
                        phases++;

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
                    if (phases != 0)
                        load /= phases;
                    else
                        Error("No phase IDs are set");

                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
                        Node.SetAttributeValue("nominalKW1", load.ToString("N1"));
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
                        Node.SetAttributeValue("nominalKW2", load.ToString("N1"));
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
                        Node.SetAttributeValue("nominalKW3", load.ToString("N1"));
                    Node.SetAttributeValue("nominalKWAggregate", "");
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
