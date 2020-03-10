using System;
using System.Collections.Generic;
using System.Text;

namespace MainPower.Adms.ExtractManager
{
    public class Settings
    {
        public string IdfFileShare { get; set; } = @"//mpsutil/idfshare";
        public string EnricherPath { get; set; } = @"D:\osi\osi_cust\bin\enricher";
        public string MaestroIntermediatePath { get; set; } = @"D:\osi\monarch\sys\maestro\intermediate";
        public string EnricherDataPath { get; set; } = @"\\mpsutil\idfshare\enricherdata";
        public string GisServerUtl { get; set; } = "http://mpgis3.mainpower.co.nz/adms";
        public int EnricherDebug { get; set; } = 3;
        public bool EnricherExportShapeFiles { get; set; } = true;
        public int EnricherThreads { get; set; } = 10;
        public string EnricherReferenceModel { get; set; }
        public string EnricherLatestModel { get; set; }
    }
}
