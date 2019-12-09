using CommandLine;

namespace MainPower.Adms.Enricher
{
    public class Options
    {
        [Option('i', "idfinput", HelpText = "Input IDF file path", Required = true)]
        public string InputPath { get; set; }

        [Option('o', "output", HelpText = "Output path", Required = true)]
        public string OutputPath { get; set; }

        [Option('m', "model", HelpText = "Model file path", Required = false)]
        public string Model { get; set; }

        [Option('d', "data", HelpText = "Data path", Required = true)]
        public string DataPath { get; set; }

        [Option('D', "debug", HelpText = "Debug Level", Required = false)]
        public int Debug { get; set; }

        [Option('n', "newmodel", HelpText = "Start with a blank model", Default = false)]
        public bool BlankModel { get; set; }

        [Option('p', "pause", HelpText = "Pause before quitting", Default = true)]
        public bool PauseBeforeQuit { get; set; }

        [Option('s', "exportshp", HelpText = "Export shape files", Default = true)]
        public bool ExportShapeFiles { get; set; }

        [Option("threads", HelpText = "The number of threads for group processing", Default = 10)]
        public int Threads { get; set; }
    }
}
