using System;
using System.Collections.Generic;
using System.Text;

namespace MainPower.Adms.Enricher
{
    public class EnricherResult
    {
        public DateTime Time { get; set; }
        public int Result { get; set; }
        public int Fatals { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
    }
}
