using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Group : Element
    {
        private GroupSet _groupset = null;
        private XElement _group = null;

        internal bool NoGroup
        {
            get
            {
                return _groupset == null;
            }
        }


        internal bool NoData
        {
            get
            {
                return _group == null;
            }
        }

        public Group(XElement node, Group processor) : base(node, processor)
        {
            if (!FileManager.I.GroupFiles.ContainsKey(Id))
            {
                Error("There were no files containing this group");
                return;
            }
            _groupset = FileManager.I.GroupFiles[Id];
            if (_groupset.DataFile != null)
                _group = _groupset.DataFile.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).First();
        }

        /// <summary>
        /// Finds all symbol elements with the provided datalink id, and sets the symbol parameters
        /// </summary>
        /// <param name="id"></param>
        /// <param name="symbolName"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <param name="z"></param>
        internal void SetSymbolNameByDataLink(string id, string symbolName, double scale = double.NaN, double rotation = double.NaN, double z = double.NaN)
        {
            foreach (var idf in _groupset.GraphicFiles)
            {
                var dataLinks = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks)
                {
                    var symbol = dataLink.Parent;
                    symbol.SetAttributeValue("name", symbolName);
                    symbol.SetAttributeValue("library", "MPNZ.LIB2");
                    if (!scale.Equals(double.NaN))
                        symbol.SetAttributeValue("scale", scale.ToString("N3"));

                    if (!rotation.Equals(double.NaN))
                        symbol.SetAttributeValue("rotation", rotation.ToString("N0"));

                    if (!z.Equals(double.NaN))
                        symbol.SetAttributeValue("z", z.ToString("N1"));

                    symbol.SetAttributeValue("maxSize", "30");
                }
            }
        }

        /// <summary>
        /// Set the InSubstation of a Switch with the given id
        /// </summary>
        /// <param name="value"></param>
        internal void SetSwitchInSubstation(string id, string value)
        {
            var switches = _group.Descendants("element").Where(n => n.Attribute("id")?.Value == id);
            foreach (var sw in switches)
            {
                //TODO attribute name constant
                sw.SetAttributeValue("inSubstation", value);
            }
        }

        /// <summary>
        /// Finds all symbols with a given datalink id and deletes the datalink
        /// </summary>
        /// <param name="id"></param>
        internal void RemoveDataLinksFromSymbols(string id)
        {
            foreach (var idf in _groupset.GraphicFiles)
            {
                var dataLinks = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks.ToList())
                {
                    dataLink.Remove();
                }
            }
        }

        /// <summary>
        /// Sets the width of all lines in the group
        /// </summary>
        /// <param name="width"></param>
        internal void SetLineWidth(int width = 5)
        {
            foreach (var idf in _groupset.GraphicFiles)
            {
                var lines = idf.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).Descendants("element").Where(n => n.Attribute("type").Value == "Line");
                foreach (var line in lines)
                {
                    line.SetAttributeValue("width", width.ToString());
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
        internal void AddDatalink(string id)
        {
            foreach (var idf in _groupset.GraphicFiles)
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

        internal override void Process()
        {
            //TODO: Backport into GIS Extractor
            SetLineWidth();
            ReplaceSymbolLibraryName();
            DeleteTextElements();
            var tasks = new List<Task>();

            var nodes = _group.Descendants("element");
            foreach (var node in nodes.ToList())
            {
                Element d = null;
                var elType = node.Attribute("type").Value;
                /*
                if (elType == "Switch" || elType == "Transformer" || elType == "Line" || elType == "Load" || elType == "Regulator")
                {
                    if (!(node.Attribute("s1phaseID1").Value == "1"))
                        Fatal($"Side1 phase 1 isn't 1 {node.Attribute("id").Value}");
                    if (!(node.Attribute("s1phaseID2").Value == "2"))
                        Fatal($"Side1 phase 2 isn't 2 {node.Attribute("id").Value}");
                    if (!(node.Attribute("s1phaseID3").Value == "3"))
                        Fatal($"Side1 phase 3 isn't 3 {node.Attribute("id").Value}");
                    if (elType != "Load")
                    {
                        if (!(node.Attribute("s2phaseID1")?.Value == "1"))
                            Fatal($"Side2 phase 1 isn't 1 {node.Attribute("id").Value}");
                        if (!(node.Attribute("s2phaseID2").Value == "2"))
                            Fatal($"Side2 phase 2 isn't 2 {node.Attribute("id").Value}");
                        if (!(node.Attribute("s2phaseID3" +
                            "").Value == "3"))
                            Fatal($"Side2 phase 3 isn't 3 {node.Attribute("id").Value}");
                    }
                }
                */
                switch (elType)
                {
                    case "Switch":
                        d = new Switch(node, this);
                        Enricher.I.SwitchCount++;
                        break;
                    case "Transformer":
                        d = new Transformer(node, this);
                        Enricher.I.TransformerCount++;
                        break;
                    case "Line":
                        Enricher.I.LineCount++;
                        d = new Line(node, this);
                        break;
                    case "Load":
                        Enricher.I.LoadCount++;
                        d = new Load(node, this);
                        //Enricher.I.Model.AddDevice(node, Id, DeviceType.Load);
                        break;
                    case "Feeder":
                        Enricher.I.LoadCount++;
                        d = new Feeder(node, this);
                        break;
                    case "Circuit":
                        Enricher.I.LoadCount++;
                        d = new Circuit(node, this);
                        break;
                    case "Regulator":
                        Enricher.I.LoadCount++;
                        d = new Regulator(node, this);
                        break;
                    case "Substation":
                        Enricher.I.LoadCount++;
                        d = new Substation(node, this);
                        break;
                    case "Area":
                        Enricher.I.LoadCount++;
                        d = new Area(node, this);
                        break;
                    case "Region":
                        Enricher.I.LoadCount++;
                        d = new Region(node, this);
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
            foreach (var idf in _groupset.GraphicFiles)
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
            foreach (var idf in _groupset.GraphicFiles)
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
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
