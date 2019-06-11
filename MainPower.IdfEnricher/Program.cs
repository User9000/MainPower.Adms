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
                       var enricher = Enricher.Singleton;
                       enricher.Options = o;
                       enricher.LoadSourceData();
                       enricher.ProcessImportConfiguration();
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
                   File.Copy(log , $"{o.Path}\\output\\{Path.GetFileName(log)}");


               });
            
        }
    }
}
