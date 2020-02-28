using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class Enricher : ErrorReporter
    {              
        public Model Model { get; set; } = new Model();
        public FileManager FileManager { get; private set; } = new FileManager();
        
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

            //Add parallel transformer sets to the import configuration
            FileManager.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "Transformer Parallel Sets"), new XAttribute("name", "Transformer Parallel Sets")));
            //Add line types to the import configuration
            FileManager.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "Line Types"), new XAttribute("name", "Line Types")));
            //Add custom scada linking to the import configuration
            FileManager.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "SCADA"), new XAttribute("name", "Custom SCADA Links")));
            //Add bookmarks to the import configuration
            FileManager.ImportConfig.Groups.Add(new XElement("group", new XAttribute("id", "Bookmarks"), new XAttribute("name", "Bookmarks")));
            
            
            ProcessGeographic();

            Model.ValidateConnectivity();
            Model.ValidateBaseVoltages();
            Model.ValidatePhasing();
            Model.CalculateNominalFeeders();
            Model.TraceLoadAllocation();

            if (Fatals == 0)
            {

                Model.Serialize($"{o.OutputPath}\\model");
                if (true)
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

                Model.ExportDeviceCoordinates();
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
                FileManager.SaveFiles(o.OutputPath);

                //TODO: this is a temporary measure
                File.Copy(Path.Combine(Program.Options.DataPath, "ExternalInfo.xml"), Path.Combine(Program.Options.OutputPath, "ExternalInfo.xml"), true);
                File.Copy(Path.Combine(Program.Options.DataPath, "Parallel Sets.xml"), Path.Combine(Program.Options.OutputPath, "Parallel Sets.xml"), true);
                File.Copy(Path.Combine(Program.Options.DataPath, "Conductors.xml"), Path.Combine(Program.Options.OutputPath, "Conductors.xml"), true);
                File.Copy(Path.Combine(Program.Options.DataPath, "CustomSCADALinks.xml"), Path.Combine(Program.Options.OutputPath, "CustomSCADALinks.xml"), true);
                File.Copy(Path.Combine(Program.Options.DataPath, "Bookmarks.xml"), Path.Combine(Program.Options.OutputPath, "Bookmarks.xml"), true);

            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Stats: Tx:{Model.TxCount} Line:{Model.LineCount} Line5:{Model.Line5Count} Line25:{Model.Line25Count} Load:{Model.LoadCount} Switch:{Model.SwitchCount} Reg:{Model.RegCount} Feeder:{Model.FeederCount} Runtime:{runtime.TotalMinutes} min");
            Info($"Stats: Debug:{Debugs} Info:{Infos} Warn:{Warns} Error:{Errors} Fatal:{Fatals}");
        }

        private void ValidateOptions(Options o)
        {
            if (o.Threads < 1 || o.Threads > 100)
            {
                Warn("Options.Threads was outside the range 1-100, setting to 10");
                o.Threads = 10;
            }
            //Options = o;
        }

        public bool ProcessImportConfiguration()
        {
            if (!FileManager.Initialize(Program.Options.InputPath))
            {
                return false;
            }
            var tasks = new List<Task>();

            foreach (var group in FileManager.Groups.Values)
            {
                try
                {
                    if (group.NoData)
                        continue;
                    //removing the group from the model is not required for new models so lets skip it as it is an expensive operation
                    
                    //TODO: reinstate
                    if (!Program.Options.BlankModel)
                        Model.RemoveGroup(group.Id);
                    
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Program.Options.Threads)
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

            foreach (var group in Program.Enricher.FileManager.Groups.Values)
            {
                try
                {
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Program.Options.Threads)
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

            foreach (var group in FileManager.Groups.Values)
            {
                try
                {
                    //No point running 1000 threads at once
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > Program.Options.Threads)
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
