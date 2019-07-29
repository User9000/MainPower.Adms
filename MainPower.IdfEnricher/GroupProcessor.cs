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

        internal void SetSwitchInSubstation(string value)
        {
            throw new NotImplementedException();
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
        internal void AddDatalink(string id)
        {
            foreach (var idf in FileManager.I.GroupFiles[Id].GraphicFiles)
            {
                var symbols = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Descendants("colorLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var symbol in symbols)
                {
                    var parent = symbol.Parent;
                    XElement x = new XElement("dataLink",
                        new XAttribute("id", id),
                        new XElement("link",
                            new XAttribute("d", "EMAP"),
                            new XAttribute("f", "AggregateState"),
                            new XAttribute("i", "0"),
                            new XAttribute("identityType", "Key"),
                            new XAttribute("o", "EMAP_DEVICE")
                        )
                    );
                    parent.Add(x);
                }
            }
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
            var tasks = new List<Task>();
            var dataidf = fm.GroupFiles[Id].DataFile;
            if (dataidf == null)
                return;
            var nodes = dataidf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element");
            foreach (var node in nodes)
            {
                DeviceProcessor d = null;
                var elType = node.Attribute("type").Value;
                switch (elType)
                {
                    case "Switch":
                        d = new SwitchProcessor(node, this);
                        Enricher.I.Model.AddDevice(node, Id, DeviceType.Switch);
                        Enricher.I.SwitchCount++;
                        break;
                    case "Transformer":
                        d = new TransformerProcessor(node, this);
                        Enricher.I.Model.AddDevice(node, Id, DeviceType.Transformer);
                        Enricher.I.TransformerCount++;
                        break;
                    case "Line":
                        Enricher.I.LineCount++;
                        Enricher.I.Model.AddDevice(node, Id, DeviceType.Line);
                        d = new LineProcessor(node, this);
                        break;
                    case "Load":
                        Enricher.I.LoadCount++;
                        d = new LoadProcessor(node, this);
                        //Enricher.Singleton.Model.AddDevice(node, Id, DeviceType.Load);
                        break;
                    case "Feeder":
                        Enricher.I.LoadCount++;
                        d = new FeederProcessor(node, this);
                        break;
                    case "Circuit":
                        Enricher.I.LoadCount++;
                        d = new CircuitProcessor(node, this);
                        break;
                    case "Regulator":
                        Enricher.I.LoadCount++;
                        d = new RegulatorProcessor(node, this);
                        Enricher.I.Model.AddDevice(node, Id, DeviceType.Regulator);
                        break;
                    case "Substation":
                        Enricher.I.LoadCount++;
                        d = new SubstationProcessor(node, this);
                        break;
                    case "Area":
                        Enricher.I.LoadCount++;
                        d = new AreaProcessor(node, this);
                        break;
                    case "Region":
                        Enricher.I.LoadCount++;
                        d = new RegionProcessor(node, this);
                        break;
                    case "Source":
                        Enricher.I.Model.AddSource(node, Id);
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
            var groupnode = FileManager.I.GroupFiles[Id].DataFile.Content.Descendants("group").Where(n => n.Attribute("id")?.Value == Id).First();
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
