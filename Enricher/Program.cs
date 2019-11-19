using CommandLine;
using CommandLine.Text;
using log4net;
using log4net.Appender;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MainPower.Osi.Enricher
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetLogFileName(string name)
        {
            var rootAppender = LogManager.GetRepository()
                                         .GetAppenders()
                                         .OfType<RollingFileAppender>()
                                         .FirstOrDefault(fa => fa.Name == name);

            return rootAppender != null ? rootAppender.File : string.Empty;
        }

        static int Main(string[] args)
        {
            /*
            Point tl = new Point { X = 0, Y = 0 };
            Point br = new Point { X = 500000, Y = 500000 };
            Point tl2 = IdfGroup.TranslatePoint(tl);
            Point br2 = IdfGroup.TranslatePoint(br);

            Console.WriteLine($"{tl2.X} {tl2.Y} {br2.X} {br2.Y}");
            Console.ReadKey();
            return 0;
            */

            Console.BufferWidth = 320;
            
            int retCode = 3;

            var r = Parser.Default.ParseArguments<Options>(args)
               .WithParsed(o =>
               {
                   try
                   {
                       //clear the output directory
                       var files = Directory.GetFiles(o.OutputPath, "*.xml");
                       foreach (var file in files)
                       {
                           File.Delete(file);
                       }
                       Enricher.I.Go(o);
                       Console.WriteLine("All done....");
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                   }
                   try
                   {
                       if (o.ArchiveIdf) {
                           //copy the input idfs to the log location
                           string logpath = GetLogFileName("file");
                           string logfile = Path.GetFileName(logpath);
                           string zipfile1 = logfile.Replace("enricher", "idf-input").Replace(".csv", ".zip");
                           string zipfile2 = logfile.Replace("enricher", "idf-output").Replace(".csv", ".zip");
                           string logdir = Path.GetDirectoryName(logpath);
                           ZipFile.CreateFromDirectory(o.InputPath, Path.Combine(logdir, zipfile1));
                           ZipFile.CreateFromDirectory(o.OutputPath, Path.Combine(logdir, zipfile2));
                       }
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                   }
                   if (ErrorReporter.Fatals > 0)
                       retCode = 3;
                   else if (ErrorReporter.Errors > 0)
                       retCode = 2;
                   else if (ErrorReporter.Warns > 0)
                       retCode = 1;
                   else
                       retCode = 0;
                   if (o.PauseBeforeQuit)
                       Console.ReadKey();
               });
            return retCode;
        }
    }
}
