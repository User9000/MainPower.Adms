using System;
using System.Collections.Generic;
using System.Text;

namespace MainPower.Adms.ExtractManager
{
    public class Settings
    {
        public string IdfFileShare { get; set; } = @"//mpsutil/idfshare";
        public string EnricherPath { get; set; } = @"D:\osi\osi_cust\bin\enricher";
        public string VirtuosoDestinationPath { get; set; } = @"D:\osi\monarch\sys\maestro\intermediate";
        public string EnricherDataPath { get; set; } = @"\\mpsutil\idfshare\enricherdata";
        public string GisServerUrl { get; set; } = "http://mpgis3.mainpower.co.nz/adms";
        public string LogPath { get; set; } = "D:\\osi\\osi_cust\\bin";
        public int EnricherDebug { get; set; } = 3;
        public bool EnricherExportShapeFiles { get; set; } = true;
        public int EnricherThreads { get; set; } = 10;
        public string EnricherReferenceModel { get; set; }
        public string EnricherLatestModel { get; set; }
        
        /// <summary>
        /// Timeout period, in minutes
        /// </summary>
        public int Timeout { get; set; } = 360;
    }
}
