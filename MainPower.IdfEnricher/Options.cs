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
        [Option('p', "path", HelpText = "default file path", Default = @"C:\Users\hsc\Downloads\admsenrichertest")]
        internal string Path { get; set; } = @"C:\Users\hsc\Downloads\admsenrichertest";
    }
}
