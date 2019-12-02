using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
               
        public int TransformerCount { get; set; }
        public int LineCount { get; set; }
        public int Line5Count { get; set; }
        public int Line25Count { get; set; }
        public int CapCount { get; set; }
        public int SwitchCount { get; set; }
        public int LoadCount { get; set; }

        public Model Model { get; set; } = new Model();

        public void Go(Options o)
        {
            DateTime start = DateTime.Now;
            ValidateOptions(o);
            PrintLogHeader();

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

            IdfTransformer.GenerateParallelSets(Path.Combine(Options.OutputPath, "Parallel Sets.xml"));
            FileManager.I.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "Transformer Parallel Sets"), new XAttribute("name", "Transformer Parallel Sets")));
            FileManager.I.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "Line Types"), new XAttribute("name", "Line Types")));
            File.Copy(Path.Combine(Options.DataPath, "Conductors.xml"), Path.Combine(Options.OutputPath, "Conductors.xml"), true);
            ProcessGeographic();

            Model.ValidateConnectivity();
            Model.ValidateBaseVoltages();
            Model.ValidatePhasing();
            Model.CalculateNominalFeeders();
            Model.TraceLoadAllocation();

            if (Fatals == 0)
            {

                Model.Serialize($"{o.DataPath}\\model");
                if (o.CheckSwitchFlow)
                {
                    //TODO move this into the nodemodel??
                    Info("Verifying connected device upstream side consistency...");
                    foreach (var d in Model.Devices.Values.Where(s => (s.Type == DeviceType.Switch) && s.Connectivity && s.SwitchState))
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
                                    //DataManager.I.SetVale<AdmsSwitch>(d.Name, "NominalUpstreamSide", d.Upstream);
                                }
                            }
                        }
                    }
                    //DataManager.I.Save<AdmsSwitch>();
                }
                ProcessGraphics();
                if (o.ExportShapeFiles)
                {
                    Model.ExportToShapeFile($"{o.OutputPath}\\");
                }
                if (o.ExportDeviceInfo)
                {
                    Model.ExportDeviceCoordinates();
                }
                IdfLine.ExportConductors();
            }
            else
            {
                Info("Skipping model serialization and flow checking due to the ocurrence of previous fatal errors");
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
            Info($"Stats: Tx:{TransformerCount} Line:{LineCount} Line5:{Line5Count} Line25:{Line25Count} Load:{LoadCount} Switch:{SwitchCount} Runtime:{runtime.TotalMinutes} min");
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

        public void ProcessGraphics()
        {
            Info("Processing graphics...");
            var tasks = new List<Task>();

            foreach (var group in FileManager.I.Groups.Values)
            {
                try
                {
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Options.Threads)
                        Thread.Sleep(100);
                    tasks.Add(Task.Run((Action)group.ProcessGraphics));
                }
                catch (Exception ex)
                {
                    Fatal($"Uncaugut exception: {ex.Message}");
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessGeographic()
        {
            Info("Processing geographic information...");
            var tasks = new List<Task>();

            foreach (var group in FileManager.I.Groups.Values)
            {
                try
                {
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Options.Threads)
                        Thread.Sleep(100);
                    tasks.Add(Task.Run((Action)group.ProcessGeographic));
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
