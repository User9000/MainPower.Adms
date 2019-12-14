using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PropertyChanged;
using MicroMvvm;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace MainPower.Adms.IdfManager
{
    public class IdfBundle : ObservableObject
    {
        public string Path { get; set; }
        public EnricherResult EnricherResult { get; set; }
        public string Name { get; internal set; }
        public string OutputPath { get; set; }

        public Brush Color { get
            {
                return EnricherResult.Result switch
                {
                    0 => Brushes.LightGreen,
                    1 => Brushes.LightYellow,
                    2 => Brushes.PeachPuff,
                    3 => Brushes.OrangeRed,
                    _ => Brushes.LightGray
                };
            } 
        }

        public IdfBundle(string path)
        {
            Path = path;
            OutputPath = System.IO.Path.Combine(Path, "output");
            EnricherResult = Util.DeserializeNewtonsoft<EnricherResult>(System.IO.Path.Combine(Path, "output", "result.json")) ?? new EnricherResult();
        }

        #region OpenLogCommand
        void OpenLogExecute(object o)
        {
            var log = System.IO.Path.Combine(Path, "output", "log.csv");
            if (File.Exists(log))
            {
                using Process p = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = System.IO.Path.Combine(Path, "output", "log.csv"),
                        UseShellExecute = true
                    }
                };
                p.Start();
            }
            else
            {
                MessageBox.Show("Log file not fuond");
            }
        } 

        bool CanOpenLogExecute(object o)
        {
            return EnricherResult.HasOutput;
        }

        public ICommand OpenLog { get { return new RelayCommand<object>(OpenLogExecute, CanOpenLogExecute); } }
        #endregion

        public override string ToString()
        {
            return Name;
        }
    }

    public class EnricherResult : ObservableObject
    {
        public DateTime Time { get; set; }

        [DependsOn(nameof(Result))]
        public bool HasOutput
        {
            get
            {
                return Result >= 0;
            }
        }

        [DependsOn(nameof(Result))]
        public bool Success
        {
            get
            {
                return Result < 3 && Result >= 0;
            }
        }

        public int Result { get; set; } = -1;
        public int Fatals { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }

        public string ResultMessage
        {
            get
            {
                return Result switch
                {
                    0 => "Enricher enriched successfully.",
                    1 => "Enricher enriched with warnings.",
                    2 => "Enricher enriched with errors.",
                    3 => "Enricher failed.",
                    _ => "Idf has not been enriched yet.",
                };
            }
        }

        public string StatsMessage
        {
            get
            {
                return $"Fatals: {Fatals}, Errors: {Errors}, Warnings: {Warnings}";
            }
        }

    }

    public class EnricherOptions : ObservableObject
    {
        public IdfBundle Model { get; set; }
        public bool UseLatestModel { get; set; } = true;
        public int Debug { get; set; } = 3;
        public bool NewModel { get; set; } = false;
        public bool ExportShapeFiles { get; set; } = true;
        public int Threads { get; set; } = 10;
        public bool PauseOnCompletion { get; set; } = true;
    }

}