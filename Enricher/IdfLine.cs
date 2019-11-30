using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfLine : IdfElement
    {
        private const string GIS_PHASE_CONDUCTOR = "mpwr_cable_type";
        private const string GIS_NEUTRAL_CONDUCTOR = "mpwr_neutral_cable_type";
        private const string IDF_LINE_TYPE = "lineType";
        private const string LINE_BUSBAR = "lineType_busbar";

        private static readonly Dictionary<(string voltage, int phases, string type, string phase, string neutral, bool service), (int count,double length)> conductors = new Dictionary<(string voltage, int phases, string type, string phase, string neutral, bool service), (int, double)>();

        /// <summary>
        /// Represents a IDF Line object
        /// </summary>
        /// <param name="node">The XElement node from the IDF</param>
        /// <param name="parent">The Group that this element belongs to</param>
        public IdfLine(XElement node, IdfGroup parent) : base(node, parent) { }

        /// <summary>
        /// Process the Line object
        /// </summary>
        public override void Process()
        {
            try
            {
                if (!Enricher.I.Model.AddDevice(this, ParentGroup.Id, DeviceType.Line))
                {
                    Err("Failed to add line to model -> deleted 😬");
                    Node.Remove();
                }
                
                bool isBusbar = Node.Attribute("lineType")?.Value.Contains("BUSBAR") ?? false;
                string lineType = LINE_BUSBAR;
                string phase_conductor = Node.Attribute(GIS_PHASE_CONDUCTOR)?.Value ?? "";
                string neutral_conductor = Node.Attribute(GIS_NEUTRAL_CONDUCTOR)?.Value ?? "";
                

                string voltage = Node.Attribute(IdfDeviceBasekV).Value;
                switch (voltage) 
                {
                    case "66":
                        if (!isBusbar) lineType = "lineType_66kV_default";
                        break;
                    case "33":
                        if (!isBusbar) lineType = "lineType_33kV_default";
                        break;
                    case "22":
                        if (!isBusbar) lineType = "lineType_22kV_default";
                        break;
                    case "11":
                        if (!isBusbar) lineType = "lineType_11kV_default";
                        break;
                    case "6.6":
                        if (!isBusbar) lineType = "lineType_6.6kV_default";
                        break;
                    case "0.4":
                    case "0.4000":                      
                        if (!isBusbar) lineType = "lineType_400V_default";
                        break;
                    default:
                        break;
                }

                Node.SetAttributeValue("ratedKV", Node.Attribute("baseKV").Value);

                if (!double.TryParse(Node.Attribute("length")?.Value ?? "", out double length))
                {
                    length = 0;
                }

                if (!isBusbar && length <= 25)
                    Enricher.I.Line25Count++;
                if (!isBusbar && length <= 5)
                    Enricher.I.Line5Count++;

                var conductor = (voltage, S1Phases, lineType, Node.Attribute(GIS_PHASE_CONDUCTOR)?.Value, Node.Attribute(GIS_NEUTRAL_CONDUCTOR)?.Value, Name.StartsWith("Service"));
                //HashSet not thread safe
                lock (conductors)
                {
                    if (conductors.ContainsKey(conductor))
                    {
                        var val = conductors[conductor];
                        val.count++;
                        val.length+= length;
                        conductors[conductor] = val;
                    }
                    else
                    {
                        conductors.Add(conductor, (1, length));
                    }
                }

                var kvps = new List<KeyValuePair<string, string>>();
                kvps.Add(new KeyValuePair<string, string>("Phase Cond.", phase_conductor));
                kvps.Add(new KeyValuePair<string, string>("Neutral Cond.", neutral_conductor ));

                GenerateDeviceInfo(kvps);

                Node.SetAttributeValue(IDF_LINE_TYPE, lineType);

                Node.SetAttributeValue(GIS_NEUTRAL_CONDUCTOR, null);
                Node.SetAttributeValue(GIS_PHASE_CONDUCTOR, null);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }

        public static void ExportConductors()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Voltage", typeof(string));
            dt.Columns.Add("Phases", typeof(int));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Phase Conductor", typeof(string));
            dt.Columns.Add("Neutral Conductor", typeof(string));
            dt.Columns.Add("Service", typeof(bool));
            dt.Columns.Add("Count", typeof(int));
            dt.Columns.Add("Length", typeof(int));

            foreach (var item in conductors)
            {
                DataRow r = dt.NewRow();
                r[0] = item.Key.voltage;
                r[1] = item.Key.phases;
                r[2] = item.Key.type;
                r[3] = item.Key.phase;
                r[4] = item.Key.neutral;
                r[5] = item.Key.service;
                r[6] = item.Value.count;
                r[7] = item.Value.length;
                dt.Rows.Add(r);
            }

            dt.DefaultView.Sort = "Count desc";
            Util.ExportDatatable(dt.DefaultView.ToTable(), Path.Combine(Enricher.I.Options.OutputPath, "Conductors.csv"));
        }
    }
}
