using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MainPower.Adms.ExtractManager
{
    public class Enricher
    {
        private readonly Settings _settings;

        public Enricher(Settings settings)
        {
            _settings = settings;
        }

        public async Task<EnricherResult> RunEnricher(CancellationToken token, string inputPath, string outputPath, ExtractType extractType, string modelPath = "")
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
                arguments += " -n";
            else
                arguments += $" -m \"{modelPath}\"";

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

