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
        private XElement _dataGroup = null;
        private Dictionary<string, XElement> _displayGroups = new Dictionary<string, XElement>();
        internal bool NoData
        {
            get
            {
                return _dataGroup == null;
            }
        }

        public void AddDisplayGroup(string displayName, XElement element)
        {
            //TODO validate
            _displayGroups.Add(displayName, element);
        }

        public void SetDataGroup (XElement element)
        {
            //TODO validate
            if (_dataGroup == null)
            {
                _dataGroup = element;
            }
            else
            {
                throw new Exception("Datagroup is already set");
            }
        }

        public Group(XElement node, Group processor) : base(node, processor) { }

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

#if !nofixes
        internal void SetStreetlightLayers()
        {
            //this shouold be called after everything else has been processed
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var streetlights = group.Descendants("element").Where(n => n.Attribute("type")?.Value == "Symbol" && n.Attribute("name")?.Value == "DEFAULT");
                foreach (var streetlight in streetlights)
                {
                    streetlight.SetAttributeValue("name", $"Symbol 24");
                    streetlight.SetAttributeValue("library", $"MPNZ.LIB2");
                    streetlight.SetAttributeValue("scale", $"1.0");
                    streetlight.SetAttributeValue("layer", $"Layer_{display}_SL");
                    streetlight.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                }

                streetlights = group.Descendants("element").Where(n => n.Attribute("type")?.Value == "Line" && n.Attribute("layer")?.Value == "Layer_MPWR_ALWAYS_ON");
                foreach (var streetlight in streetlights)
                {
                    streetlight.SetAttributeValue("layer", $"Layer_{display}_SL");
                    streetlight.SetAttributeValue("overlay", $"Overlay_{display}_Default");
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

        
        private void ReplaceSymbolLibraryName()
        {
            foreach (var group in _displayGroups.Values)
            {
                var symbols = group.Descendants("element").Where(n => n.Attribute("type")?.Value == "Symbol" && n.Attribute("library")?.Value == "OSI.LIB2");
                foreach (var symbol in symbols)
                {
                    symbol.SetAttributeValue("library", "MPNZ.LIB2");
                }
            }
        }

        public void SetLayerFromVoltage(string id, string basekV, bool isSwitch)
        {
            SetLayerFromVoltage(id, double.Parse(basekV), isSwitch);
        }

        public void SetLayerFromVoltage(string id, double basekV, bool isSwitch)
        {
            if (basekV < 1)
            {
                SetLayerFromDatalinkId(id, "LV", "Internals", "Default", "Default");
            }
            else if (basekV < 30)
            {
                if (isSwitch)
                    SetLayerFromDatalinkId(id, "HVSwitchgear", "Internals", "Default", "Default");
                else 
                    SetLayerFromDatalinkId(id, "HVDistribution", "Internals", "Default", "Default");
            }
            else  if (basekV < 100)
            {
                if (isSwitch)
                    SetLayerFromDatalinkId(id, "HVSwitchgear", "Internals", "Default", "Default");
                else
                    SetLayerFromDatalinkId(id, "HVSubtransmission", "Internals", "Default", "Default");
            }
        }

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

                var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");

                foreach (var text in texts.ToList())
                {
                    text.Remove();
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
                    pole.SetAttributeValue("scale", "1.0");
                }

                //move the tranny symbols onto the pole layer
                var trannys = group.Descendants("element").Where(n => n.Attribute("type").Value == "Symbol" && n.Attribute("name")?.Value == "Symbol 35");
                foreach (var tranny in trannys)
                {
                    tranny.SetAttributeValue("layer", $"Layer_{display}_Poles");
                    tranny.SetAttributeValue("overlay", $"Overlay_{display}_Default");
                    tranny.SetAttributeValue("scale", "1.0");
                    tranny.SetAttributeValue("z", "4");
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
                    text.SetAttributeValue("overlay", $"Overlay_{display}_Text");
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
                var elements = group.Descendants("element").Where(n => n.Attribute("mpwr_internals") != null);

                foreach (var element in elements.ToList())
                {
                    element.SetAttributeValue("mpwr_internals", null);
                }

            }
        }

        private void SetText()
        {
            foreach (var group in _displayGroups.Values)
            {
                var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");

                foreach (var text in texts.ToList())
                {
                    //extract id from text
                    var objId = text.Attribute("id").Value;
                    objId = objId.Substring(4, objId.Length - 4);

                    //TODO: convert to this
                    //objId = objId[4..];
                    objId = $"d_{objId}";

                    //find matching object
                    var device = group.Descendants("element").Where(n => n.Attribute("type").Value == "Symbol" && n.Attribute("id").Value == objId).FirstOrDefault();
                    if (device != null)
                    {
                        var x = double.Parse(device.Attribute("x").Value);
                        var y = double.Parse(device.Attribute("y").Value);

                        if (text.Attribute("mpwr_internals")?.Value == "True")
                        {
                            x += 0.25;
                            //text.SetAttributeValue("anchor", "Left");
                            //text.SetAttributeValue("x", x.ToString());
                            //text.SetAttributeValue("y", y.ToString());
                            text.SetAttributeValue("fixedSize", "False");
                            text.SetAttributeValue("maxFontSize", "50");
                            text.SetAttributeValue("fontSize", "0.5");
                        }
                        else
                        {
                            x += 1;
                            //text.SetAttributeValue("x", x.ToString());
                            //text.SetAttributeValue("y", y.ToString());
                            //text.SetAttributeValue("anchor", "Left");
                            text.SetAttributeValue("fixedSize", "False");
                            text.SetAttributeValue("maxFontSize", "50");
                            text.SetAttributeValue("fontSize", "16");
                        }
                    }
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
        internal void RemoveDataLinkFromSymbols(string id)
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
        /// Sets the width of all lines in the group
        /// </summary>
        /// <param name="width"></param>
        internal void SetGlobalLineWidth(int width = 4)
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

#endif

        private void SetText2()
        {
            foreach (var group in _displayGroups.Values)
            {
                var texts = group.Descendants("element").Where(n => n.Attribute("type").Value == "Text");

                foreach (var text in texts.ToList())
                {
                    text.SetAttributeValue("layer", "Layer_MainPower_Text");

                    if (text.Attribute("mpwr_internals")?.Value == "True")
                    {
                        text.SetAttributeValue("anchor", "Left");
                        text.SetAttributeValue("fixedSize", "False");
                        text.SetAttributeValue("maxFontSize", "50");
                        text.SetAttributeValue("fontSize", "0.5");
                    }
                    else
                    {
                        text.SetAttributeValue("anchor", "Left");
                        text.SetAttributeValue("fixedSize", "False");
                        text.SetAttributeValue("maxFontSize", "50");
                        text.SetAttributeValue("fontSize", "16");
                    }
                }
            }
        }

        private void SetPoles()
        {
            foreach (var group in _displayGroups.Values)
            {
                string display = group.Document.Root.Attribute("displayName").Value;
                var poles = group.Descendants("element").Where(n => n.Attribute("type").Value == "Symbol" && n.Attribute("name")?.Value == "Symbol 23");
                foreach (var pole in poles)
                {
                    pole.SetAttributeValue("scale", "1.0");
                }

                //move the tranny symbols onto the pole layer
                var trannys = group.Descendants("element").Where(n => n.Attribute("type").Value == "Symbol" && n.Attribute("name")?.Value == "Symbol 35");
                foreach (var tranny in trannys)
                {
                    tranny.SetAttributeValue("scale", "1.0");
                    tranny.SetAttributeValue("z", "4");
                }
            }
        }

        internal void AddColorToLine(string id, Color c)
        {
            foreach (var group in _displayGroups.Values)
            {
                var dataLinks = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var dataLink in dataLinks)
                {
                    var parent = dataLink.Parent;
                    parent.Descendants("color").Remove();
                    XElement color = new XElement("color", new XAttribute("red", c.R.ToString()), new XAttribute("green", c.G.ToString()), new XAttribute("blue", c.B.ToString()));
                    parent.Add(color);
                }
            }
        }

        internal void UpdateLinkId(string oldId, string newId)
        {
            foreach (var group in _displayGroups.Values)
            {
                var links = group.Descendants("element").Descendants("colorLink").Where(n => n.Attribute("id")?.Value == oldId);
                foreach (var link in links)
                {
                    link.SetAttributeValue("id", newId);
                }
                links = group.Descendants("element").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == oldId);
                foreach (var link in links)
                {
                    link.SetAttributeValue("id", newId);
                }

                links = group.Descendants("element").Descendants("flowLink").Where(n => n.Attribute("id")?.Value == oldId);
                foreach (var link in links)
                {
                    link.SetAttributeValue("id", newId);
                }

            }
        }

        //TODO: backport to extractor
        internal void AddDataAndFlowlink(string id)
        {
            foreach (var group in _displayGroups.Values)
            {
                var symbols = group.Descendants("element").Descendants("colorLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var symbol in symbols)
                {
                    var parent = symbol.Parent;
#if !nofixes
                    if (parent.Descendants("dataLink").Any())
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
#endif
                    if (parent.Descendants("flowLink").Any())
                    {

                        XElement x = new XElement("flowLink",
                            new XAttribute("id", id),
                            new XElement("link",
                                new XAttribute("d", "EMAP"),
                                new XAttribute("f", "AggregateFlow"),
                                new XAttribute("i", "0"),
                                new XAttribute("identityType", "Key"),
                                new XAttribute("o", "EMAP_LINE")
                            )
                        );
                        parent.Add(x);
                    }
                }
            }
        }

        internal override void Process()
        {
#if !nofixes
            //TODO: Backport into GIS Extractor
            SetGlobalLineWidth();
            ReplaceSymbolLibraryName();
            //DeleteTextElements();
            SetTextLayer();
            SetPoleLayer();
            SetText();
            SetStationOutlineLayer();
            SetInternalsOutlineLayer();
            //RemoveCapacitorDataLinksFromSymbols();
            SetPoles();
            SetText2();
#endif
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
                    case "Capacitor":
                        Enricher.I.CapCount++;
                        d = new Capacitor(node, this);
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
                    case "Generator":
                        d = new Generator(node, this);
                        break;
                    default:
                        break;
                }
                if (d != null)
                {
                    d.Process();
                }
            }
#if !nofixes
            SetStreetlightLayers();
            DeleteInternals();
#endif
        }

        /// <summary>
        /// Adds an element to the first data file containing the group
        /// </summary>
        /// <param name="xml"></param>
        internal void AddGroupElement (XElement xml)
        {
            _dataGroup.Add(xml);
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
            Warn("Could not locate line geometry", id, "");
            return points;
        }

        private PointD TranslatePoint(PointD p)
        {
            p.X -= 250000;
            p.Y -= 250000;
            p.X *= 0.4552;
            p.Y *= 0.4552;
            p.X += 19252195.535;
            p.Y -= 5288390.101;

            p = ProjectionTransforms.MetersToLatLon(p);
            //p.X /= 1e5;
            //p.Y /= 1e5;
            return p;
        }

        internal void AddScadaCommand(string id, string key)
        {
            try
            {
                foreach (var group in _displayGroups.Values)
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
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

