using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Group : Element
    {
        private GroupSet _groupset = null;
        private XElement _dataGroup = null;
        private Dictionary<string, XElement> _displayGroups = new Dictionary<string, XElement>();

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
                return _dataGroup == null;
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
                _dataGroup = _groupset.DataFile.Content.Descendants("group").Where(n => n.Attribute("id").Value == Id).First();

            foreach (var idf in _groupset.GraphicFiles)
            {
                var name = idf.Content.Descendants("data").First().Attribute("displayName").Value;
                var group = idf.Content.Descendants("group").Where(x => x.Attribute("id").Value == Id).FirstOrDefault();
                if (group != null)
                {
                    _displayGroups.Add(name, group);
                }
            }
        }

        /// <summary>
        /// Finds all symbol elements with the provided datalink id, and sets the symbol parameters
        /// </summary>
        /// <param name="id"></param>
        /// <param name="symbolName"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <param name="z"></param>
        internal void SetSymbolNameByDataLink(string id, string symbolName, double scale = double.NaN, double iScale = double.NaN, double rotation = double.NaN, double z = double.NaN)
        {
            foreach (var group in _displayGroups.Values)
            {
                var dataLinks = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks)
                {
                    var symbol = dataLink.Parent;
                    symbol.SetAttributeValue("name", symbolName);
                    symbol.SetAttributeValue("library", "MPNZ.LIB2");

                    if (!scale.Equals(double.NaN))
                        symbol.SetAttributeValue("scale", scale.ToString("N3"));

                    bool internals = symbol.Attribute("mpwr_internals")?.Value == "True";
                    if (internals)
                    {
                        if (!iScale.Equals(double.NaN))
                        {
                            symbol.SetAttributeValue("scale", iScale.ToString("N3"));
                        }
                    }
                    
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
            var switches = _dataGroup.Descendants("element").Where(n => n.Attribute("id")?.Value == id);
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
            foreach (var group in _displayGroups.Values)
            {
                var dataLinks = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks.ToList())
                {
                    dataLink.Remove();
                }
            }
        }

        /// <summary>
        /// Finds all symbols with datalinks referring to capacitors and deletes them
        /// </summary>
        /// <param name="id"></param>
        internal void RemoveCapacitorDataLinksFromSymbols()
        {
            foreach (var group in _displayGroups.Values)
            {
                var dataLinks = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value.StartsWith("mpwr_capac") ?? false);
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
        internal void SetLineWidth(int width = 4)
        {
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var lines = group.Descendants("element").Where(n => n.Attribute("type").Value == "Line");
                    foreach (var line in lines)
                    {
                        line.SetAttributeValue("width", width.ToString());
                    }
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
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var symbols = group.Descendants("element").Descendants("colorLink").Where(n => n.Attribute("id")?.Value == id);
                    foreach (var symbol in symbols)
                    {
                        var parent = symbol.Parent;
                        if (parent.Descendants("dataLink").Count() == 0)
                        {
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
            }
        }

        internal void AddMissingPhases(XElement node, bool onesideonly = false)
        {
            if (node.Attribute(IDF_DEVICE_S1_PHASEID1) == null)
                node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "");
            if (node.Attribute(IDF_DEVICE_S1_PHASEID2) == null)
                node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "");
            if (node.Attribute(IDF_DEVICE_S1_PHASEID3) == null)
                node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "");

            if (!onesideonly)
            {
                if (node.Attribute(IDF_DEVICE_S2_PHASEID1) == null)
                    node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "");
                if (node.Attribute(IDF_DEVICE_S2_PHASEID2) == null)
                    node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "");
                if (node.Attribute(IDF_DEVICE_S2_PHASEID3) == null)
                    node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "");
            }
        }

        internal override void Process()
        {
            //TODO: Backport into GIS Extractor
            SetLineWidth();
            ReplaceSymbolLibraryName();
            //DeleteTextElements();
            SetTextLayer();
            SetPoleLayer();
            SetTextSize();
            SetStationOutlineLayer();
            SetInternalsOutlineLayer();
            RemoveCapacitorDataLinksFromSymbols();

            var tasks = new List<Task>();

            var nodes = _dataGroup.Descendants("element");
            foreach (var node in nodes.ToList())
            {
                Element d = null;
                var elType = node.Attribute("type").Value;

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

            DeleteInternals();
        }

        private void ReplaceSymbolLibraryName()
        {
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var symbols = group.Descendants("element").Where(n => n.Attribute("type")?.Value == "Symbol" && n.Attribute("library")?.Value == "OSI.LIB2");
                    foreach (var symbol in symbols)
                    {
                        symbol.SetAttributeValue("library", "MPNZ.LIB2");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="basekV"></param>
        public void SetLayerFromVoltage(string id, string basekV)
        {
            SetLayerFromVoltage(id, double.Parse(basekV));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="basekV"></param>
        public void SetLayerFromVoltage(string id, double basekV)
        {
            if (basekV < 1)
            {
                SetLayerFromDatalinkId(id, "LV", "Internals", "Default", "Default");
            }
            else if (basekV < 30)
            {
                SetLayerFromDatalinkId(id, "HVDistribution", "Internals", "Default", "Default");
            }
            else  if (basekV < 100)
            {
                SetLayerFromDatalinkId(id, "HVSubtransmission", "Internals", "Default", "Default");
            }
        }

        /// <summary>
        /// Sets the layer of an element with the given DataLink id
        /// </summary>
        /// <param name="id">DataLink id</param>
        /// <param name="layer">layer id to set</param>
        /// <param name="internalsLayer">layer id to set (internals)</param>
        /// <param name="overlay">overlay id to set</param>
        /// <param name="internalsOverlay">overlay id to set (internals)</param>
        public void SetLayerFromDatalinkId(string id, string layer, string internalsLayer, string overlay, string internalsOverlay)
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var dataLinks = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks)
                {
                    var parent = dataLink.Parent;
                    var internals = parent.Attribute("mpwr_internals")?.Value == "True";
                    if (internals)
                    {
                        parent.SetAttributeValue("layer", $"Layer_{display}_{internalsLayer}");
                        parent.SetAttributeValue("overlay", $"Overlay_{display}_{internalsOverlay}");
                    }
                    else
                    {
                        parent.SetAttributeValue("layer", $"Layer_{display}_{layer}");
                        parent.SetAttributeValue("overlay", $"Overlay_{display}_{overlay}");
                    }
                }
            }
        }

        private void DeleteTextElements()
        {
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");

                    foreach (var text in texts.ToList())
                    {
                        text.Remove();
                    }
                }
            }
        }

        private void SetPoleLayer()
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var poles = group.Descendants("element").Where(n => n.Attribute("type").Value == "Symbol" && n.Attribute("name")?.Value == "Symbol 23");
                foreach (var pole in poles)
                {
                    pole.SetAttributeValue("layer", $"Layer_{display}_Poles");
                    pole.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                }
            }
        }

        private void SetTextLayer()
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");
                foreach (var text in texts)
                {
                    text.SetAttributeValue("layer", $"Layer_{display}_Text");
                    text.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                }
            }
        }

        private void SetInternalsOutlineLayer()
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var internals = group.Descendants("color").Where(n => n.Attribute("red").Value == "255" && n.Attribute("green").Value == "0" && n.Attribute("blue").Value == "128");
                foreach (var intern in internals)
                {
                    intern.Parent.SetAttributeValue("layer", $"Layer_{display}_Internals");
                    intern.Parent.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                }
            }
        }

        private void SetStationOutlineLayer()
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var internals = group.Descendants("color").Where(n => n.Attribute("red").Value == "128" && n.Attribute("green").Value == "128" && n.Attribute("blue").Value == "0");
                foreach (var intern in internals)
                {
                    intern.Parent.SetAttributeValue("layer", $"Layer_{display}_Stations");
                    intern.Parent.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                }
            }
        }


        private void DeleteInternals()
        {
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var elements = group.Descendants("element").Where(n => n.Attribute("mpwr_internals") != null);

                    foreach (var element in elements.ToList())
                    {
                        element.SetAttributeValue("mpwr_internals", null);
                    }
                }
            }
        }

        private void SetTextSize()
        {
            foreach (var group in _displayGroups.Values)
            {
                //lock (idf)
                {
                    var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");

                    foreach (var text in texts.ToList())
                    {
                        text.SetAttributeValue("fixedSize", "True");
                        text.SetAttributeValue("fontSize", "4");
                    }
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

        /// <summary>
        /// Returns the point for a symbol with datalink
        /// </summary>
        /// <param name="id">The datalink id</param>
        internal List<PointD> GetSymbolGeometry(string id)
        {
            var points = new List<PointD>();
            try
            {

                foreach (var group in _displayGroups.Values)
                {
                    //lock (idf)
                    {
                        var elements = group.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol");
                        var res = elements.Descendants("dataLink").Where(x => x.Attribute("id")?.Value == id).FirstOrDefault();
                        if (res != null)
                        {
                            PointD p = new PointD();
                            p.X = double.Parse(res.Parent.Attribute("x").Value);
                            p.Y = double.Parse(res.Parent.Attribute("y").Value);
                            p = TranslatePoint(p);
                            points.Add(p);
                            return points;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"Uncaught exception in GetSymbolGeometry [{id}]:{ex.Message}");
            }
            Warn("Could not locate symbol geometry", id, "");
            return points;
        }

        /// <summary>
        /// Returns the point for a symbol with datalink
        /// </summary>
        /// <param name="id">The datalink id</param>
        internal List<PointD> GetLineGeometry(string id)
        {
            var points = new List<PointD>();
            foreach (var kvp in _displayGroups)
            {
                if (kvp.Key != "MainPower")
                    continue;
                var group = kvp.Value;
                //lock (idf)
                {

                    var res = group.Descendants("element").Where(x => x.Attribute("type").Value == "Line").Descendants("dataLink").Where(x => x.Attribute("id").Value == id).FirstOrDefault();
                    if (res != null)
                    {
                        foreach (var xy in res.Parent.Descendants("xy"))
                        {
                            PointD p = new PointD();
                            p.X = float.Parse(xy.Attribute("x").Value);
                            p.Y = float.Parse(xy.Attribute("y").Value);
                            p = TranslatePoint(p);
                            points.Add(p);
                        }
                        return points;
                    }
                }
            }
            Warn("Could not locate line geometry", id, "");
            return points;
        }

        private PointD TranslatePoint(PointD p)
        {
            p.X *= 0.7;
            p.Y *= 0.7;
            p.X += 19050000;
            p.Y -= 5410000;
            return p;
        }

        internal void AddScadaCommand(string id, string key)
        {
            try
            {
                foreach (var group in _displayGroups.Values)
                {
                    //lock (idf)
                    {
                        var symbols = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
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
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}
