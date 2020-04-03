using CommandLine;
using CommandLine.Text;
using log4net;
using log4net.Appender;
using log4net.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MainPower.Adms.Enricher
{
    static class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Enricher Enricher { get; private set; } = new Enricher();

        public static Options Options { get; set; }

        static int Main(string[] args)
        {
            try
            {
                //this will throw when called by virtuoso, maybe because the output is redirected?
                //anyhoo, ignore exceptions and continue
                Console.BufferWidth = 320;
            }
            catch { }

            EnricherResult result = new EnricherResult();
            int retCode = result.Result = 3;
            result.Time = DateTime.Now;

            var r = Parser.Default.ParseArguments<Options>(args)
               .WithParsed(o =>
               {
                   try
                   {
                       Options = o;

                       //Create the output directory
                       Directory.CreateDirectory(o.OutputPath);
                       bool deleteFailed = false;
                       //clear the output directory
                       //need to do this before setting up logging, else we can't delete the old log file
                       var files = Directory.GetFiles(o.OutputPath);
                       foreach (var file in files)
                       {
                           try
                           {
                               File.Delete(file);
                           }
                           catch (Exception ex)
                           {
                               deleteFailed = true;
                               ErrorReporter.StaticFatal(ex.Message, typeof(Program));
                           }
                       }
                       //if there were failures then no point continuing
                       if (deleteFailed)
                           throw new Exception("Failed to clean output directory");

                       //setup logging
                       Level loglevel = Level.Info;
                       switch (o.Debug)
                       {
                           case 1:
                               loglevel = Level.Error;
                               break;
                           case 2:
                               loglevel = Level.Warn;
                               break;
                           case 3:
                               loglevel = Level.Info;
                               break;
                           case 4:
                               loglevel = Level.Debug;
                               break;
                       }
                       Logger.Setup(o.OutputPath, loglevel);
                       Enricher.Go(o);
                       Console.WriteLine("All done...");
                   }
                   catch (Exception ex)
                   {
                       ErrorReporter.StaticFatal(ex.Message, typeof(Program));
                       //print this one to the console as logging isn't guarantted to be working
                       Console.WriteLine(ex.ToString());
                   }

                   if (ErrorReporter.Fatals > 0)
                       result.Result = 3;
                   else if (ErrorReporter.Errors > 0)
                       result.Result = 2;
                   else if (ErrorReporter.Warns > 0)
                       result.Result = 1;
                   else
                       result.Result = 0;

                   result.Fatals = ErrorReporter.Fatals;
                   result.Errors = ErrorReporter.Errors;
                   result.Warnings = ErrorReporter.Warns;
                   result.Time = DateTime.Now;
               });
            try
            {
                Util.SerializeNewtonsoft(Path.Combine(Options.OutputPath, "result.json"), result);
                Util.SerializeNewtonsoft(Path.Combine(Options.OutputPath, "lastrunoptions.json"), Options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //TODO: bit hacky, but chances are the other arguments won't conatin -p
            if (string.Join(' ', args).Contains(" -p"))
                Console.ReadKey();

            return retCode;
        }
    }
}
