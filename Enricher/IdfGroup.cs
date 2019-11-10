using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{ 
    public class IdfGroup : IdfElement
    {
        private const string SYMBOL_DATALINK = "Symbol 23";

        private XElement _dataGroup = null;
        private Dictionary<string, XElement> _displayGroups = new Dictionary<string, XElement>();
        public bool NoData
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

        public IdfGroup(XElement node, IdfGroup processor) : base(node, processor) { }

        /// <summary>
        /// Finds all symbol elements with the provided datalink id, and sets the symbol parameters
        /// </summary>
        /// <param name="id"></param>
        /// <param name="symbolName"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <param name="z"></param>
        public void SetSymbolNameByDataLink(string id, string symbolName, double scale = double.NaN, double iScale = double.NaN, double rotation = double.NaN, double z = double.NaN)
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

        public void AddColorToLine(string id, Color c)
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

        public void UpdateLinkId(string oldId, string newId)
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

        public void CreateDataLinkSymbol(string id)
        {
            foreach (var group in _displayGroups)
            {
                if (group.Key != "MainPower")
                    continue;
                var dataLinks = group.Value.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol").Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id).ToList();
                foreach (var dataLink in dataLinks)
                {
                    var parent = dataLink.Parent;

                    double x = double.Parse(parent.Attribute("x").Value) - 1;
                    double y = double.Parse(parent.Attribute("y").Value);

                    XElement symbol = new XElement("element");
                    symbol.Add(new XAttribute("id", parent.Attribute("id").Value + "_dlink"));
                    symbol.Add(new XAttribute("type", "Symbol"));
                    symbol.Add(new XAttribute("x", x.ToString()));
                    symbol.Add(new XAttribute("y", y.ToString()));
                    symbol.Add(new XAttribute("overlay", parent.Attribute("overlay")?.Value?? "Overlay_MainPower_Default"));
                    symbol.Add(new XAttribute("layer", parent.Attribute("layer")?.Value?? "Layer_MainPower_Internals"));
                    symbol.Add(new XAttribute("library", "MPNZ.LIB2"));
                    symbol.Add(new XAttribute("z", "4"));
                    symbol.Add(new XAttribute("name", SYMBOL_DATALINK));
                    symbol.Add(new XAttribute("scale", "0.2"));
                    symbol.Add(new XAttribute("maxSize", "30"));

                    XElement command = new XElement("command");
                    command.Add(new XAttribute("topic", "Jump to Tabular"));
                    command.Add(new XAttribute("plugin", "eMap"));
                    command.Add(new XAttribute("instance", "Active"));
                    symbol.Add(command);

                    XElement field1 = new XElement("field");
                    field1.Add(new XAttribute("name", "Data Link URL"));
                    field1.Add(new XAttribute("value", "@URL"));
                    command.Add(field1);

                    XElement field2 = new XElement("field");
                    field2.Add(new XAttribute("name", "Data Mode"));
                    field2.Add(new XAttribute("value", "@MODE"));
                    command.Add(field2);

                    XElement field3 = new XElement("field");
                    field3.Add(new XAttribute("name", "Data Instance"));
                    field3.Add(new XAttribute("value", "@I"));
                    command.Add(field3);

                    XElement field4 = new XElement("field");
                    field4.Add(new XAttribute("name", "Tabular Type"));
                    field4.Add(new XAttribute("value", "Detail"));
                    command.Add(field4);

                    XElement datalink = new XElement("dataLink");
                    datalink.Add(new XAttribute("id", id));
                    symbol.Add(dataLink);


                    XElement link = new XElement("link");
                    link.Add(new XAttribute("d", "EMAP"));
                    link.Add(new XAttribute("o", "EMAP_DEVICE"));
                    link.Add(new XAttribute("f", "AggregateState"));
                    link.Add(new XAttribute("i", "0"));
                    link.Add(new XAttribute("identityType", "Key"));
                    datalink.Add(link);

                    group.Value.Add(symbol);

                }
            }
        }
        
        private void CheckDataLinks()
        {
            foreach (var group in _displayGroups.Values)
            {
                var dataLinks = group.Descendants("element").Descendants("dataLink");
                foreach (var dataLink in dataLinks)
                {
                    if (!_dataGroup.Descendants("element").Where(x => x.Attribute("id")?.Value == dataLink.Attribute("id")?.Value).Any())
                    {
                        //TODO: actually we can't check these without the data...
                        //TODO: we could check the cache.
                        Err("Datalink ");
                    }
                }
            }
        }

        //TODO: backport to extractor
        public void AddDataAndFlowlink(string id)
        {
            foreach (var group in _displayGroups.Values)
            {
                var symbols = group.Descendants("element").Descendants("colorLink").Where(n => n.Attribute("id")?.Value == id);
                foreach (var symbol in symbols)
                {
                    var parent = symbol.Parent;

                    if (!parent.Descendants("flowLink").Any())
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

        public override void Process()
        {
            var tasks = new List<Task>();

            var nodes = _dataGroup.Descendants("element");
            foreach (var node in nodes.ToList())
            {
                IdfElement d = null;
                var elType = node.Attribute("type").Value;

                switch (elType)
                {
                    case "Switch":
                        d = new IdfSwitch(node, this);
                        Enricher.I.SwitchCount++;
                        break;
                    case "Transformer":
                        d = new IdfTransformer(node, this);
                        Enricher.I.TransformerCount++;
                        break;
                    case "Line":
                        Enricher.I.LineCount++;
                        d = new IdfLine(node, this);
                        break;
                    case "Load":
                        Enricher.I.LoadCount++;
                        d = new IdfLoad(node, this);
                        break;
                    case "Feeder":
                        Enricher.I.LoadCount++;
                        d = new IdfFeeder(node, this);
                        break;
                    case "Circuit":
                        Enricher.I.LoadCount++;
                        d = new IdfCircuit(node, this);
                        break;
                    case "Regulator":
                        Enricher.I.LoadCount++;
                        d = new IdfRegulator(node, this);
                        break;
                    case "Substation":
                        Enricher.I.LoadCount++;
                        d = new IdfSubstation(node, this);
                        break;
                    case "Capacitor":
                        Enricher.I.CapCount++;
                        d = new IdfCapacitor(node, this);
                        break;
                    case "Area":
                        Enricher.I.LoadCount++;
                        d = new IdfArea(node, this);
                        break;
                    case "Region":
                        Enricher.I.LoadCount++;
                        d = new IdfRegion(node, this);
                        break;
                    case "Source":
                        d = new IdfSource(node, this);
                        Enricher.I.Model.AddSource(node, Id);
                        break;
                    case "Generator":
                        d = new IdfGenerator(node, this);
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

        /// <summary>
        /// Adds an element to the first data file containing the group
        /// </summary>
        /// <param name="xml"></param>
        public void AddGroupElement (XElement xml)
        {
            _dataGroup.Add(xml);
        }

        /// <summary>
        /// Returns the point for a symbol with datalink
        /// </summary>
        /// <param name="id">The datalink id</param>
        public List<Point> GetSymbolGeometry(string id)
        {
            var points = new List<Point>();
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
                            Point p = new Point();
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
                Err($"Uncaught exception in GetSymbolGeometry [{id}]:{ex.Message}");
            }
            Warn("Could not locate symbol geometry", id, "");
            return points;
        }

        /// <summary>
        /// Returns the point for a symbol with datalink
        /// </summary>
        /// <param name="id">The datalink id</param>
        public List<Point> GetLineGeometry(string id)
        {
            var points = new List<Point>();
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
                        Point p = new Point();
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

        private Point TranslatePoint(Point p)
        {
            p.X -= 250000;
            p.Y -= 250000;
            p.X *= 0.4552;
            p.Y *= 0.4552;
            p.X += 19252195.535;
            p.Y -= 5288390.101;

            p = ProjectionTransforms.MetersToLatLon(p);
 
            return p;
        }

        public void AddScadaDatalink(string id, string key)
        {
            try
            {
                foreach (var group in _displayGroups.Values)
                {
                    var symbols = group.Descendants("element").Where(x=> x.Attribute("type")?.Value == "Symbol" && x.Attribute("name")?.Value != SYMBOL_DATALINK).Descendants("dataLink").Where(n => n.Attribute("id")?.Value == id);
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

        public void AddDatalinkToText(string id)
        {
            try
            {
                foreach (var group in _displayGroups.Values)
                {
                    var symbols = group.Descendants("element").Where(x => x.Attribute("type")?.Value == "Text" && x.Attribute("id").Value == $"d_t_{id}");
                    foreach (var symbol in symbols.ToList())
                    {
                        if (!symbol.Descendants("dataLink").Any())
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
                            symbol.Add(x);
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

