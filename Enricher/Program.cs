﻿using CommandLine;
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
        static int Main(string[] args)
        {
            Console.BufferWidth = 320;
            
            int retCode = 3;

            var r = Parser.Default.ParseArguments<Options>(args)
               .WithParsed(o =>
               {
                   try
                   {
                       //clear the output directory
                       //need to do this before setting up logging, else we can't delete the old log file
                       var files = Directory.GetFiles(o.OutputPath);
                       foreach (var file in files)
                       {
                           File.Delete(file);
                       }

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
                       Console.WriteLine("All done....");
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
