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

        private System.Timers.Timer _timer;
        private Settings _settings;
        public Extractor(Settings s)
        {
            _settings = s;
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
        }

        public async Task RequestExtract(CancellationToken token, string id, ExtractType type)
        {
            //TODO: encode the path
            string extracttype = type switch
            {
                ExtractType.Full => "complete",
                ExtractType.Incremental => "incremental"
            };
            string request_string = $"{_settings.GisServerUtl}?request=export&export_type={extracttype}&path={_settings.EnricherPath}&correlation_id={id}";
            var method = new HttpMethod("GET");
            var request = new HttpRequestMessage(method, request_string);
            var res = await client.SendAsync(request);
            var message = await res.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(message);
            if (res.StatusCode == System.Net.HttpStatusCode.OK && doc?.Element("service_response")?.Element("status")?.Value == "OK")
            {
                await CheckForIdfOutput(_settings.EnricherDataPath, token);
            }
            else
            {
                throw new Exception($"Could not trigger an extract.  Message from gis server:{message}");
            }
        }


        private async Task CheckForIdfOutput(string path, CancellationToken token)
        {
            //check for idf output every 10 seconds, or until cancelled
            while (!token.IsCancellationRequested)
            {
                if (File.Exists(path))
                {
                    _log.Info("IDF ouptut has arrived");
                    return;
                }
                await Task.Delay(10 * 1000);
            }
        }

        public async Task Complete()
        {
            string request_string = $"{_settings.GisServerUtl}?request=update_export_checkpoints";
            var method = new HttpMethod("GET");
            var request = new HttpRequestMessage(method, request_string);
            var res = await client.SendAsync(request);
            var message = await res.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(message);
            if (res.StatusCode == System.Net.HttpStatusCode.OK && doc?.Element("service_response")?.Element("status")?.Value == "OK")
            {
                return;
            }
            else
            {
                throw new Exception($"Could not trigger an extract.  Message from gis server:{message}");
            }
        }
    }
}
