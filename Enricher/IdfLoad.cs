using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{

    public class IdfLoad : IdfElement
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

        public IdfLoad(XElement node, IdfGroup processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {
                ParentGroup.UpdateLinkId(Id, Name);
                UpdateId(Name);
                SetAllNominalStates();

                if (Name.StartsWith("Streetlight"))
                {
                    Err("I'm a streetlight");
                }
                else
                {
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
                        Err("No phase IDs are set");
                
                    load = 3;
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
                    double voltage = double.Parse(Node.Attribute(IdfDeviceBasekV).Value);
                    Node.SetAttributeValue("ratedKV", (voltage * 1.2).ToString());

                    var geo = ParentGroup.GetSymbolGeometry(Id);
                    Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Load, geo.geometry, geo.internals);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
