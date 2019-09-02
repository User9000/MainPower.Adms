﻿using EGIS.ShapeFileLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    [Serializable]
    [MessagePackObject]
    public class NodeModel : ErrorReporter
    {
        /// <summary>
        /// The Dictionary Devices in the model, keyed by id
        /// </summary>
        [Key(0)]
        public Dictionary<string, Device> Devices { get; set; } = new Dictionary<string, Device>();

        /// <summary>
        /// The Dictionary of Nodes in the model, keyed by id
        /// </summary>
        [Key(1)]
        public Dictionary<string, Node> Nodes { get; set; } = new Dictionary<string, Node>();

        /// <summary>
        /// The Dictionary of Sources in the model, keyed by id
        /// </summary>
        [Key(2)]
        public Dictionary<string, Source> Sources { get; set; } = new Dictionary<string, Source>();

        /// <summary>
        /// Add a device to the model from an IDF XML element
        /// </summary>
        /// <param name="node">The xml element</param>
        /// <param name="gid">The group id</param>
        /// <param name="type">The type of device we are adding</param>
        /// <param name="phaseshift">The phase shift that happens from side1 to side2 of the device (applicable to transformers only)</param>
        /// <returns>true if adding the device was successful, false otherwise</returns>
        public bool AddDevice(XElement node, string gid, DeviceType type, List<PointD> geo, int phaseshift = 0)
        {
            var s1nodeid = node.Attribute("s1node").Value;
            var s2nodeid = node.Attribute("s2node").Value;

            Device d = new Device
            {
                Id = node.Attribute("id").Value,
                Name = node.Attribute("name").Value,
                GroupId = gid,
                Type = type,
                Geometry = geo
            };

            string t = node.Attribute("s1phaseID1").Value;
            d.PhaseID[0, 0] = t == "" ? (short)0 : short.Parse(t);
            t = node.Attribute("s1phaseID2")?.Value ?? "";
            d.PhaseID[0, 1] = t == "" ? (short)0 : short.Parse(t);
            t = node.Attribute("s1phaseID3")?.Value ?? "";
            d.PhaseID[0, 2] = t == "" ? (short)0 : short.Parse(t);
            t = node.Attribute("s2phaseID1")?.Value ?? "";
            d.PhaseID[1, 0] = t == "" ? (short)0 : short.Parse(t);
            t = node.Attribute("s2phaseID2")?.Value ?? "";
            d.PhaseID[1, 1] = t == "" ? (short)0 : short.Parse(t);
            t = node.Attribute("s2phaseID3")?.Value ?? "";
            d.PhaseID[1, 2] = t == "" ? (short)0 : short.Parse(t);

            if (type == DeviceType.Transformer)
            {
                d.Base1kV = double.Parse(node.Attribute("s1baseKV").Value);
                d.Base2kV = double.Parse(node.Attribute("s2baseKV").Value);
                d.PhaseShift = phaseshift;
            }
            else
            {
                d.PhaseShift = 0;
                d.Base1kV = d.Base2kV = double.Parse(node.Attribute("baseKV").Value);

                if (type == DeviceType.Switch)
                    d.SwitchState = bool.Parse(node.Attribute("nominalState1").Value);
                else if (type == DeviceType.Line)
                {
                    d.Length = double.Parse(node.Attribute("length").Value);
                    if (d.Length == 0)
                        d.Length = 0.01;//helps improve directionality for parallel bus sections
                }
            }
            return AddDevice(d, s1nodeid, s2nodeid);
        }

        /// <summary>
        /// Adds a device to the model
        /// </summary>
        /// <param name="d">The device object</param>
        /// <param name="n1id">Side 1 Node Id</param>
        /// <param name="n2id">Side 2 Node Id</param>
        /// <returns>true if successful, false otherwise</returns>
        private bool AddDevice(Device d, string n1id, string n2id)
        {
            if (Devices.ContainsKey(d.Id))
            {
                Error($"Device Id already exists.", d.Id, d.Name);
                return false;
            }
            //in case someone does another operation at the same time
            lock (Devices) lock (Nodes)
                {
                    Node n1, n2;
                    if (Nodes.ContainsKey(n1id))
                        n1 = Nodes[n1id];
                    else
                    {
                        n1 = new Node() { Id = n1id };
                        Nodes.Add(n1id, n1);
                    }
                    if (Nodes.ContainsKey(n2id))
                        n2 = Nodes[n2id];
                    else
                    {
                        n2 = new Node() { Id = n2id };
                        Nodes.Add(n2id, n2);
                    }
                    d.Node1 = n1;
                    d.Node2 = n2;

                    Devices.Add(d.Id, d);
                    n1.Devices.Add(d);
                    n2.Devices.Add(d);
                }
            return true;
        }

        /// <summary>
        /// Adds a source to the model from an IDF XML element
        /// </summary>
        /// <param name="node">The xml element</param>
        /// <param name="gid">The group id</param>
        /// <returns></returns>
        public bool AddSource(XElement node, string gid)
        {
            Source s = new Source
            {
                Id = node.Attribute("id").Value,
                GroupId = gid,
                Name = node.Attribute("name").Value,
                DeviceId = node.Attribute("device").Value
            };
            s.PhaseAngles[0] = (short)(short.Parse(node.Attribute("phase1Angle").Value) / 30);
            s.PhaseAngles[1] = (short)(short.Parse(node.Attribute("phase2Angle").Value) / 30);
            s.PhaseAngles[2] = (short)(short.Parse(node.Attribute("phase3Angle").Value) / 30);

            return AddSource(s);
        }

        /// <summary>
        /// Adds a Source to the model
        /// </summary>
        /// <param name="s">The Source</param>
        /// <returns>true if successful, false otherwise</returns>
        private bool AddSource(Source s)
        {
            if (Sources.ContainsKey(s.Id))
            {
                Error($"Source with id [{s.Id}] already exists");
                return false;
            }
            lock (Sources)
            {
                Sources.Add(s.Id, s);
            }
            return true;
        }

        /// <summary>
        /// Removes all devices in a group from the model
        /// </summary>
        /// <param name="id">The group id</param>        
        public void RemoveGroup(string id)
        {
            lock (Devices) lock (Nodes) lock (Sources)
                    {
                        var devices = from d in Devices.Values where d.GroupId == id select d;
                        foreach (Device d in devices.ToList())
                        {
                            RemoveDevice(d);
                        }
                        var sources = from s in Sources.Values where s.GroupId == id select s;
                        foreach (Source s in sources.ToList())
                        {
                            Sources.Remove(s.Id);
                        }
                        CleanOrphanNodes();
                    }
        }

        /// <summary>
        /// Removes a device from the model
        /// </summary>
        /// <param name="d"></param>
        private void RemoveDevice(Device d)
        {
            lock (Devices)
            {
                d.Node1.Devices.Remove(d);
                d.Node2.Devices.Remove(d);
                Devices.Remove(d.Id);
            }
        }

        /// <summary>
        /// Searches the model for nodes with no devices, then removes them
        /// </summary>
        private void CleanOrphanNodes()
        {
            lock (Nodes)
            {
                var nodes = from n in Nodes.Values where n.Devices.Count == 0 select n;
                foreach (var node in nodes.ToList())
                {
                    Nodes.Remove(node.Id);
                }
            }
        }

        /// <summary>
        /// Saves the model to a file
        /// </summary>
        /// <param name="file">The filename</param>
        public void Serialize(string file)
        {
            Info("Saving the connectivity model...");
            try
            {
                //TODO: skip this until we have sorted out the PointD thing
                //Util.SerializeMessagePack(file, this);
            }
            catch (Exception ex)
            {
                Fatal($"Failed to serialize model to file [{file}]. {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the model from a file
        /// </summary>
        /// <param name="file">The filename</param>
        /// <returns>A new NodeModel, or null if the operation fails</returns>
        public static NodeModel Deserialize(string file)
        {
            StaticInfo("Loading model...", typeof(NodeModel));
            try
            {
                var model = Util.DeserializeMessagePack<NodeModel>(file);
                if (model != null)
                    model.RebuildNodeReferences();
                return model;
            }
            catch (Exception ex)
            {
                StaticFatal($"Failed to deserialize model from file [{file}]. {ex.Message}", typeof(NodeModel));
                return null;
            }
        }

        /// <summary>
        /// Rebuilds the Device-Node relationships following a model load from file. Required as circular references can't be handled by most deserializers
        /// </summary>
        private void RebuildNodeReferences()
        {
            foreach (var device in Devices.Values)
            {
                device.Node1.Devices.Add(device);
                device.Node2.Devices.Add(device);
            }
        }

        /// <summary>
        /// Trace connectivity downstream from the first connected source
        /// </summary>
        public void ValidateConnectivity()
        {
            Info("Performing connectivity check...");
            DateTime start = DateTime.Now;
            if (Sources.Count == 0)
                return;
            int i = 0;
            var sources = Sources.Values.ToList();
            while (!Devices.ContainsKey(sources[i].DeviceId) && i < Sources.Count)
            {
                i++;
            }
            if (i < sources.Count)
                TraceNodeConnectivity(Devices[sources[i].DeviceId], Devices[sources[i].DeviceId].Node1);

            TimeSpan runtime = DateTime.Now - start;
            Info($"Connectivity check: {GetDisconnectedCount()} of {Devices.Count} devices disconnected ({runtime.TotalSeconds} seconds)");
        }

        /// <summary>
        /// Marks the node as being connected, and calls itself recursively to adjacent nodes
        /// </summary>
        /// <param name="d">The device we are tracing into</param>
        /// <param name="n">The node we are tracing in from</param>
        private void TraceNodeConnectivity(Device d, Node n)
        {
            //TODO: turn this into a non recursive function
            Node traceNode;
            if (d.ConnectivityMark)
                return;
            else
                d.ConnectivityMark = true;

            if (d.Node1 == n)
            {
                traceNode = d.Node2;
            }
            else
            {
                traceNode = d.Node1;
            }
            foreach (Device dd in traceNode.Devices)
            {
                if (dd != d)
                {
                    TraceNodeConnectivity(dd, traceNode);
                }
            }
        }

        /// <summary>
        /// Checks that all devices connected to each node have the same base voltage
        /// </summary>
        public void ValidateBaseVoltages()
        {
            Info("Checking voltage consistency...");
            DateTime start = DateTime.Now;

            long count = 0;
            foreach (var node in Nodes.Values)
            {
                var basekv = double.NaN;
                foreach (var device in node.Devices)
                {
                    double nodevoltage;
                    if (device.Node1 == node)
                        nodevoltage = device.Base1kV;
                    else
                        nodevoltage = device.Base2kV;
                    if (basekv.Equals(double.NaN))
                        basekv = nodevoltage;
                    else
                    {
                        if (basekv != nodevoltage)
                        {
                            count++;
                            node.IsDirty = true;
                            break;
                        }
                    }
                }
            }
            var dirtynodes = from n in Nodes.Values where n.IsDirty select n;
            foreach (var node in dirtynodes)
            {
                foreach (var device in node.Devices)
                {
                    Error($"Device is connected to a dirty node [{node.Id}]", device.Id, device.Name);
                }
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"There were {count} nodes with inconsistent base voltages ({runtime.Seconds}s)");
        }


        /// <summary>
        /// Traces outwards from each source
        /// Calculates the likely upstream side of each device
        /// Checks the phasing of each device to ensure consistency with upstrea devices
        /// </summary>
        public void ValidatePhasing()
        {
            Info("Performing shortest distance to source calculations...");
            DateTime start = DateTime.Now;

            if (Sources.Count == 0)
                return;
            foreach (var source in Sources.Values)
            {
                if (Devices.ContainsKey(source.DeviceId))
                    TraceNodeEnergization(Devices[source.DeviceId], source);
            }
            foreach (Device dd in Devices.Values)
            {
                dd.CalculateUpstreamSide();
            }
            ValidateDevicePhasing();
            TimeSpan runtime = DateTime.Now - start;
            Info($"Power flow check: {GetDeenergizedCount()} of {Devices.Count} devices deenergized ({runtime.TotalSeconds}s)");
        }

        /// <summary>
        /// Trace though the network starting at this device, calculating the shortest path to the source for each device
        /// </summary>
        /// <param name="d">The device we are tracing into</param>
        /// <param name="s">The source that is energizing this trace</param>
        /// <param name="distance">The distance thus far from the source</param>
        private void TraceNodeEnergization(Device d, Source s)
        {
            long loop = 0;
            //The stack keeps track of all the branches
            //The tuple items are:
            //d - the device we are tracing into
            //n - the node we are tracing in from
            //ud - the device the trace came from
            //distance - the distance thus far from the source
            //TODO: make this a queue
            Stack<(Device d, Node n, Device ud, double distance)> stack = new Stack<(Device, Node, Device, double)>();
            stack.Push((d, d.Node1, null, 0));
            do
            {
                loop++;
                var set = stack.Pop();
                Node traceNode;
                //if we haven't visited this node from source s, add new PFDetails for this source
                if (!set.d.SP2S.ContainsKey(s))
                    set.d.SP2S.Add(s, new PFDetail());
                //mark that we have been here from any source
                //TODO: this is basically !s.SPS2S.Empty()
                set.d.SP2SMark = true;

                var openSwitch = !set.d.SwitchState && set.d.Type == DeviceType.Switch;

                //we are coming in from side 1
                if (set.d.Node1 == set.n)
                {
                    //if we have already been here from a shorter path, then quit
                    if (set.distance >= set.d.SP2S[s].N1IntDistance)
                        continue;

                    //mark that we have been here from side 1 and update the distance
                    set.d.SP2S[s].N1ExtDistance = set.d.SP2S[s].N1IntDistance = set.distance;
                    if (!openSwitch)
                        set.d.SP2S[s].N2IntDistance = set.distance + d.Length;
                    traceNode = set.d.Node2;
                }
                else //we are coming in from side 2
                {
                    if (set.distance >= set.d.SP2S[s].N2IntDistance)
                        continue;
                    set.d.SP2S[s].N2ExtDistance = set.d.SP2S[s].N2IntDistance = set.distance;
                    if (!openSwitch)
                        set.d.SP2S[s].N1IntDistance = set.distance + d.Length;
                    traceNode = set.d.Node1;
                }
                if (set.d.SwitchState || set.d.Type != DeviceType.Switch)
                {
                    foreach (Device dd in traceNode.Devices)
                    {
                        if (dd != set.d)
                        {
                            stack.Push((dd, traceNode, set.d, set.distance + set.d.Length));
                        }
                    }
                }
            }
            while (stack.Count > 0);
            Debug($"Power flow took {loop} loops for source {s.Name}");
        }

        /// <summary>
        /// Verifies that for each device the same phases are present on both sides, and that the phases present on the device are also present on the upstream device
        /// </summary>
        private void ValidateDevicePhasing()
        {
            foreach (Device d in Devices.Values)
            {
                //don't check disconnected, deenergized devices or head devices
                if (!d.ConnectivityMark || d.Upstream == 0 || !d.SP2SMark)
                    continue;

                int iUp = d.Upstream - 1;
                int iDown = (iUp + 1) % 2;

                //check that we have the same phases on both sides
                //TODO implement for transformers
                //lets leave transformers alone at the moment because the rules are different
                if (d.Type != DeviceType.Transformer)
                {
                    //t is a temporary array used to track matched phases
                    int[] t = new int[3];
                    //-1 is used as the default valie
                    t[0] = t[1] = t[2] = -1;
                    //loop through phases on the upstream side
                    for (int i = 0; i < 3; i++)
                    {
                        //and match them up to phases on the downstream side
                        for (int j = 0; j < 3; j++)
                        {
                            if (d.PhaseID[iUp, i] == d.PhaseID[iDown, j] && t[j] == -1)
                                t[j] = i; //when we find a match on the lv side, mark that side as used in the t array
                        }
                    }
                    //if there are any unmatched phases at the end of it, then raise a mismatch warning
                    if (t[0] == -1 || t[1] == -1 || t[2] == -1)
                        Warn($"Phases on both sides of device are different", d.Id, d.Name);
                }

                //check that the phase IDs on this device match the phase IDs on all upstream devices
                foreach (Device us in d.GetUpstreamDevices())
                {
                    //get the 0 based side index of the upstream devices connecting node with this device
                    int usUp = (d.UpstreamNode == us.Node1) ? 0 : 1;

                    for (int i = 0; i < 3; i++)
                    {
                        //if the phase is not set (0) then it doens't need to be connected upstream
                        if (d.PhaseID[iUp, i] != 0 && d.PhaseID[iUp, i] != us.PhaseID[usUp, i])
                            Warn($"Phase {i + 1} on side {d.Upstream} doesn't agree with upstream device {us.Name}", d.Id, d.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the number of disconnected devices
        /// </summary>
        /// <returns>Number of disconnected devices</returns>
        public int GetDisconnectedCount()
        {
            var devices = from d in Devices.Values where !d.ConnectivityMark select d;
            return devices.Count();
        }

        /// <summary>
        /// Calculate the number of deenergized devices
        /// </summary>
        /// <returns>Number of deenergized devices</returns>
        public int GetDeenergizedCount()
        {
            var devices = from d in Devices.Values where !d.SP2SMark select d;
            return devices.Count();
        }

        /// <summary>
        /// Returns the upstream side of a device, by the device name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>0 if the device is not found, 1 for Side1, 2 for Side2</returns>
        public int GetUpstreamSideByName(string name)
        {
            var d = (from i in Devices.Values where i.Name == name select i).FirstOrDefault();
            if (d == null)
                return 0;
            else
                return d.Upstream;
        }

        /// <summary>
        /// Print detailed results of the shortest path to source calculations for a device
        /// </summary>
        /// <param name="name">The name of the device</param>
        public void PrintPFDetailsByName(string name)
        {
            var d = (from i in Devices.Values where i.Name == name select i).FirstOrDefault();
            if (d == null)
            {
                Console.WriteLine($"Device [{name}] doesn't exist.");
                return;
            }
            else
            {
                d.PrintPFResults();
            }
        }

        /// <summary>
        /// Exports the model to two shape files (Devices.shp and Lines.shp)
        /// </summary>
        /// <param name="dir">The directory to export to</param>
        public void ExportToShapeFile(string dir)
        {
            DbfFieldDesc[] deviceFields = new DbfFieldDesc[14];
            deviceFields[0] = new DbfFieldDesc
            {
                FieldName = "Node1Id",
                FieldLength = 40,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[1] = new DbfFieldDesc
            {
                FieldName = "Node2Id",
                FieldLength = 40,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[2] = new DbfFieldDesc
            {
                FieldName = "Id",
                FieldLength = 40,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[3] = new DbfFieldDesc
            {
                FieldName = "Name",
                FieldLength = 40,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[4] = new DbfFieldDesc
            {
                FieldName = "GroupId",
                FieldLength = 40,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[5] = new DbfFieldDesc
            {
                FieldName = "Connected",
                FieldLength = 1,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[6] = new DbfFieldDesc
            {
                FieldName = "Energized",
                FieldLength = 1,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[7] = new DbfFieldDesc
            {
                FieldName = "Upstream",
                FieldLength = 1,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[8] = new DbfFieldDesc
            {
                FieldName = "Type",
                FieldLength = 15,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[9] = new DbfFieldDesc
            {
                FieldName = "Base1kV",
                FieldLength = 8,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[10] = new DbfFieldDesc
            {
                FieldName = "Base2kV",
                FieldLength = 8,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[11] = new DbfFieldDesc
            {
                FieldName = "Side1Phase",
                FieldLength = 3,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[12] = new DbfFieldDesc
            {
                FieldName = "Side2Phase",
                FieldLength = 3,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[13] = new DbfFieldDesc
            {
                FieldName = "PhaseShift",
                FieldLength = 2,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };

            ShapeFileWriter sfwDevices = ShapeFileWriter.CreateWriter(dir, "Devices", ShapeType.Point, deviceFields);
            ExportWebMercatorProjectionFile(Path.Combine(dir, "Devices.prj"));
            ShapeFileWriter sfwLines = ShapeFileWriter.CreateWriter(dir, "Lines", ShapeType.PolyLine, deviceFields);
            ExportWebMercatorProjectionFile(Path.Combine(dir, "Lines.prj"));
            try
            {
                foreach (Device d in Devices.Values)
                {
                    string[] fieldData = new string[14];
                    fieldData[0] = d.Node1.Id;
                    fieldData[1] = d.Node2.Id;
                    fieldData[2] = d.Id;
                    fieldData[3] = d.Name;
                    fieldData[4] = d.GroupId;
                    fieldData[5] = d.ConnectivityMark ? "1" : "0";
                    fieldData[6] = d.SP2SMark ? "1" : "0";
                    fieldData[7] = d.Upstream.ToString();
                    fieldData[8] = d.Type.ToString();
                    fieldData[9] = d.Base1kV.ToString("N3");
                    fieldData[10] = d.Base2kV.ToString("N3");
                    fieldData[11] = $"{d.PhaseID[0, 0]}{d.PhaseID[0, 1]}{d.PhaseID[0, 2]}";
                    fieldData[12] = $"{d.PhaseID[1, 0]}{d.PhaseID[1, 1]}{d.PhaseID[1, 2]}";
                    fieldData[13] = d.PhaseShift.ToString("N2");

                    if (d.Geometry.Count == 0)
                    {
                        Warn("Not exporting device to shape file due to missing geometry", d.Id, d.Name);
                        continue;
                    }

                    if (d.Type == DeviceType.Line)
                    {
                        sfwLines.AddRecord(d.Geometry.ToArray(), d.Geometry.Count, fieldData);
                    }
                    else
                    {
                        sfwDevices.AddRecord(new PointD[] { d.Geometry[0] }, 1, fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
            finally
            {
                sfwDevices.Close();
                sfwLines.Close();
            }
        }

        /// <summary>
        /// Writes a .prj file to disk with the projection string for Web Mercator
        /// </summary>
        /// <param name="file">The file to write to</param>
        private void ExportWebMercatorProjectionFile(string file)
        {
            File.WriteAllText(file, "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]");
        }



    }
}