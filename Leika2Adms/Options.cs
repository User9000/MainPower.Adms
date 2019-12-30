using CommandLine;

namespace MainPower.Adms.Leika2Adms
{
    public class Options
    {
        [Option('l', "leikapath", HelpText = "Leika file path", Required = true)]
        public string LeikaPath { get; set; }

        [Option('c', "conductors", HelpText = "Conductor csv file", Required = true)]
        public string Conductors{ get; set; }

        [Option('o', "output", HelpText = "Output xml file", Required = true)]
        public string Output { get; set; }
    }
}
