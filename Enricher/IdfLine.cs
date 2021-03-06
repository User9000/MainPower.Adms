﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class IdfLine : IdfElement
    {
        private const string GisPhaseConductor = "mpwr_cable_type";
        private const string GisNeutralConductor = "mpwr_neutral_cable_type";
        private const string IdfLineType = "lineType";
        private const string LineBusbar = "lType_busbar";

        private static readonly Dictionary<(string voltage, int phases, string type, string phase, string neutral, bool service, bool inDataset), (int count,double length)> _conductors = new Dictionary<(string voltage, int phases, string type, string phase, string neutral, bool service, bool inDataset), (int, double)>();
        private static DataTable _conductorTypes = null;

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

                if (_conductorTypes == null)
                {
                    lock (_conductors)
                    {
                        LoadConductorTypes();
                    }
                }

                CheckPhases();

                Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Line);               
                bool isBusbar = Node.Attribute(IdfLineType)?.Value.Contains("BUSBAR") ?? false;
                string originalLineType = Node.Attribute(IdfLineType)?.Value;
                string lineType = LineBusbar;
                string phase_conductor = Node.Attribute(GisPhaseConductor)?.Value;
                string neutral_conductor = Node.Attribute(GisNeutralConductor)?.Value;
                string voltage = Node.Attribute(IdfDeviceBasekV).Value;
                bool notInConductorDataset = false;
                
                if (!isBusbar)
                {
                    lineType = GetConductorId(double.Parse(voltage), S1Phases, phase_conductor, neutral_conductor);
                    if (string.IsNullOrEmpty(lineType))
                    {
                        notInConductorDataset = true;
                        switch (voltage)
                        {
                            case "66":
                                if (!isBusbar) lineType = "lType_66kV_default";
                                break;
                            case "33":
                                if (!isBusbar) lineType = "lType_33kV_default";
                                break;
                            case "22":
                                if (!isBusbar) lineType = "lType_22kV_default";
                                break;
                            case "11":
                                if (!isBusbar) lineType = "lType_11kV_default";
                                break;
                            case "6.6":
                            case "6.600":
                                if (!isBusbar) lineType = "lType_6.6kV_default";
                                break;
                            case "0.4":
                            case "0.4000":
                                if (!isBusbar) lineType = "lType_400V_default";
                                break;
                            default:
                                Err("line type not assigned due to unrecognised voltage!");
                                break;
                        }
                    }
                    else
                    {
                        string phid = "";
                        if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId1)?.Value)) phid += "A";
                        if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId2)?.Value)) phid += "B";
                        if (!string.IsNullOrWhiteSpace(Node.Attribute(IdfDeviceS1PhaseId3)?.Value)) phid += "C";

                        lineType = lineType.Replace("lType_", $"lType_{phid}_");
                    }
                }

                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV).Value);

                if (!double.TryParse(Node.Attribute("length")?.Value ?? "", out double length))
                {
                    length = 0;
                }

                var conductor = (voltage, S1Phases, originalLineType, phase_conductor ?? "", neutral_conductor ?? "", Name.StartsWith("Service"), notInConductorDataset);

                //Dictionary not thread safe
                lock (_conductors)
                {
                    if (_conductors.ContainsKey(conductor))
                    {
                        var val = _conductors[conductor];
                        val.count++;
                        val.length+= length;
                        _conductors[conductor] = val;
                    }
                    else
                    {
                        _conductors.Add(conductor, (1, length));
                    }
                }

                var kvps = new List<KeyValuePair<string, string>>();
                kvps.Add(new KeyValuePair<string, string>("Phase Cond.", phase_conductor));
                kvps.Add(new KeyValuePair<string, string>("Neutral Cond.", neutral_conductor ));

                GenerateDeviceInfo(kvps);

                Node.SetAttributeValue(IdfLineType, lineType);

                Node.SetAttributeValue(GisNeutralConductor, null);
                Node.SetAttributeValue(GisPhaseConductor, null);
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
            dt.Columns.Add("Missing", typeof(bool));
            dt.Columns.Add("Count", typeof(int));
            dt.Columns.Add("Length", typeof(int));

            foreach (var item in _conductors)
            {
                DataRow r = dt.NewRow();
                r[0] = item.Key.voltage;
                r[1] = item.Key.phases;
                r[2] = item.Key.type;
                r[3] = item.Key.phase;
                r[4] = item.Key.neutral;
                r[5] = item.Key.service;
                r[6] = item.Key.inDataset;
                r[7] = item.Value.count;
                r[8] = item.Value.length;
                dt.Rows.Add(r);
            }

            dt.DefaultView.Sort = "Count desc";
            Util.ExportDatatable(dt.DefaultView.ToTable(), Path.Combine(Program.Options.OutputPath, "ConductorSummary.csv"));
        }

        private void LoadConductorTypes()
        {
            _conductorTypes = Util.GetDataTableFromCsv(Path.Combine(Program.Options.DataPath, "Conductors.csv"), true);
        }

        private string GetConductorId(double voltage, int phases, string pconductor, string nconductor)
        {
            if (string.IsNullOrEmpty(pconductor))
                pconductor = $"[Phase Conductor] = ''";
            else
                pconductor = $"[Phase Conductor] = '{pconductor}'";

            if (string.IsNullOrEmpty(nconductor))
                nconductor = $"[Neutral Conductor] = ''";
            else
                nconductor = $"[Neutral Conductor] = '{nconductor}'";

            var result = _conductorTypes.Select($"[ADMS] = 'TRUE' AND [Voltage] = '{voltage}' AND [Phases] = '{phases}' AND {pconductor} AND {nconductor}");
            if (result.Length == 0)
            {
                return null;
            }
            else
            {
                return result[0]["ID"] as string;
            }
        }
    }
}
