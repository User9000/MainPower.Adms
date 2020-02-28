using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{

    public class IdfLoad : IdfElement
    {
        private const string SymbolLoadSL = "Symbol 24";
        private const string SymbolLoadResidential = "Symbol 13";
        private const string SymbolLoadResidentialCritical = "Symbol 26";
        private const string SymbolLoadLargeUser = "Symbol 27";
        private const string SymbolLoadPumping = "Symbol 26";
        private const string SymbolLoadDG = "Symbol 25";
        private const string SymbolLoadIrrigation = "Symbol 28";
        private const string SymbolLoadGeneral = "Symbol 30";
        private const string SymbolLoadUknown = "Symbol 29";

        private const string LoadIcpLoad = "Consumption";
        //private const string LoadIcpType = "Type";
        private const string LoadIcpType = "CUSTOMER_GROUP__C";
        
        public IdfLoad(XElement node, IdfGroup processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {
                string symbol = "";
                ParentGroup.UpdateLinkId(Id, Name);
                UpdateId(Name);
                CheckPhasesSide1Only();

                if (Name.StartsWith("Streetlight"))
                {
                    Err("I'm a streetlight");
                }
                else
                {
                    Icp icp = DataManager.I.RequestRecordById<Icp>(Node.Attribute("name").Value);
                    string icpType = icp?[LoadIcpType];
                   
                    switch (icpType)
                    {
                        case "Residential":
                            symbol = SymbolLoadResidential;
                            break;
                        case "General":
                            symbol = SymbolLoadGeneral;
                            break;
                        case "Irrigation":
                            symbol = SymbolLoadIrrigation;
                            break;
                        case "Council Pumping":
                            symbol = SymbolLoadPumping;
                            break;
                        case "Distributed Generation":
                            symbol = SymbolLoadDG;
                            break;
                        case "Streetlight":
                            symbol = SymbolLoadSL;
                            break;
                        case "Large User":
                            symbol = SymbolLoadLargeUser;
                            break;
                        default:
                            symbol = SymbolLoadUknown;
                            break;
                    }
                    Node.SetAttributeValue("ratedKV", Node.Attribute("baseKV").Value);
                    Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Load, symbol);
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }

        public void SetNominalLoad(double load)
        {
            if (load == 0)
                load = 3;
            else 
                load /= S1Phases;

            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
            {
                Node.SetAttributeValue("nominalKW1", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR1", "0.1");
                Node.SetAttributeValue("ratedKVA1", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers1", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
            {
                Node.SetAttributeValue("nominalKW2", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR2", "0.1");
                Node.SetAttributeValue("ratedKVA2", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers2", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
            {
                Node.SetAttributeValue("nominalKW3", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR3", "0.1");
                Node.SetAttributeValue("ratedKVA3", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers3", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
        }
    }
}
