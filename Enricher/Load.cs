using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{

    internal class Load : Element
    {
        private const string SYMBOL_LOAD_SL = "Symbol 24";
        private const string SYMBOL_LOAD_RESIDENTIAL = "Symbol 13";
        private const string SYMBOL_LOAD_RESIDENTIAL_CRITICAL = "Symbol 26";
        private const string SYMBOL_LOAD_LARGEUSER = "Symbol 27";
        private const string SYMBOL_LOAD_PUMPING = "Symbol 26";
        private const string SYMBOL_LOAD_DG = "Symbol 25";
        private const string SYMBOL_LOAD_IRRIGATION = "Symbol 28";
        private const string SYMBOL_LOAD_GENERAL = "Symbol 30";
        private const string SYMBOL_LOAD_UNKNOWN = "Symbol 29";

        private const string LOAD_ICP_LOAD = "Consumption";
        private const string LOAD_ICP_TYPE = "Type";

        public Load(XElement node, Group processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
#if !nofixes
                ParentGroup.AddMissingPhases(Node, true);
                if (string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID1)?.Value) &&
                    string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID2)?.Value) &&
                    string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S1_PHASEID3)?.Value) &&
                    string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID1)?.Value) &&
                    string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID2)?.Value) &&
                    string.IsNullOrWhiteSpace(Node.Attribute(IDF_DEVICE_S2_PHASEID3)?.Value))
                {
                    Error("All phases are belong to null, now it 3 phase");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "1");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "3");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "1");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "3");
                }
#endif
                ParentGroup.UpdateLinkId(Id, Name);
                UpdateId(Name);

                if (Name.StartsWith("Streetlight"))
                {
                    Error("I'm a streetlight");
#if !nofixes
                    ParentGroup.SetLayerFromDatalinkId(Id, "Loads", "Loads", "Default", "Default");
                    ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_SL, 1.0, 1.0);
                    ParentGroup.RemoveDataLinkFromSymbols(Id);
                    Node.Remove();
#endif
                }
                else
                {
#if !nofixes
                    Node.SetAttributeValue("aorGroup", "1");
                    Node.SetAttributeValue("nominalState1", "True");
                    Node.SetAttributeValue("nominalState2", "True");
                    Node.SetAttributeValue("nominalState3", "True");
                    Node.SetAttributeValue("ratedKV", "0.4400");
                    Node.SetAttributeValue("secondaryBaseKV", "0.4000");
                    Node.SetAttributeValue("baseKV", "0.4000");
                    Node.SetAttributeValue("connectionType", "Wye-G");
                    Node.SetAttributeValue("loadProfileType", "Conforming");
#endif
                    double nomLoad = 3;
                    int phases = 0;

                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
                        phases++;
                    if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
                        phases++;

                    Icp icp = DataManager.I.RequestRecordById<Icp>(Node.Attribute("name").Value);
                    string icpType = icp?[LOAD_ICP_TYPE];
                    double? load = icp?.AsDouble(LOAD_ICP_LOAD);
                    if (!load.HasValue)
                    {
                        Warn($"ICP was not found in the ICP database, assigning default load of 3 kW");
                        load = nomLoad;
                    }
                    else if (load < 72)
                    {
                        Warn($"ICP had low load ({load}) - assigning default load of 3 kW");
                        load = nomLoad;
                    }
                    else
                    {
                        load /= 72;
                    }
                    
                    if (phases != 0)
                        load /= phases;
                    else
                        Error("No phase IDs are set");

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
                    switch (icpType)
                    {
                        case "Residential":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_RESIDENTIAL, 2.0);
                            break;
                        case "General":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_GENERAL, 2.0);
                            break;
                        case "Irrigation":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_IRRIGATION, 2.0);
                            break;
                        case "Council Pumping":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_PUMPING, 2.0);
                            break;
                        case "Distributed Generation":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_DG, 2.0);
                            break;
                        case "Streetlight":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_SL, 1.0);
                            break;
                        case "Large User":
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_LARGEUSER, 2.0);
                            break;
                        default:
                            ParentGroup.SetSymbolNameByDataLink(Id, SYMBOL_LOAD_UNKNOWN, 2.0);
                            break;
                    }
                    double voltage = double.Parse(Node.Attribute(IDF_DEVICE_BASEKV).Value);
                    Node.SetAttributeValue("ratedKV", (voltage * 1.2).ToString());

                    var geo = ParentGroup.GetSymbolGeometry(Id);
                    Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Load, geo);
#if !nofixes
                    ParentGroup.SetLayerFromDatalinkId(Id, "Loads", "Loads", "Default", "Default");
#endif
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
