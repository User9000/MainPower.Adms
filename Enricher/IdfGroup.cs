using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{ 
    public class IdfGroup : IdfElement
    {
        //private const string SymbolDataLink = "Symbol 23";
        private const string GisDisplayName = "MainPower";

        private const double SymbolScaleSwitchHV = 20;
        private const double SymbolScaleRegulator = 13;
        private const double SymbolScaleTrannyPole = 17;
        private const double SymbolScalePole = 3;
        private const double SymbolScaleSwitchLV = 3;
        private const double SymbolScaleSwitchInternals = 0.2;
        private const double SymbolScaleSwitchSLD = 10;
        private const double SymbolScaleTransformerGIS = 0.3;
        private const double SymbolScaleTransformerSLD = 20;

        private XElement _dataGroup = null;
        private Dictionary<string, XElement> _displayGroups = new Dictionary<string, XElement>();

        #region Admin
        public bool NoData
        {
            get
            {
                return _dataGroup == null;
            }
        }
        public bool EmptyGroup
        {
            get
            {
                return _dataGroup?.IsEmpty ?? true;
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
        /// Adds an element to the first data file containing the group
        /// </summary>
        /// <param name="xml"></param>
        public void AddGroupElement(XElement xml)
        {
            _dataGroup.Add(xml);
        }

        #endregion
        
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
                        break;
                    case "Transformer":
                        d = new IdfTransformer(node, this);
                        break;
                    case "Line":
                        d = new IdfLine(node, this);
                        break;
                    case "Load":
                        d = new IdfLoad(node, this);
                        break;
                    case "Feeder":
                        d = new IdfFeeder(node, this);
                        break;
                    case "Substation Circuit":
                        d = new IdfCircuit(node, this);
                        break;
                    case "Regulator":
                        d = new IdfRegulator(node, this);
                        break;
                    case "Substation":
                        d = new IdfSubstation(node, this);
                        break;
                    case "Capacitor":
                        d = new IdfCapacitor(node, this);
                        break;
                    case "Area":
                        d = new IdfArea(node, this);
                        break;
                    case "Region":
                        d = new IdfRegion(node, this);
                        break;
                    case "Source":
                        d = new IdfSource(node, this);
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
        }

        public void ProcessGraphics()
        {
            foreach (var group in _displayGroups)
            {
                var geographic = group.Key == GisDisplayName;
                var display = group.Value;
                
                var symbols = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol" && x.Elements("dataLink").Any());
                
                foreach (var symbol in symbols.ToList())
                {
                    try
                    {
                        //we can assume that datalinks will be of the id type, not the dsID type
                        //nope. no we cannot assume that
                        var datalink = symbol.Element("dataLink").Attribute("id")?.Value;
                        if (string.IsNullOrWhiteSpace(datalink))
                        {
                            //probably a premise
                            datalink = symbol.Element("dataLink").Attribute("dsID")?.Value;
                            if (string.IsNullOrWhiteSpace(datalink))
                            {
                                //no datalink id at all, not much we can do with that
                                Err($"Data link id/dsID was missing for symbol {symbol.Attribute("id")?.Value} in display {group.Key}");
                                continue;
                            }
                        }
                        var device = Program.Enricher.Model.Devices.TryGetValue(datalink, out ModelDevice value) ? value : null;
                        if (device != null)
                        {
                            //if we have SCADA control of a device, then we have some special flags to set
                            if ((device.Flags | 0x04) == 0x04)
                            {
                                symbol.SetAttributeValue("selectable", "True");
                                symbol.SetAttributeValue("enterable", "False");
                            }
                            //set the symbol, size etc
                            if (!string.IsNullOrWhiteSpace(device.SymbolName))
                            {
                                SetSymbol(symbol, device, geographic);
                            }
                            if (geographic)
                            {
                                if (device.Type == DeviceType.Switch)
                                {
                                    var tagSize = device.Internals ? "0.5" : device.Base1kV < 1 ? "3" : "20";
                                    var x = new XElement("tagLink", new XAttribute("limit", "True"), new XAttribute("location", "Down"), new XAttribute("quality", "True"), new XAttribute("tag", "True"), new XAttribute("tagFontSize", tagSize));
                                    symbol.Add(x);

                                    //replace the standard with a better one 
                                    symbol.Element("command")?.Remove();
                                    var command = new XElement("command", new XAttribute("topic", "LoadDisplay"), new XAttribute("plugin", "Tabular Viewer"), new XAttribute("instance", "Active"));
                                    command.Add(new XElement("field", new XAttribute("name", "Name"), new XAttribute("value", "MainPower/SwitchDetails")));
                                    command.Add(new XElement("field", new XAttribute("name", "Parameters"), new XAttribute("value", "pDevice=@ER")));
                                    command.Add(new XElement("field", new XAttribute("name", "MenuVisibility"), new XAttribute("value", "HideMenu")));
                                    command.Add(new XElement("field", new XAttribute("name", "ViewportName"), new XAttribute("value", "DeviceDetails")));
                                    symbol.Add(command);
                                }
                            }
                        }
                        else
                        {
                            //the datalink isn't valid, so lets remove it
                            Err($"Removing invalid datalink [{datalink}] from symbol [{symbol.Attribute("id")?.Value}] in display file [{group.Key}]");
                            symbol.Element("dataLink")?.Remove();
                        }
                    }
                    catch (Exception ex)
                    {
                        Fatal($"Uncaught exception processing symbol: {ex.Message}");
                    }
                }

                //TODO: john should be doing this 
                //set the scaling of the tranny pole symbols
                symbols = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol" && x.Attribute("name")?.Value == "Symbol 35");
                foreach (var symbol in symbols.ToList())
                {
                    symbol.SetAttributeValue("scale", SymbolScaleTrannyPole);
                    symbol.SetAttributeValue("maxSize", "30");
                    symbol.SetAttributeValue("mpwr_internals", null);
                }

                //TODO: john should be doing this 
                //set the scaling of the pole symbols
                symbols = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol" && x.Attribute("name")?.Value == "Symbol 23");
                foreach (var symbol in symbols.ToList())
                {
                    symbol.SetAttributeValue("scale", SymbolScalePole);
                    symbol.SetAttributeValue("maxSize", "30");    
                }

                var lines = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Line" && x.Elements("colorLink").Any());
                foreach (var line in lines)
                {
                    try {
                        //we can assume that datalinks will be of the id type, not the dsID type
                        //TODO: john needs to set the data link in schematics
                        var datalink = line.Element("colorLink").Attribute("id")?.Value;
                        if (string.IsNullOrWhiteSpace(datalink))
                        {
                            Err($"Color link id was missing for line {line.Attribute("id")?.Value} in display { group.Key} - removing all data/flow/color links");
                            line.Element("dataLink")?.Remove();
                            line.Element("colorLink")?.Remove();
                            line.Element("flowLink")?.Remove();
                            continue;
                        }

                        var device = Program.Enricher.Model.Devices.TryGetValue(datalink, out ModelDevice value) ? value : null;
                        if (device != null)
                        {
                            if (device.Type == DeviceType.Line)
                                SetLineStyle(line, datalink, device.Base1kV, device.Color, device.Name.StartsWith("Service"));
                            else
                                Warn("Expected device type is Line.", device.Id, device.Name);
                            /*
                            if (geographic)
                            {
                        
                                var overlay = line.Attribute("overlay")?.Value;
                                if (overlay == "Overlay_Planned_mainpower")
                                {
                                    device.Flags |= 0x01;
                                    if (device.Energization[0])
                                        Warn("Planned device is energized", device.Id, device.Name);

                                }

                                if (overlay == "Overlay_OutOfService_mainpower")
                                {
                                    device.Flags |= 0x02;
                                    if (device.Energization[0])
                                        Warn("Out of service device is energized", device.Id, device.Name);
                                }
                        
                            }
                        */
                        }
                        else
                        {
                            Err($"Removing invalid data/flow/color links [{datalink}] from line [{line.Attribute("id")?.Value}] in display file [{group.Key}]");
                            line.Element("dataLink")?.Remove();
                            line.Element("colorLink")?.Remove();
                            line.Element("flowLink")?.Remove();
                        }
                        line.SetAttributeValue("mpwr_internals", null);
                    }
                    catch (Exception ex)
                    {
                        Fatal($"Uncaught exception processing line: {ex.Message}");
                    }
                }

                var naughtylines = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Line" && x.Attribute("flowStyle")?.Value == "Arrow");
                foreach (var line in naughtylines)
                {
                    if (string.IsNullOrWhiteSpace(line.Element("dataLink")?.Attribute("id")?.Value))
                    {
                        Err($"Line {line.Attribute("id")?.Value} in display { group.Key} has a flowStyle set, but no data link.");
                    }
                }

                var texts = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Text");
                foreach (var text in texts)
                {
                    try
                    {
                        var val = text.Attribute("text")?.Value;
                        if (val == null)
                        {
                            text.SetAttributeValue("text", "ERR");
                        }
                        if (!geographic)
                        {
                            text.SetAttributeValue("fontSize", "50");
                        }
                    }
                    catch (Exception ex)
                    {
                        Fatal($"Uncaught exception processing text: {ex.Message}");
                    }
                }
            }
        }

        public void ProcessGeographic()
        {
            foreach (var group in _displayGroups)
            {
                var geographic = group.Key == GisDisplayName;
                var display = group.Value;

                if (!geographic)
                    continue;

                var symbols = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Symbol" && x.Elements("dataLink").Any());
                foreach (var symbol in symbols)
                {
                    try
                    {
                        //we can assume that datalinks will be of the id type, not the dsID type
                        var datalink = symbol.Element("dataLink").Attribute("id")?.Value;
                        if (string.IsNullOrWhiteSpace(datalink))
                        {
                            //probably a premise
                            continue;
                        }
                        var device = Program.Enricher.Model.Devices.TryGetValue(datalink, out ModelDevice value) ? value : null;
                        if (device != null)
                        {
                            bool internals = symbol.Attribute("mpwr_internals")?.Value == "True";
                            List<Point> points = new List<Point>();
                            Point p = new Point
                            {
                                X = double.Parse(symbol.Attribute("x").Value),
                                Y = double.Parse(symbol.Attribute("y").Value)
                            };
                            p = TranslatePoint(p);
                            points.Add(p);
                            device.Geometry = points;
                            device.Internals = internals;
                        }
                    }
                    catch (Exception ex)
                    {
                        Fatal($"Uncaught exception processing symbol: {ex.Message}");
                    }
                }
                //TODO: change this to datalink?
                var lines = display.Descendants("element").Where(x => x.Attribute("type")?.Value == "Line" && x.Elements("colorLink").Any());
                foreach (var line in lines)
                {
                    try
                    {
                        //we can assume that datalinks will be of the id type, not the dsID type
                        //TODO: john needs to set the data link in schematics
                        var datalink = line.Element("colorLink").Attribute("id").Value;
                        var device = Program.Enricher.Model.Devices.TryGetValue(datalink, out ModelDevice value) ? value : null;
                        if (device != null)
                        {
                            bool internals = line.Attribute("mpwr_internals")?.Value == "True";
                            List<Point> points = new List<Point>();
                            foreach (var xy in line.Descendants("xy"))
                            {
                                Point p = new Point();
                                p.X = float.Parse(xy.Attribute("x").Value);
                                p.Y = float.Parse(xy.Attribute("y").Value);
                                p = TranslatePoint(p);
                                points.Add(p);
                            }
                            device.Internals = internals;
                            device.Geometry = points;
                        }
                    }
                    catch (Exception ex)
                    {
                        Fatal($"Uncaught exception processing line: {ex.Message}");
                    }
                }
            }
        }

        #region Graphic Manipulation Functions

        private void CopyLoadToPremise(XElement symboltocopy)
        {
            symboltocopy.SetAttributeValue("overlay", "Overlay_Load_mainpower");
            string id = symboltocopy.Descendants("dataLink").First().Attribute("id").Value;

            XElement symbol = new XElement(symboltocopy);
            symbol.Descendants().Remove();
            symbol.SetAttributeValue("id", symbol.Attribute("id").Value + ",Premise");

            symbol.SetAttributeValue("overlay", "Overlay_Premise_mainpower");

            XElement c = new XElement("command",
                   new XAttribute("plugin", "ElectraOMS"),
                   new XAttribute("topic", "view premise details"),
                   new XAttribute("instance", "Active"),
                   new XElement("field",
                       new XAttribute("name", "Data Link URL"),
                       new XAttribute("value", "@URL")),
                   new XElement("field",
                       new XAttribute("name", "Data Mode"),
                       new XAttribute("value", "@MODE")),
                   new XElement("field",
                       new XAttribute("name", "Data Instance"),
                       new XAttribute("value", "@I")
                   )
               );

            XElement x = new XElement("dataLink",
                new XAttribute("dsID", id),
                new XElement("link",
                    new XAttribute("d", "ELECTRA_OMS"),
                    new XAttribute("f", "IsAMIMeter"),
                    new XAttribute("i", "0"),
                    new XAttribute("identityType", "Key"),
                    new XAttribute("o", "OMS_ELECTRIC_PREMISE")
                )
            );
            symbol.Add(x);
            symbol.Add(c);
            symboltocopy.Parent.Add(symbol);
        }

        private void SetSymbolScadaLink(XElement symbol, string scadaKey)
        {
            //remove the old datalink, if any
            symbol.Element("dataLink")?.Remove();

            XElement x = new XElement("dataLink",
                new XAttribute("dsID", scadaKey),
                new XElement("link",
                    new XAttribute("d", "SCADA"),
                    new XAttribute("f", "State"),
                    new XAttribute("i", "0"),
                    new XAttribute("identityType", "Key"),
                    new XAttribute("o", "STATUS")
                )
            );
            symbol.Add(x);
        }
      
        private void SetSymbol(XElement symbol, ModelDevice device, bool gis)
        {

            symbol.SetAttributeValue("name", device.SymbolName);
            symbol.SetAttributeValue("library", "MPNZ.LIB2");
            bool internals = symbol.Attribute("mpwr_internals")?.Value == "True";

            double scale = double.NaN;
            double z = double.NaN;
            double rotation = double.NaN;
            double maxSize = 30;

            switch (device.Type)
            {
                case DeviceType.Switch:
                    if (gis)
                    {
                        if (internals)
                            scale = SymbolScaleSwitchInternals;
                        else if (device.Base1kV >= 1)
                            scale = SymbolScaleSwitchHV;
                        else scale = SymbolScaleSwitchLV;
                    }
                    else
                        scale = SymbolScaleSwitchSLD;
                    break;
                case DeviceType.Transformer:
                case DeviceType.EarthingTransformer:
                    if (gis)
                        scale = SymbolScaleTransformerGIS;
                    else if (internals)
                        scale = SymbolScaleTransformerSLD;
                    else
                        scale = SymbolScaleTransformerSLD;
                    rotation = 0;
                    z = 4;
                    break;
                case DeviceType.Regulator:
                    //these are only int he geographic
                    scale = SymbolScaleRegulator;
                    break;
                case DeviceType.Load:
                    scale =2;
                    break;
            }

            if (!scale.Equals(double.NaN))
                symbol.SetAttributeValue("scale", scale.ToString("F3"));
            if (!rotation.Equals(double.NaN))
                symbol.SetAttributeValue("rotation", rotation.ToString("F0"));
            if (!z.Equals(double.NaN))
                symbol.SetAttributeValue("z", z.ToString("F1"));
            if (!maxSize.Equals(double.NaN))
                symbol.SetAttributeValue("maxSize", maxSize.ToString("F3"));
        }

        /// <summary>
        /// Sets the line style
        /// </summary>
        /// <param name="line"></param>
        /// <param name="id"></param>
        /// <param name="voltage">The voltage of the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="service"></param>
        private void SetLineStyle(XElement line, string id, double voltage, string color, bool service)
        {
            //set the line color
            Color c = ColorTranslator.FromHtml(color);
            //remove and existing color
            line.Descendants("color").Remove();
            var col = new XElement("color", new XAttribute("red", c.R), new XAttribute("green", c.G), new XAttribute("blue", c.B));
            line.Add(col);

            //set the line width
            int width = 1;
            switch (voltage)
            {
                case 66:
                case 33:
                    width = 6;
                    break;
                case 22:
                case 11:
                case 6.6:
                    width = 3;
                    break;
                case 0.4:
                    if (service)
                        width = 1;
                    else
                        width = 2;
                    break;
            }
            line.SetAttributeValue("width", width);

            line.SetAttributeValue("flowDirection", "Forward");
            line.SetAttributeValue("flowStyle", "Arrow");
            line.SetAttributeValue("flowSubStyle", "Solid");
            //add the flowlink
            if (!line.Descendants("flowLink").Any())
            {
                XElement x = new XElement("flowLink",
                    new XAttribute("id", id),
                    new XElement("link",
                        new XAttribute("d", "EMAP"),
                        new XAttribute("o", "EMAP_LINE"),
                        new XAttribute("f", "AggregateFlow"),
                        new XAttribute("i", "0"),
                        new XAttribute("identityType", "Record")
                    )
                );
                line.Add(x);
            }

            //add the datalink
            //TODO john should be doing this
            if (!line.Descendants("dataLink").Any())
            {
                XElement x = new XElement("dataLink",
                    new XAttribute("id", id),
                    new XElement("link",
                        new XAttribute("d", "EMAP"),
                        new XAttribute("o", "EMAP_DEVICE"),
                        new XAttribute("f", "AggregateState"),
                        new XAttribute("i", "0"),
                        new XAttribute("identityType", "Key")
                    )
                );
                line.Add(x);
            }
        }

        #endregion

        #region Functions that need to be moved elsewhere

        /// <summary>
        /// For loads... hte id must be the icp for OMS device linking
        /// </summary>
        /// <param name="oldId"></param>
        /// <param name="newId"></param>
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

        public static Point TranslatePoint(Point p)
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
        #endregion
    }
}

