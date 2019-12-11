using System;
using System.Collections.Generic;
using System.Text;

namespace MainPower.Adms.IdfManager
{
    public class IdfBundle
    {
        public string Path { get; set; }
        public EnricherResult EnricherResult { get; set; }
        public EnricherOptions EnricherOptions { get; set; }

        public void GetEnricherResult()
        {

        }

        public void RunEnricher()
        {

        }
    }

    public class EnricherResult
    {
        public DateTime Time { get; set; }
        public int Result { get; set; }
        public int Fatals { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
    }

    public class EnricherOptions
    {
        public string Model { get; set; }
        public int Debug { get; set; }
        public bool BlankModel { get; set; }
        public bool ExportShapeFiles { get; set; }
        public int Threads { get; set; }
    }

}
