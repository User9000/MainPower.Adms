using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Adms.ExtractManager
{
    public enum ExtractType
    {
        Full,
        Incremental,
        Groups,//not supported yet
    }
    class Extractor
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Settings _settings;

        public Extractor(Settings s)
        {
            _settings = s;
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
        }

        public async Task RequestExtract(CancellationToken token, string id, ExtractType type, string extractorPath)
        {
            try
            {
                string extracttype = type switch
                {
                    ExtractType.Full => "complete",
                    ExtractType.Incremental => "incremental",
                    _ => "incremental"
                };
                string request_string = $"{_settings.GisServerUrl}?request=export&export_type={extracttype}&path={Uri.EscapeDataString(extractorPath)}&correlation_id={id}";
                var method = new HttpMethod("GET");
                var request = new HttpRequestMessage(method, request_string);
                var res = await client.SendAsync(request);
                var message = await res.Content.ReadAsStringAsync();

                XDocument doc = XDocument.Parse(message);
                if (res.StatusCode == System.Net.HttpStatusCode.OK && doc?.Element("return")?.Element("service_response")?.Element("status")?.Value == "OK")
                {
                    await CheckForIdfOutput(extractorPath, token);
                }
                else
                {
                    throw new ExtractorException($"Could not trigger an extract.  Message from gis server:{message}");
                }
            }
            catch (Exception ex)
            {
                throw new ExtractorException("Exception occured during extraction, see inner exception for details", ex);
            }
        }


        private async Task CheckForIdfOutput(string path, CancellationToken token)
        {
            _log.Info($"Waiting for IDFs to turn up in {path}");
            //check for idf output every 10 seconds, or until cancelled
            while (!token.IsCancellationRequested)
            {
                if (File.Exists(Path.Combine(path, "ImportConfig.xml")))
                {
                    _log.Info("IDF ouptut has arrived... waiting for file transfers to complete");
                    //lets give it another 10s just in case some file transfers are still completing
                    await Task.Delay(10 * 1000);
                    return;
                }
                if (File.Exists(Path.Combine(path, "abort.xml")))
                {
                    _log.Error("The extractor has aborted.");
                    throw new ExtractorException("The extractor has aborted.");
                }
                //check again in 10 seconds
                await Task.Delay(10 * 1000);
            }
            _log.Info($"{nameof(CheckForIdfOutput)} aborting as cancellation token was recieved");
        }

        public async Task Complete()
        {
            string request_string = $"{_settings.GisServerUrl}?request=update_export_checkpoints";
            var method = new HttpMethod("GET");
            var request = new HttpRequestMessage(method, request_string);
            var res = await client.SendAsync(request);
            var message = await res.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(message);
            if (res.StatusCode == System.Net.HttpStatusCode.OK && doc.Root?.Element("service_response")?.Element("status")?.Value == "OK")
            {
                return;
            }
            else
            {
                throw new Exception($"Failed to send maestro success message to GIS (http status: {res.StatusCode}).  Message from gis server:{message}");
            }
        }
    }
}
