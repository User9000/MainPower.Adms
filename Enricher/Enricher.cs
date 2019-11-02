using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Enricher : ErrorReporter
    {
        public static Enricher I { get; } = new Enricher();

        public Options Options { get; set; }
        
        private const string ICP_ICP = "ICP";
        private const string ICP_Month = "Month";
        private const string ICP_Consumption = "Consumption";

        public Dictionary<string, double> IcpConsumption { get; set; }
        
        public int TransformerCount { get; set; }
        public int LineCount { get; set; }
        public int CapCount { get; set; }
        public int SwitchCount { get; set; }
        public int LoadCount { get; set; }

        public Model Model { get; set; } = new Model();

        public void Go(Options o)
        {
            DateTime start = DateTime.Now;
            ValidateOptions(o);
            PrintLogHeader();
            if (o.ProcessTopology)
            {
                if (!o.BlankModel)
                {
                    
                    var model = Model.Deserialize($"{o.DataPath}\\model");
                    if (model != null)
                    {
                        Model = model;
                    }
                    else
                    {
                        Info("Creating a new model...");
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
            if (!ProcessImportConfiguration())
            {
                Fatal("Failed to process Import configuration, skipping everything else");
                return;
            }
            
            if (o.ProcessTopology)
            {
                Model.ValidateConnectivity();
                Model.ValidateBaseVoltages();
                Model.ValidatePhasing();
                Model.CalculateNominalFeeders();
                
                if (Fatals == 0)
                {
                    
                    Model.Serialize($"{o.DataPath}\\model");
                    if (o.CheckSwitchFlow)
                    {
                        //TODO move this into the nodemodel??
                        Info("Verifying connected device upstream side consistency...");
                        foreach (var d in Model.Devices.Values.Where(s => (s.Type == DeviceType.Switch) && s.ConnectivityMark && s.SwitchState))
                        {
                            var asset = DataManager.I.RequestRecordById<AdmsSwitch>(d.Name);
                            if (asset != null)
                            {
                                if (asset.AsBool("NotifyUpstreamSide") ?? false)
                                {
                                    var upstream = asset.AsInt("NominalUpstreamSide");
                                    if ((upstream ?? 0) != d.Upstream)
                                    {
                                        Warn($"Calculated nominal upstream side for switch [{d.Name}] ({d.Upstream}) is different from adms database ({upstream})");
                                        DataManager.I.SetVale<AdmsSwitch>(d.Name, "NominalUpstreamSide", d.Upstream);
                                    }
                                }
                            }
                        }
                        DataManager.I.Save<AdmsSwitch>();
                    }
                    if (o.ExportShapeFiles)
                    {
                        Model.ExportToShapeFile($"{o.OutputPath}\\");
                    }
                    if (o.ExportDeviceInfo)
                    {
                        Model.ExportDeviceCoordinates();
                    }
                    //TODO
                    IdfLine.ExportConductors();
                }    
                else
                {
                    Info("Skipping model serialization and flow checking due to the ocurrence of previous fatal errors");
                }               
            }
            if (Fatals > 0)
            {
                Info("Output was not generated due to one or more fatal errors. Please review the log for more detail.");
            }
            else
            {
                Info("Saving the output IDF...");
                FileManager.I.SaveFiles(o.OutputPath);
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Stats: Tx:{TransformerCount} Line:{LineCount} Load:{LoadCount} Switch:{SwitchCount} Runtime:{runtime.TotalMinutes} min");
            Info($"Stats: Debug:{Debugs} Info:{Infos} Warn:{Warns} Error:{Errors} Fatal:{Fatals}");
        }

        private void ValidateOptions(Options o)
        {
            if (o.Threads < 1 || o.Threads > 100)
            {
                Warn("Options.Threads was outside the range 1-100, setting to 10");
                o.Threads = 10;
            }
            Options = o;
        }

        //TODO: get rid of this
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

        public bool ProcessImportConfiguration()
        {
            FileManager fm = FileManager.I;
            if (!fm.Initialize(Options.InputPath))
            {
                return false;
            }
            var tasks = new List<Task>();

            foreach (var group in fm.Groups.Values)
            {
                try
                {
                    if (group.NoData)
                        continue;
                    if (Options.ProcessTopology)
                        Model.RemoveGroup(group.Id);
                    
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Options.Threads)
                        Thread.Sleep(100);
                    tasks.Add(Task.Run((Action)group.Process));
                }
                catch (Exception ex)
                {
                    Fatal($"Uncaugut exception: {ex.Message}");
                }
            }

            Task.WaitAll(tasks.ToArray());

            return true;
        }
    }
}
