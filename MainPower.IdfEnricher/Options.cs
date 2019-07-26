using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    class Options
    {
        [Option('i', "ipath", HelpText = "input file path", Default = @"C:\Users\hsc\Downloads\admsenrichertest\idf")]
        internal string InputPath { get; set; } = @"C:\Users\hsc\Downloads\admsenrichertest\idf";

        [Option('o', "opath", HelpText = "output path", Default = @"C:\Users\hsc\Downloads\admsenrichertest\output")]
        internal string OutputPath { get; set; } = @"C:\Users\hsc\Downloads\admsenrichertest\output";

        [Option('d', "dpath", HelpText = "data path", Default = @"C:\Users\hsc\Downloads\admsenrichertest")]
        internal string DataPath { get; set; } = @"C:\Users\hsc\Downloads\admsenrichertest";
    }
}
