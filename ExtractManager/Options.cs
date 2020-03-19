using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace MainPower.Adms.ExtractManager
{
    public class Options
    {
        [Option('i', "incremental", HelpText = "Trigger an incremental extract", Required = false)]
        public bool IncrementalExtract { get; set; }

        [Option('c', "complete", HelpText = "Trigger a complete extract", Required = false)]
        public bool CompleteExtract { get; set; }

        [Option('s', "success", HelpText = "Signal a sucessful maestro import", Required = false)]
        public bool SuccessfulImport { get; set; }

        [Option('d', "debug", HelpText = "Debug", Required = false)]
        public int Debug { get; set; }
    }
}
