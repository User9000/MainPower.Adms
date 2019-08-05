using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Enricher : ErrorReporter
    {
        internal static Enricher I { get; } = new Enricher();

        public Options Options { get; set; }

        
        private const string ICP_ICP = "ICP";
        private const string ICP_Month = "Month";
        private const string ICP_Consumption = "Consumption";

        internal Dictionary<string, double> IcpConsumption { get; set; }
        
        internal int TransformerCount { get; set; }
        internal int LineCount { get; set; }
        internal int SwitchCount { get; set; }
        internal int LoadCount { get; set; }

        internal NodeModel Model { get; set; } = new NodeModel();

        internal void Go(Options o)
        {
            DateTime start = DateTime.Now;
            Options = o;
            if (o.ProcessTopology)
            {
                if (!o.BlankModel)
                {
                    Console.WriteLine("Loading previous model...");
                    var model = NodeModel.Deserialize($"{o.DataPath}\\model");
                    if (model != null)
                    {
                        Model = model;
                    }
                    else
                    {
                        Console.WriteLine("Creating a new model...");
                    }
                }
            }
            if (o.ConvertIcps)
            {
                Info("Converting ICP database...");
                ConvertIcpDatabase();
            }
            if (!DataManager.I.Load("", true))
            {
                Fatal("Failed to initialize the datamanager");
                return;
            }
            DataManager.I.Save("Datamanager.json");
            //LoadSourceData();
            ProcessImportConfiguration();
            TimeSpan runtime = DateTime.Now - start;
            if (o.ProcessTopology)
            {
                start = DateTime.Now;
                Model.DoConnectivity();
                Model.CheckVoltageConsistency();
                runtime = DateTime.Now - start;
                Console.WriteLine($"Connectivity check: {Model.GetDisconnectedCount()} devices disconnected ({runtime.TotalSeconds} seconds)");
                start = DateTime.Now;
                Model.DoPowerFlow();
                runtime = DateTime.Now - start;
                Console.WriteLine($"Power flow check: {Model.GetDeenergizedCount()} devices deenergized ({runtime.TotalSeconds} seconds)");
                if (Fatals == 0)
                {
                    Model.Serialize($"{o.DataPath}\\model");
                    if (o.CheckSwitchFlow || o.UpdateSwitchFlow)
                    {

                    }
                }    
                else
                {
                    Console.WriteLine("Skipping model serialization and flow checking due to the ocurrence of previous fatal errors");
                }

                
            }
            if (Fatals > 0)
            {
                Console.WriteLine("Output was not generated due to one or more fatal errors. Please review the log for more detail.");
            }
            else
            {
                FileManager.I.SaveFiles(o.OutputPath);
            }
            Console.WriteLine($"Stats: Tx:{TransformerCount} Line:{LineCount} Load:{LoadCount} Switch:{SwitchCount} Runtime:{runtime.TotalMinutes} min");
            Console.WriteLine($"Stats: Debug:{Debugs} Info:{Infos} Warn:{Warns} Error:{Errors} Fatal:{Fatals}");
        }

        public  void ConvertIcpDatabase()
        {
            DataTable icps = Util.GetDataTableFromCsv($"{Options.DataPath}\\ICPs-source.csv", true);
            IcpConsumption = new Dictionary<string, double>();
            var icp2 = new DataTable();
            icp2.Columns.Add("ICP", typeof(string));
            icp2.Columns.Add("AverageMonthlyLoad", typeof(double));
            //to start with we are just going for a plain jane average
            for (int i = 0; i < icps.Rows.Count; i++)
            {
                string icp = icps.Rows[i][ICP_ICP] as string;
                double c = icps.Rows[i][ICP_Consumption] as double? ?? 0;

                if (IcpConsumption.ContainsKey(icp))
                {
                    IcpConsumption[icp] = (IcpConsumption[icp] + c) / 2;
                }
                else
                {
                    IcpConsumption.Add(icp, c);
                }
            }
            foreach (var kvp in IcpConsumption)
            {
                var r = icp2.NewRow();
                r["ICP"] = kvp.Key;
                r["AverageMonthlyLoad"] = kvp.Value;
                icp2.Rows.Add(r);
            }
            Util.ExportDatatable(icp2, $"{Options.DataPath}\\ICPs.csv");
        }

        internal void ProcessImportConfiguration()
        {
            FileManager fm = FileManager.I;
            fm.Initialize(Options.InputPath);
            var tasks = new List<Task>();

            var groups = fm.ImportConfig.Content.Descendants("group");
            foreach (var group in groups)
            {
                try
                {
                    Group p = new Group(group, null);
                    if (p.NoData || p.NoGroup)
                        continue;
                    if (Options.ProcessTopology)
                        Model.RemoveGroup(p.Id);
                    tasks.Add(Task.Run((Action)p.Process));
                }
                catch (Exception ex)
                {
                    Fatal($"Uncaugut exception: {ex.Message}");
                }
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
