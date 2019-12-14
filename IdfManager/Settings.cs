using System;
using System.Collections.Generic;
using System.Text;
using MicroMvvm;

namespace MainPower.Adms.IdfManager
{
    public class Settings :ObservableObject
    {
        public string IdfFileShare { get; set; }
        public string EnricherPath { get; set; }
        public string Leika2AdmsPath { get; set; }
        public string MaestroIntermediatePath { get; set; }
        public string EnricherDataPath { get; set; }
    }
}
