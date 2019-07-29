using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    public class Options
    {
        [Option('i', "ipath", HelpText = "Input file path", Required = true)]
        public string InputPath { get; set; }

        [Option('o', "opath", HelpText = "Output path", Required = true)]
        public string OutputPath { get; set; }

        [Option('d', "dpath", HelpText = "Data path", Required = true)]
        public string DataPath { get; set; }

        [Option('t', "dotopology", HelpText = "Process topology", Default = true)]
        public bool ProcessTopology { get; set; }

        [Option('c', "checkflow", HelpText = "Check switch flow direction", Default =true)]
        public bool CheckSwitchFlow { get; set; }

        [Option('u', "updateflow", HelpText = "Update switch flow direction", Default = true)]
        public bool UpdateSwitchFlow { get; set; }

        [Option('m', "blankmodel", HelpText = "Start with a blank model", Default = false)]
        public bool BlankModel { get; set; }

        [Option('p', "pause", HelpText = "Pause before quitting", Default = true)]
        public bool PauseBeforeQuit { get; set; }

        [Option('z', "archiveidf", HelpText = "Archive the input idfs to the log directory", Default = true)]
        public bool ArchiveIdf { get; set; }

    }
}
