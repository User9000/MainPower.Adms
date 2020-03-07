using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MainPower.Adms.ScadaConverter;
using System.IO;
using System.IO.Compression;

namespace MainPower.Adms.ScadaConverter
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('i', "input", HelpText = "input location.", Required = true)]
        public string Input { get; set; }

        [Option('a', "archive", HelpText = "archive location.", Required = false)]
        public string Archive { get; set; }

        [Option('o', "output", HelpText = "output location.", Required = true)]
        public string Output { get; set; }

        [Option('t', "temp", HelpText = "temporary files location.", Required = true)]
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

                   if (o.Archive != "")
                   {
                       CreateArchive(o);
                   }

                   Console.WriteLine("All done....");
                   Console.ReadKey();
               });
        }

        static void CreateArchive(Options o)
        {
            var adir = Directory.CreateDirectory(Path.Combine(o.Archive, DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss")));
            var files = Directory.GetFiles(o.Output);


            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(adir.FullName, Path.GetFileName(file)));
            }
            try
            {
                ZipFile.CreateFromDirectory(adir.FullName, Path.Combine(o.Archive, "SCADA Export " + adir.Name + ".zip"));
                ZipFile.CreateFromDirectory(o.Input, Path.Combine(o.Archive, "SCADA Input Data " + adir.Name + ".zip"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Failed to create archive:" + ex.Message);
            }
        }

    }
}