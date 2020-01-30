using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MicroMvvm;
using PropertyChanged;

namespace MainPower.Adms.IdfManager
{
    public class Settings :ObservableObject
    {
        public string IdfFileShare { get; set; }
        public string EnricherPath { get; set; }
        public string Leika2AdmsPath { get; set; }
        public string MaestroIntermediatePath { get; set; }
        public string EnricherDataPath { get; set; }
        public ObservableCollection<DestinationTarget> DestinationTargets { get; set; } = new ObservableCollection<DestinationTarget>();
    }

    public class DestinationTarget : ObservableObject
    {
        public string Name { get; set; }
        public string Path { get; set; }

        [DependsOn(nameof(Name), nameof(Path))]
        public string Caption
        {
            get
            {
                return ToString();
            }
        }

        public override string ToString()
        {
            return $"[{Name}, {Path}]";
        }
    }
}
