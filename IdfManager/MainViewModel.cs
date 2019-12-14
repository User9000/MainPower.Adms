using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using MicroMvvm;
using Newtonsoft.Json;

namespace MainPower.Adms.IdfManager
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<IdfBundle> IdfBundles { get; set; } = new ObservableCollection<IdfBundle>();
        public ObservableCollection<IdfBundle> ValidModels { get; set; } = new ObservableCollection<IdfBundle>();

        public EnricherOptions EnricherOptions { get; set; }
        public Settings Settings { get; set; }
        public IdfBundle SelectedBundle { get; set; }
        public IdfBundle LatestModel { get; set; }

        #region CopyToPdsCommand
        void CopyToPdsExecute(object o)
        {
            try
            {
                var files = Directory.GetFiles(Settings.MaestroIntermediatePath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                files = Directory.GetFiles(SelectedBundle.OutputPath, "*.xml");
                foreach (var f in files)
                {
                    File.Copy(f, Path.Combine(Settings.MaestroIntermediatePath, Path.GetFileName(f)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        bool CanCopyToPdsExecute(object o)
        {
            return SelectedBundle?.EnricherResult.Success ?? false;
        }

        public ICommand CopyToPds { get { return new RelayCommand<object>(CopyToPdsExecute, CanCopyToPdsExecute); } }
        #endregion



        #region RunEnricherCommand 
        void RunEnricherExecute(object o)
        {
            try
            {
                string arguments = "";
                arguments += $" -i \"{SelectedBundle.Path}\"";
                arguments += $" -o \"{SelectedBundle.OutputPath}\"";
                arguments += $" -d \"{Settings.EnricherDataPath}\"";
                arguments += $" -D {EnricherOptions.Debug}";
                arguments += $" --threads {EnricherOptions.Threads}";

                if (EnricherOptions.ExportShapeFiles)
                    arguments += " -s";
                if (EnricherOptions.PauseOnCompletion)
                    arguments += " -p";
                if (EnricherOptions.NewModel)
                    arguments += " -n";
                else if (EnricherOptions.UseLatestModel)
                {
                    if (LatestModel != null)
                    {
                        if (File.Exists(Path.Combine(LatestModel.OutputPath, "model")))
                            arguments += $" -m {Path.Combine(LatestModel.OutputPath, "model")}";
                        else
                            throw new Exception("Selected model doesn't exist!");
                    }
                    else
                        throw new Exception("Could not find the latest model.");

                }
                else if (File.Exists(Path.Combine(EnricherOptions.Model.OutputPath, "model")))
                    arguments += $" -m {Path.Combine(EnricherOptions.Model.OutputPath, "model")}";
                else
                    throw new Exception("Could not locate any input model.");

                using Process p = new Process()
                {
                    StartInfo =
                {
                    FileName = Settings.EnricherPath,
                    Arguments = arguments,
                }
                };
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        bool CanRunEnricherExecute(object o)
        {
            return true;
        }

        public ICommand RunEnricher { get { return new RelayCommand<object>(RunEnricherExecute, CanRunEnricherExecute); } }
        #endregion

        public MainViewModel()
        {
            //load the settings file
            Settings = Util.DeserializeNewtonsoft<Settings>("settings.json") ?? new Settings();
            EnricherOptions = new EnricherOptions();
            //scan the idf directory
            ScanIdfs();
        }

        public void ScanIdfs()
        {
            IdfBundles.Clear();
            ValidModels.Clear();
            try
            {
                DirectoryInfo d = new DirectoryInfo(Settings.IdfFileShare);
                foreach (var i in d.GetDirectories())
                {
                    var idf = new IdfBundle(i.FullName)
                    {
                        Name = i.Name
                    };
                    IdfBundles.Add(idf);
                    if (idf.EnricherResult.Success)
                        ValidModels.Add(idf);
                }
                LatestModel = (from m in ValidModels select m).OrderByDescending(x => x.EnricherResult.Time).FirstOrDefault();
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
