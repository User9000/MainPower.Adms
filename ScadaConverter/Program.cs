using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MainPower.Osi.ScadaConverter;

namespace MainPower.Osi.ScadaConverter
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('i', "input", HelpText = "input location.", Default = @"..\..\..\data\input\")]
        public string Input { get; set; }

        [Option('o', "output", HelpText = "output location.", Default = @"..\..\..\data\output\")]
        public string Output { get; set; }

        [Option('t', "temp", HelpText = "temporary files location.", Default = @"..\..\..\data\temp\")]
        public string Temp { get; set; }

        [Option('r', "rtu", HelpText = "Generate RTU data", Default = true)]
        public bool GenerateRTUInfo { get; set; }

        [Option("itdb", HelpText = "intouch databse file", Default = @"DB.csv")]
        public string InTouchDatabse { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o =>
               {
                   if (o.GenerateRTUInfo)
                       SCD5200TagReader.GenerateRTUTagInfo(o.Input);
                   InTouchExporter itr = new InTouchExporter(o);
                   //make sure to call the io before the memory so the calcs get deferred correctly
                   itr.ProcessIoDiscreteData();
                   itr.ProcessIoRealData();
                   itr.ProcessIoIntegerData();
                   itr.ProcessMemoryDiscreteData();
                   itr.ProcessMemoryRealData();
                   itr.ProcessMemoryIntegerData();
                   itr.ExportCompleteTagList();
                   itr.CheckForDuplicateIO();
                   itr.CheckForDuplicateNames();
                   itr.CheckCombinationPointsAreCombined();
                   itr.ValidateFeebackPoints();
                   itr.CopyRtus();
                   Console.WriteLine("All done....");
                   Console.ReadKey();
               });
        }
    }
}