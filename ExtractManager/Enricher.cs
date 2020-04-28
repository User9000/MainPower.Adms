using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MainPower.Adms.ExtractManager
{
    public class Enricher
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Settings _settings;

        public Enricher(Settings settings)
        {
            _settings = settings;
        }

        public async Task<EnricherResult> RunEnricher(CancellationToken token, string inputPath, string outputPath, ExtractType extractType, string modelPath = "", bool processDeletedGroups = false)
        {
            string arguments = "";
            arguments += $" -i \"{inputPath}\"";
            arguments += $" -o \"{outputPath}\"";
            arguments += $" -d \"{_settings.EnricherDataPath}\"";
            arguments += $" -D {_settings.EnricherDebug}";
            arguments += $" --threads {_settings.EnricherThreads}";

            if (_settings.EnricherExportShapeFiles)
                arguments += " -s";

            if (extractType == ExtractType.Full)
            {
                arguments += " -n";
                if (processDeletedGroups)
                {
                    arguments += $" -g \"{Path.Combine(Path.GetDirectoryName(modelPath), "groups.dat")}\"";
                }
            }
            else
                arguments += $" -m \"{modelPath}\"";
            _log.Info("Running the enricher with the arguments: " + arguments);
            await RunProcessAsync(_settings.EnricherPath, arguments);
            return Util.DeserializeNewtonsoft<EnricherResult>(System.IO.Path.Combine(outputPath, "result.json")) ?? new EnricherResult();
        }

        private static Task<int> RunProcessAsync(string fileName, string arguments)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName, Arguments = arguments },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}

