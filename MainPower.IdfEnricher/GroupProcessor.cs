using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    class GroupProcessor
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal string Id { get; private set; }

        internal GroupProcessor(string id)
        {
            Id = id;   
        }

        internal void SetSymbolName(string id, string symbolName, double scale = double.NaN, double rotation = double.NaN, double z = double.NaN)
        {
            foreach (var idf in FileManager.I.GroupFiles[Id].GraphicFiles)
            {
                var symbols = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var symbol in symbols)
                {
                    var parent = symbol.Parent;
                    parent.SetAttributeValue("name", symbolName);
                    parent.SetAttributeValue("library", "MPNZ.LIB2");
                    if (!scale.Equals(double.NaN))
                        parent.SetAttributeValue("scale", scale.ToString("N3"));

                    if (!rotation.Equals(double.NaN))
                        parent.SetAttributeValue("rotation", rotation.ToString("N0"));

                    if (!z.Equals(double.NaN))
                        parent.SetAttributeValue("z", z.ToString("N1"));
                }
            }
        }

        internal void AddColorLink (string id)
        {
            /*
            lock (Graphics)
            {
                if (Graphics.SelectSingleNode($"//element[@id=\"d_{id}\"]") is XmlElement node)
                {
                    XmlDocumentFragment f = Graphics.CreateDocumentFragment();
                    f.InnerXml = $"<colorLink id=\"{id}\"><link d=\"EMAP_COLOR\" o=\"EMAP_DEVICE_COLOR\" f=\"ColorAggregate\" i=\"0\" identityType=\"Key\" /></colorLink>";
                    node.AppendChild(f);
                }
            }
            */
            //TODO: implement this
        }

        private string FindFile(string id)
        {

            DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(Enricher.Singleton.Options.InputPath);
            FileInfo[] filesInDir = hdDirectoryInWhichToSearch.GetFiles(id + "*");

            foreach (FileInfo foundFile in filesInDir)
            {
                if (foundFile.Name.EndsWith("_data.xml"))
                    return foundFile.Name.Substring(0, foundFile.Name.Length - "_data.xml".Length);
            }
            return "";
        }

        internal void Process()
        {
            FileManager fm = FileManager.I;
            if (!fm.GroupFiles.ContainsKey(Id))
            {
                //TODO: log error
                return;
            }

            ReplaceSymbolLibraryName();
            DeleteTextElements();

            foreach (var idf in fm.GroupFiles[Id].DataFiles)
            {
                var tasks = new List<Task>();
                var nodes = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element");
                foreach (var node in nodes)
                {
                    DeviceProcessor d = null;
                    var elType = node.Attribute("type").Value;
                    switch (elType)
                    {
                        case "Switch":
                            d = new SwitchProcessor(node, this);
                            Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Switch);
                            Enricher.Singleton.SwitchCount++;
                            break;
                        case "Transformer":
                            d = new TransformerProcessor(node, this);
                            Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Transformer);
                            Enricher.Singleton.TransformerCount++;
                            break;
                        case "Line":
                            Enricher.Singleton.LineCount++;
                            Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Line);
                            d = new LineProcessor(node, this);
                            break;
                        case "Load":
                            Enricher.Singleton.LoadCount++;
                            d = new LoadProcessor(node, this);
                            //Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Load);
                            break;
                        case "Feeder":
                            Enricher.Singleton.LoadCount++;
                            d = new FeederProcessor(node, this);
                            break;
                        case "Circuit":
                            Enricher.Singleton.LoadCount++;
                            d = new CircuitProcessor(node, this);
                            break;
                        case "Regulator":
                            Enricher.Singleton.LoadCount++;
                            d = new RegulatorProcessor(node, this);
                            Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Regulator);
                            break;
                        case "Substation":
                            Enricher.Singleton.LoadCount++;
                            d = new SubstationProcessor(node, this);
                            break;
                        case "Area":
                            Enricher.Singleton.LoadCount++;
                            d = new AreaProcessor(node, this);
                            break;
                        case "Region":
                            Enricher.Singleton.LoadCount++;
                            d = new RegionProcessor(node, this);
                            break;
                        case "Source":
                            Enricher.Singleton.Model.AddSource(node, Id);
                            break;
                        default:
                            break;
                    }
                    if (d != null)
                    {
                        d.Process();
                    }
                }
            }
        }

        private void ReplaceSymbolLibraryName()
        {
            foreach (var idf in FileManager.I.GroupFiles[Id].GraphicFiles)
            {
                var symbols  = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Where(n => n.Attribute("type")?.Value == "Symbol" && n.Attribute("library")?.Value == "OSI.LIB2");
                foreach (var symbol in symbols)
                {
                    symbol.SetAttributeValue("library", "MPNZ.LIB2");
                }
            }
        }

        private void DeleteTextElements()
        {
            foreach (var idf in FileManager.I.GroupFiles[Id].GraphicFiles)
            {

                var texts = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Where(n => n.Attribute("type").Value == "Text");
                
                foreach (var text in texts.ToList())
                {
                    text.Remove();
                }
            }
        }

        /// <summary>
        /// Adds an element to the first data file containing the group
        /// </summary>
        /// <param name="xml"></param>
        internal void AddGroupElement (XElement xml)
        {
            var groupnode = FileManager.I.GroupFiles[Id].DataFiles[0].Content.Descendants("group").Where(n => n.Attribute("id")?.Value == Id).First();
            groupnode.Add(xml);
        }

        internal void AddScadaCommand(string id, string key)
        {
            try
            {
                foreach (var idf in FileManager.I.GroupFiles[Id].GraphicFiles)
                {
                    var symbols = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Descendants("dataLink").Where(n=> n.Attribute("id")?.Value == id);
                    foreach (var symbol in symbols.ToList())
                    {
                        var parent = symbol.Parent;
                        symbol.Remove();
                        XElement x = new XElement("dataLink",
                            new XAttribute("dsID", key),
                            new XElement("link",
                                new XAttribute("d", "SCADA"),
                                new XAttribute("f", "State"),
                                new XAttribute("i", "0"),
                                new XAttribute("identityType", "Key"),
                                new XAttribute("o", "STATUS")
                            )
                        );
                        parent.Add(x);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
