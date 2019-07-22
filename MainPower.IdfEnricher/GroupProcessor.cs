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

        internal void SetSymbolName(string id, string symbolName, double scale = double.NaN, double rotation = double.NaN, double z = double.NaN)
        {
            lock (Graphics)
            {
                if (Graphics.SelectSingleNode($"//element[@id=\"d_{id}\"]") is XmlElement node)
                {
                    node.SetAttribute("name", symbolName);
                    node.SetAttribute("library", "MPNZ.LIB2");
                    if (!scale.Equals(double.NaN))
                        node.SetAttribute("scale", scale.ToString("N3"));

                    if (!rotation.Equals(double.NaN))
                        node.SetAttribute("rotation", rotation.ToString("N0"));

                    if (!z.Equals(double.NaN))
                        node.SetAttribute("z", z.ToString("N1"));
                }
            }
        }

        internal void AddColorLink (string id)
        {
            lock (Graphics)
            {
                if (Graphics.SelectSingleNode($"//element[@id=\"d_{id}\"]") is XmlElement node)
                {
                    XmlDocumentFragment f = Graphics.CreateDocumentFragment();
                    f.InnerXml = $"<colorLink id=\"{id}\"><link d=\"EMAP_COLOR\" o=\"EMAP_DEVICE_COLOR\" f=\"ColorAggregate\" i=\"0\" identityType=\"Key\" /></colorLink>";
                    node.AppendChild(f);
                }
            }
        }

        internal void  Process()
        {
            try
            {
                //throw errors to caller
                Graphics = new XmlDocument();
                Graphics.Load($"{Enricher.Singleton.Options.Path}\\idf\\{Id}_display.xml");
                ReplaceSymbolLibraryName();
                DeleteTextElements();
                Data = new XmlDocument();
                Data.Load($"{Enricher.Singleton.Options.Path}\\idf\\{Id}_data.xml");

                var tasks = new List<Task>();
                var nodes = Data.SelectNodes($"//group[@id=\"{Id}\"]/element");
                foreach (XmlElement node in nodes)
                {
                    DeviceProcessor d = null;
                    var elType = node.Attributes["type"].InnerText;
                    switch (elType)
                    {
                        case "Switch":
                            d = new SwitchProcessor(node, this);
                            Enricher.Singleton.SwitchCount++;
                            break;
                        case "Transformer":
                            d = new TransformerProcessor(node, this);
                            Enricher.Singleton.TransformerCount++;
                            break;
                        case "Line":
                            Enricher.Singleton.LineCount++;
                            d = new LineProcessor(node, this);
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

        private void ReplaceSymbolLibraryName()
        {
            foreach (XmlNode var in Graphics.SelectNodes("//element[@library=\"OSI.LIB2\"]"))
            {
                var.Attributes["library"].InnerText = "MPNZ.LIB2";
            }
        }


        private void DeleteTextElements()
        {
            foreach (XmlNode var in Graphics.SelectNodes("//element[@type=\"Text\"]"))
            {
                var.ParentNode.RemoveChild(var);
            }
        }

        internal void AddGroupElement (string xml)
        {
            XmlDocumentFragment f = Data.CreateDocumentFragment();
            f.InnerXml = xml;
            var node = Data.SelectSingleNode($"//group[@id=\"{Id}\"]");
            node.AppendChild(f);
        }

        internal void AddScadaCommand(string id, string key)
        {
            try
            {
                string xmlc = "<command instance=\"Active\" plugin=\"SCADA Interface\" topic=\"OpenPointDialog\"><field name=\"LinkString\" value=\"@URL\" /></command>";
                string xmldl = $"<dataLink dsID=\"{key}\"><link d=\"SCADA\" f=\"State\" i=\"0\" identityType=\"Key\" o=\"STATUS\"></link></dataLink>";

                XmlDocumentFragment fc = Graphics.CreateDocumentFragment();
                XmlDocumentFragment fdl = Graphics.CreateDocumentFragment();
                fc.InnerXml = xmlc;
                fdl.InnerXml = xmldl;

                var node = Graphics.SelectSingleNode($"//element[@id=\"d_{id}\"]");
                //var cnode = node.SelectSingleNode("./command");
                var dlnode = node.SelectSingleNode("./dataLink");
                //node.RemoveChild(cnode);
                node.RemoveChild(dlnode);
                node.AppendChild(fdl);
                //node.AppendChild(fc);
            }
            catch (Exception ex)
            {
                _log.Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
