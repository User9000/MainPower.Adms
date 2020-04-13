using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using static log4net.Appender.ManagedColoredConsoleAppender;

namespace MainPower.Adms.Enricher
{
    public static class Logger
    {
        public static void Setup(string path, Level level)
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            hierarchy.Root.RemoveAllAppenders();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date{HH:mm:ss,fff},%level,%message%newline";
            patternLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = false;
            roller.File = Path.Combine(path, "log.csv");
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 5;
            roller.MaximumFileSize = "10GB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();

            PatternLayout patternLayout2 = new PatternLayout();
            patternLayout2.ConversionPattern = "%level,%message%newline";
            patternLayout2.ActivateOptions();

            ManagedColoredConsoleAppender console = new ManagedColoredConsoleAppender();
            var l1 = new LevelColors
            {
                ForeColor = ConsoleColor.Green,
                Level = Level.Fatal
            };
            var l2 = new LevelColors
            {
                ForeColor = ConsoleColor.Red,
                Level = Level.Error
            };
            var l3 = new LevelColors
            {
                ForeColor = ConsoleColor.Yellow,
                Level = Level.Warn
            };
            var l4 = new LevelColors
            {
                ForeColor = ConsoleColor.White,
                Level = Level.Info
            };
            var l5 = new LevelColors
            {
                ForeColor = ConsoleColor.White,
                Level = Level.Debug
            };

            console.AddMapping(l1);
            console.AddMapping(l2);
            console.AddMapping(l3);
            console.AddMapping(l4);
            console.AddMapping(l5);
            console.Layout = patternLayout2;
            console.ActivateOptions();

            hierarchy.Root.Level = level;
            BasicConfigurator.Configure(hierarchy, roller, console);
        }
    }
}
