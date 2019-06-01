using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
    class GroupProcessor
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal XmlDocument Graphics { get; private set; }
        internal XmlDocument Data { get; private set; }
        internal string Id { get; private set; }

        internal GroupProcessor(string id)
        {
            Id = id;   
        }

        internal void SetSymbolName(string id, string symbolName)
        {
            lock (Graphics)
            {
                if (Graphics.SelectSingleNode($"//element[@id=\"d_{id}\"]") is XmlElement node)
                {
                    node.SetAttribute("symbol", symbolName);
                    node.SetAttribute("library", "MPNZ.LIB2");
                    node.SetAttribute("scale", "10");
                }
            }
        }

        internal void  Process()
        {
            try
            {
                //throw errors to caller
                Graphics = new XmlDocument();
                Graphics.Load($"{Enricher.Singleton.Options.Path}\\{Id}_display.xml");
                Data = new XmlDocument();
                Data.Load($"{Enricher.Singleton.Options.Path}\\{Id}_data.xml");

                var tasks = new List<Task>();
                var nodes = Data.SelectNodes($"//group[@id=\"{Id}\"]/element");
                foreach (XmlElement node in nodes)
                {
                    DeviceProcessor d = null;
                    var elType = node.Attributes["type"].InnerText;
                    switch (elType)
                    {
                        case "Switch":
                            //d = new SwitchProcessor(node, this);
                            break;
                        case "Transformer":
                            d = new TransformerProcessor(node, this);
                            break;
                        default:
                            break;
                    }
                    if (d != null)
                    {
                        d.Process();
                    }
                }
                Directory.CreateDirectory($"{Enricher.Singleton.Options.Path}\\output\\");
                Data.Save($"{Enricher.Singleton.Options.Path}\\output\\{Id}_data.xml");
                Graphics.Save($"{Enricher.Singleton.Options.Path}\\output\\{Id}_display.xml");
            }
            catch (Exception ex)
            {
                _log.Error($"GROUP,,,{ex.Message}");
            }
        }
    }
}
