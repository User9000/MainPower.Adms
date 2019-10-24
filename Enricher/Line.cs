using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Line : Element
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
        public Line(XElement node, Group parent) : base(node, parent) { }

        /// <summary>
        /// Process the Line object
        /// </summary>
        public override void Process()
        {
            try
            {
                #region TODO: Backport to GIS Extractor
                //CheckPhases();
                SetAllNominalStates();
                ParentGroup.AddDataAndFlowlink(Id);
                /*
                //assume that unset lv busses are for streetlights
                if (Name.StartsWith("LV Bus") && S1Phases == 0)
                {
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "");
                    S1Phases = S2Phases = 1;
                }              

                //overwrite streetlight phasing
                if (Name.StartsWith("SL"))
                {
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID1, "");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S1_PHASEID3, "");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID1, "");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID2, "2");
                    Node.SetAttributeValue(IDF_DEVICE_S2_PHASEID3, "");
                    S1Phases = S2Phases = 1;
                }*/

                CheckPhases();


                #endregion

                var geo = ParentGroup.GetLineGeometry(Id);
                if (!Enricher.I.Model.AddDevice(Node, ParentGroup.Id, DeviceType.Line, geo))
                {
                    Error("Failed to add line to model -> deleted 😬");
                    Node.Remove();
                }
                
                bool isBusbar = Node.Attribute("lineType")?.Value.Contains("BUSBAR") ?? false;
                string lineType = LINE_BUSBAR;

                //TODO: this should be determined by conductor type
                string voltage = Node.Attribute(IDF_DEVICE_BASEKV).Value;
                switch (voltage) 
                {
                    case "66":
                        Node.SetAttributeValue("ratedKV", "70");
                        ParentGroup.AddColorToLine(Id, Color.DarkBlue);
                        if (!isBusbar) lineType = "lineType_66kV_default";
                        break;
                    case "33":
                        Node.SetAttributeValue("ratedKV", "40");
                        ParentGroup.AddColorToLine(Id, Color.DarkGreen);
                        if (!isBusbar) lineType = "lineType_33kV_default";
                        break;
                    case "22":
                        Node.SetAttributeValue("ratedKV", "30");
                        ParentGroup.AddColorToLine(Id, Color.Orange);
                        if (!isBusbar) lineType = "lineType_22kV_default";
                        break;
                    case "11":
                        Node.SetAttributeValue("ratedKV", "15");
                        ParentGroup.AddColorToLine(Id, Color.Red);
                        if (!isBusbar) lineType = "lineType_11kV_default";
                        break;
                    case "6.6":
                        Node.SetAttributeValue("ratedKV", "11");
                        ParentGroup.AddColorToLine(Id, Color.Turquoise);
                        if (!isBusbar) lineType = "lineType_6.6kV_default";
                        break;
                    case "0.4":
                    case "0.4000":
                        Node.SetAttributeValue("ratedKV", "1");
                        ParentGroup.AddColorToLine(Id, Color.Yellow);
                        if (!isBusbar) lineType = "lineType_400V_default";
                        break;
                    default:
                        Node.SetAttributeValue("ratedKV", "100");
                        ParentGroup.AddColorToLine(Id, Color.Purple);
                        break;
                }

                if (!double.TryParse(Node.Attribute("length")?.Value ?? "", out double length))
                {
                    length = 0;
                }


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
                Node.SetAttributeValue(IDF_LINE_TYPE, lineType);

                Node.SetAttributeValue(GIS_NEUTRAL_CONDUCTOR, null);
                Node.SetAttributeValue(GIS_PHASE_CONDUCTOR, null);
#if !nofixes
                ParentGroup.SetLayerFromVoltage(Id, Node.Attribute(IDF_DEVICE_BASEKV).Value, false);
#endif
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
