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

        internal DataTable T1Disconnectors { get; set; }
        internal DataTable T1Fuses { get; set; }
        internal DataTable T1HvCircuitBreakers { get; set; }
        internal DataTable T1RingMainUnits { get; set; }
        internal DataTable T1Transformers { get; set; }
        internal DataTable ScadaStatus { get; set; }
        internal DataTable ScadaAnalog { get; set; }
        internal DataTable ScadaAccumulator { get; set; }
        internal DataTable AdmsSwitch { get; set; }
        internal DataTable AdmsTransformer { get; set; }

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
            LoadSourceData();
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

        internal void LoadSourceData()
        {
            Console.WriteLine("Loading Disconnectors...");
            T1Disconnectors = Util.GetDataTableFromCsv($"{Options.DataPath}\\T1Disconnectors.csv", true);
            Console.WriteLine("Loading Fuses...");
            T1Fuses = Util.GetDataTableFromCsv($"{Options.DataPath}\\T1Fuses.csv", true);
            Console.WriteLine("Loading Circuit Breakers...");
            T1HvCircuitBreakers = Util.GetDataTableFromCsv($"{Options.DataPath}\\T1HvCircuitBreakers.csv", true);
            Console.WriteLine("Loading RMUs...");
            T1RingMainUnits = Util.GetDataTableFromCsv($"{Options.DataPath}\\T1RingMainUnits.csv", true);
            Console.WriteLine("Loading Transformers...");
            T1Transformers = Util.GetDataTableFromCsv($"{Options.DataPath}\\T1Transformers.csv", true);
            Console.WriteLine("Loading SCADA...");
            ScadaStatus = Util.GetDataTableFromCsv($"{Options.DataPath}\\ScadaStatus.csv", true);
            ScadaAnalog = Util.GetDataTableFromCsv($"{Options.DataPath}\\ScadaAnalog.csv", true);
            ScadaAccumulator = Util.GetDataTableFromCsv($"{Options.DataPath}\\ScadaAccumulator.csv", true);
            Console.WriteLine("Loading ADMS...");
            AdmsSwitch = Util.GetDataTableFromCsv($"{Options.DataPath}\\AdmsSwitch.csv", true);
            AdmsTransformer = Util.GetDataTableFromCsv($"{Options.DataPath}\\AdmsTransformer.csv", true);
            Console.WriteLine("Loading ICPs...");
            LoadIcpDatabase();
        }

        private void LoadIcpDatabase()
        {
            DataTable icps = Util.GetDataTableFromCsv($"{Options.DataPath}\\ICPs.csv", true);
            IcpConsumption = new Dictionary<string, double>();
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
        }

        public double GetIcpLoad(string icp)
        {
            if (IcpConsumption.ContainsKey(icp))
            {
                return IcpConsumption[icp];
            }
            else
            {
                return double.NaN;
            }
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

        private DataRow GenericDatasetQuery(DataTable data, string queryColumn, string queryName, bool trueforstringfalseforint, string id)
        {
            var s = trueforstringfalseforint ? "'": "";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        Debug($"{queryName}: Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn($"{queryName}: More than one {queryName} found with {queryColumn}:{id}");
                    }
                    return result[0];
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    return null;
                }
            }
        }

        internal DataRow GetT1DisconnectorByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Disconnectors, "Asset Number", "T1 Disconnector", false, id);
        }

        internal DataRow GetT1TransformerByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Transformers, "Asset Number", "T1 Transformer", false, id);
        }

        internal DataRow GetAdmsTransformerByAssetNumber(string id)
        {
            return GenericDatasetQuery(AdmsTransformer, "Asset Number", "Adms Transformer", false, id);
        }

        internal DataRow GetT1FuseByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Fuses, "Asset Number", "T1 Fuse", false, id);
        }

        internal DataRow GetT1RingMainUnitByT1AssetNumber(string id)
        {
            return GenericDatasetQuery(T1RingMainUnits, "Asset Number", "T1 RMU", false, id);
        }

        internal DataRow GetT1HvCircuitBreakerByAssetNumber (string id)
        {
            return GenericDatasetQuery(T1HvCircuitBreakers, "Asset Number", "T1 HV Circuit Breaker", false, id);
        }

        internal ScadaStatusPointInfo GetScadaStatusPointInfo(string id)
        {
            ScadaStatusPointInfo p = new ScadaStatusPointInfo();
            var data = ScadaStatus;
            var queryColumn = "Name";
            var queryName = "SCADA Point";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id}'");
                    if (result.Length == 0)
                    {
                        Debug(queryName, "Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn(queryName, $"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = (result[0]["Key"] as int?).ToString().PadLeft(8,'0');
                    p.PointType = result[0]["Type"] as string;
                    p.PointName = result[0]["Name"] as string;
                    //a point is quad state if there are four states, which are separated by '/'
                    p.QuadState = (result[0]["pStates"] as string).Count(x => x == '/') == 3;

                    return p;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    return null;
                }
            }
        }

        internal ScadaAnalogPointInfo GetScadaAnalogPointInfo(string id)
        {
            ScadaAnalogPointInfo p = new ScadaAnalogPointInfo();
            var data = ScadaAnalog;
            var queryColumn = "Name";
            var queryName = "SCADA Point";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id}'");
                    if (result.Length == 0)
                    {
                        Debug($"{queryName}: Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn($"{queryName}: More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = (result[0]["Key"] as int?).ToString().PadLeft(8, '0');
                    p.PointType = result[0]["Type"] as string;
                    p.PointName = result[0]["Name"] as string;
                    p.Units = result[0]["Units"] as string;

                    return p;
                }
                catch (Exception ex)
                {
                    Fatal($"Uncaught exception: {ex.Message}");
                    return null;
                }
            }
        }

        internal DataRow GetAdmsSwitch(string id)
        {
            return GenericDatasetQuery(AdmsSwitch, "Switch Number", "Adms Switch", true, id);
        }
    }
}
