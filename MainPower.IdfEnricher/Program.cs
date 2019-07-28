using CommandLine;
using log4net;
using log4net.Appender;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    class Program
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetLogFileName(string name)
        {
            var rootAppender = LogManager.GetRepository()
                                         .GetAppenders()
                                         .OfType<RollingFileAppender>()
                                         .FirstOrDefault(fa => fa.Name == name);

            return rootAppender != null ? rootAppender.File : string.Empty;
        }

        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o => 
               {
                   try
                   {
                       //clear the output directory
                       var files = Directory.GetFiles(o.OutputPath);
                       foreach (var file in files)
                       {
                           File.Delete(file);
                       }
                       DateTime start = DateTime.Now;
                       var enricher = Enricher.Singleton;
                       enricher.Options = o;
                       enricher.LoadSourceData();
                       enricher.ProcessImportConfiguration();
                       TimeSpan runtime = DateTime.Now - start;
                       Console.WriteLine($"Stats: Tx:{enricher.TransformerCount} Line:{enricher.LineCount} Load:{enricher.LoadCount} Switch:{enricher.SwitchCount} Runtime:{runtime.TotalMinutes} min");
                       start = DateTime.Now;
                       enricher.Model.DoConnectivity();
                       runtime = DateTime.Now - start;
                       Console.WriteLine($"Connectivity check: {enricher.Model.GetDisconnectedCount()} devices disconnected ({runtime.TotalSeconds} seconds)");
                       start = DateTime.Now;
                       enricher.Model.DoPowerFlow();
                       runtime = DateTime.Now - start;
                       Console.WriteLine($"Power flow check: {enricher.Model.GetDeenergizedCount()} devices deenergized ({runtime.TotalSeconds} seconds)");
                       //Console.WriteLine($"P91:{enricher.Model.GetUpstreamSideByName("P91")}");
                       //Console.WriteLine($"P92:{enricher.Model.GetUpstreamSideByName("P92")}");
                       //Console.WriteLine($"P21:{enricher.Model.GetUpstreamSideByName("P21")}");
                       //Console.WriteLine($"P22:{enricher.Model.GetUpstreamSideByName("P22")}");
                       //Console.WriteLine($"P35:{enricher.Model.GetUpstreamSideByName("P35")}");
                       //Console.WriteLine($"P45:{enricher.Model.GetUpstreamSideByName("P45")}");
                       enricher.Model.PrintPFDetailsByName("P91");
                       enricher.Model.PrintPFDetailsByName("P92");
                       enricher.Model.PrintPFDetailsByName("P21");
                       enricher.Model.PrintPFDetailsByName("P22");
                       enricher.Model.PrintPFDetailsByName("P35");
                       enricher.Model.PrintPFDetailsByName("P45");
                       enricher.Model.PrintPFDetailsByName("P55");

                       Console.WriteLine("All done....");
                       Console.ReadKey();
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                       Console.ReadKey();
                   }
                   string log = GetLogFileName("file");
                   if (File.Exists(log))
                   File.Copy(log , $"{o.OutputPath}\\{Path.GetFileName(log)}");


               });
            
        }
    }
}
