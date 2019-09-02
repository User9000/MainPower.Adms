using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Load : Element
    {
        private const string LOAD_SL_SYMBOL = "Symbol 24";
        private const string LOAD_ICP_LOAD = "AverageMonthlyLoad";

        public Load(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                ParentGroup.AddMissingPhases(Node, true);


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
                    Node.SetAttributeValue("connectionType", "Wye-G");
                    Node.SetAttributeValue("loadProfileType", "Conforming");
                    double nomLoad = 3;
                    int phases = 0;

                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
                        phases++;
                    /*
                    double? load = DataManager.I.RequestRecordById<Icp>(Node.Attribute("name").Value)?.AsDouble(LOAD_ICP_LOAD);
                    if (!load.HasValue)
                    {
                        Warn($"ICP was not found in the ICP database, assigning default load of 7.5kW");
                        load = nomLoad;
                    }
                    else if (load < 72)
                    {
                        Warn($"ICP had low load ({load}) - assigning default load of 7.5kW");
                        load = nomLoad;
                    }
                    else
                    {
                        load = load / 72;
                    }
                    */
                    double? load = nomLoad;
                    if (phases != 0)
                        load /= phases;
                    else
                        Error("No phase IDs are set");

                    //TODO: we need to unset the unused phases in case they are changed from export to export
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
                    {
                        Node.SetAttributeValue("nominalKW1", load?.ToString("N2"));
                        Node.SetAttributeValue("customers1", (1.0/phases).ToString("N2"));
                    }
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
                    {
                        Node.SetAttributeValue("nominalKW2", load?.ToString("N2"));
                        Node.SetAttributeValue("customers2", (1.0 / phases).ToString("N2"));
                    }
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
                    {
                        Node.SetAttributeValue("nominalKW3", load?.ToString("N2"));
                        Node.SetAttributeValue("customers3", (1.0 / phases).ToString("N2"));
                    }
                    Node.SetAttributeValue("nominalKWAggregate", null);
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
