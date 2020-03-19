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

        private const string IdfLoadClass = "loadClass";

        private const string LoadIcpLoad = "Consumption";
        //private const string LoadIcpType = "Type";
        private const string LoadIcpType = "CUSTOMER_GROUP__C";
        
        public IdfLoad(XElement node, IdfGroup processor) : base(node, processor) { }

        private double loadFactor = 1.0;
        public override void Process()
        {
            try
            {
                string symbol = "";
                string loadClass = "";
                ParentGroup.UpdateLinkId(Id, Name);
                UpdateId(Name);
                CheckPhasesSide1Only();

                if (Name.StartsWith("Streetlight"))
                    Err("I'm a streetlight");

                Icp icp = DataManager.I.RequestRecordById<Icp>(Node.Attribute("name").Value);
                string icpType = icp?[LoadIcpType];

                switch (icpType)
                {
                    case "Residential":
                        symbol = SymbolLoadResidential;
                        loadClass = "Residential";
                        break;
                    case "General":
                        symbol = SymbolLoadGeneral;
                        loadClass = "General";
                        break;
                    case "Irrigation":
                        symbol = SymbolLoadIrrigation;
                        loadClass = "Irrigation";
                        break;
                    case "Council Pumping":
                        symbol = SymbolLoadPumping;
                        loadClass = "Pumping";
                        break;
                    case "Distributed Generation":
                        symbol = SymbolLoadDG;
                        loadClass = "Distributed Generation";
                        break;
                    case "Streetlight":
                        symbol = SymbolLoadSL;
                        loadClass = "Streetlight";
                        break;
                    case "Large User":
                        symbol = SymbolLoadLargeUser;
                        loadClass = "Large User";
                        break;
                    default:
                        Warn("Unknown Load Class");
                        symbol = SymbolLoadUknown;
                        loadClass = "";
                        break;
                }
                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV).Value);
                Node.SetAttributeValue(IdfLoadClass, loadClass);

                //just in case the load is in a disconnected island, set the nominal kw to suppress maestro warnings
                SetNominalLoad(1);

                Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Load, symbol);
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

            //assume a power factor of 0.99
            double kvar = 0.01 * load;

            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID1")?.Value))
            {
                Node.SetAttributeValue("nominalKW1", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR1", kvar.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("ratedKVA1", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers1", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID2")?.Value))
            {
                Node.SetAttributeValue("nominalKW2", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR2", kvar.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("ratedKVA2", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers2", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(Node.Attribute("s1phaseID3")?.Value))
            {
                Node.SetAttributeValue("nominalKW3", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("nominalKVAR3", kvar.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("ratedKVA3", load.ToString("F2", CultureInfo.InvariantCulture));
                Node.SetAttributeValue("customers3", (1.0 / S1Phases).ToString("F3", CultureInfo.InvariantCulture));
            }
        }
    }
}
