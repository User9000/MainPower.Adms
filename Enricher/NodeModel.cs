using EGIS.ShapeFileLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [Key(3)]
        public Dictionary<string, ModelFeeder> Feeders { get; set; } = new Dictionary<string, ModelFeeder>();

        internal void AddFeeder(string id, string dev, string name, string group)
        {
            lock (Feeders)
            {
                if (Feeders.ContainsKey(id))
                {
                    Warn("Feeder already exists!", id, name);
                    return;
                }
                ModelFeeder f = new ModelFeeder()
                {
                    DeviceId = dev,
                    FeederId = id,
                    FeederName = name,
                    GroupId = group
                };
                Feeders.Add(id, f);
            }
        }

        /// <summary>
        /// The Dictionary of Nodes in the model, keyed by id
        /// </summary>
        [Key(1)]
        public Dictionary<string, Node> Nodes { get; set; } = new Dictionary<string, Node>();

        /// <summary>
        /// The Dictionary of Sources in the model, keyed by id
        /// </summary>
        [Key(2)]
        public Dictionary<string, ModelSource> Sources { get; set; } = new Dictionary<string, ModelSource>();

        /// <summary>
        /// Add a device to the model from an IDF XML element
        /// </summary>
        /// <param name="node">The xml element</param>
        /// <param name="gid">The group id</param>
        /// <param name="type">The type of device we are adding</param>
        /// <param name="phaseshift">The phase shift that happens from side1 to side2 of the device (applicable to transformers only)</param>
        /// <returns>true if adding the device was successful, false otherwise</returns>
        public bool AddDevice(Element node, string gid, DeviceType type, List<Point> geo, int phaseshift = 0)
        {
            var s1nodeid = node.Node.Attribute("s1node")?.Value;
            var s2nodeid = node.Node.Attribute("s2node")?.Value;

            Device d = new Device
            {
                Id = node.Node.Attribute("id").Value,
                Name = node.Node.Attribute("name").Value,
                GroupId = gid,
                Type = type,
                Geometry = geo,
                IdfDevice = node
            };

            string t = node.Node.Attribute("s1phaseID1").Value;
            d.PhaseID[0, 0] = t == "" ? (short)0 : short.Parse(t);
            t = node.Node.Attribute("s1phaseID2")?.Value ?? "";
            d.PhaseID[0, 1] = t == "" ? (short)0 : short.Parse(t);
            t = node.Node.Attribute("s1phaseID3")?.Value ?? "";
            d.PhaseID[0, 2] = t == "" ? (short)0 : short.Parse(t);
            t = node.Node.Attribute("s2phaseID1")?.Value ?? "";
            d.PhaseID[1, 0] = t == "" ? (short)0 : short.Parse(t);
            t = node.Node.Attribute("s2phaseID2")?.Value ?? "";
            d.PhaseID[1, 1] = t == "" ? (short)0 : short.Parse(t);
            t = node.Node.Attribute("s2phaseID3")?.Value ?? "";
            d.PhaseID[1, 2] = t == "" ? (short)0 : short.Parse(t);

            if (type == DeviceType.Transformer)
            {
                d.Base1kV = double.Parse(node.Node.Attribute("s1baseKV").Value);
                d.Base2kV = double.Parse(node.Node.Attribute("s2baseKV").Value);
                d.PhaseShift = phaseshift;
            }
            else
            {
                d.PhaseShift = 0;
                d.Base1kV = d.Base2kV = double.Parse(node.Node.Attribute("baseKV").Value);

                if (type == DeviceType.Switch)
                    d.SwitchState = bool.Parse(node.Node.Attribute("nominalState1").Value);
                else if (type == DeviceType.Line)
                {
                    d.Length = double.Parse(node.Node.Attribute("length").Value);
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
                    if (string.IsNullOrWhiteSpace(n1id))
                    {
                        n1 = null;
                    }
                    else if (Nodes.ContainsKey(n1id))
                    {
                        n1 = Nodes[n1id];
                    }
                    else
                    {
                        n1 = new Node() { Id = n1id };
                        Nodes.Add(n1id, n1);
                    }
                    if (string.IsNullOrWhiteSpace(n2id))
                    {
                        n2 = null;
                    }
                    else if (Nodes.ContainsKey(n2id))
                    {
                        n2 = Nodes[n2id];
                    }
                    else
                    {
                        n2 = new Node() { Id = n2id };
                        Nodes.Add(n2id, n2);
                    }
                    d.Node1 = n1;
                    d.Node2 = n2;

                    Devices.Add(d.Id, d);
                    n1?.Devices.Add(d);
                    n2?.Devices.Add(d);
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
            ModelSource s = new ModelSource
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
        private bool AddSource(ModelSource s)
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
                        foreach (ModelSource s in sources.ToList())
                        {
                            Sources.Remove(s.Id);
                        }
                        CleanOrphanNodes();
                    }
            lock (Feeders)
            {
                var feeders = from f in Feeders.Values where f.GroupId == id select f;
                foreach (ModelFeeder f in feeders.ToList())
                {
                    Feeders.Remove(f.FeederId);
                }
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
                Util.SerializeMessagePack(file, this);
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
            //loop through all the sources and trace connectivity downstream
            foreach (ModelSource s in sources)
            {
                TraceNodeConnectivity(Devices[s.DeviceId], s);
            }
            
            //if (_log.IsDebugEnabled)
            {
                foreach (var device in (from d in Devices.Values where !d.ConnectivityMark select d))
                {
                    //TODO: change to debug
                    Info($"Device is disconnected", device.Id, device.Name);
                }
            }

            TimeSpan runtime = DateTime.Now - start;
            Info($"Connectivity check: {GetDisconnectedCount()} of {Devices.Count} devices disconnected ({runtime.TotalSeconds} seconds)");

        }

        /// <summary>
        /// Marks the node as being connected
        /// </summary>
        /// <param name="d">The device we are tracing into</param>
        /// <param name="s">The source of the trace</param>
        private void TraceNodeConnectivity(Device d, ModelSource s)
        {
            if (d == null)
                return;

            long loop = 0;
            Stack<(Device d, Node n)> stack = new Stack<(Device, Node)>();
            stack.Push((d, d.Node1));

            do
            {
                loop++;
                var set = stack.Pop();

                Node traceNode;
                if (set.d.ConnectivityMark)
                    continue;
                else
                    set.d.ConnectivityMark = true;

                if (set.d.Node1 == set.n)
                {
                    traceNode = set.d.Node2;
                }
                else
                {
                    traceNode = set.d.Node1;
                }
                if (traceNode == null)
                    continue;
                foreach (Device dd in traceNode.Devices)
                {
                    if (dd != set.d)
                    {
                        stack.Push((dd, traceNode));
                    }
                }
            }
            while (stack.Count > 0);
            Info($"Connectivity took {loop} loops for source {s.Name}");
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
        private void TraceNodeEnergization(Device d, ModelSource s)
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
                if (set.d.SwitchState || set.d.Type != DeviceType.Switch && traceNode != null)
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
            Info($"Power flow took {loop} loops for source {s.Name}");
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

                //convert from upstream side (1 or 2) to array indexes (0 or 1)
                int iUp = d.Upstream - 1;
                int iDown = (iUp + 1) % 2;

                //check that we only have phases 1 on index 1, 2 on index 2 etc. 
                // This isn't required by OSI, but it is what we are expecting as output from the Extractor
                for (int i = 0; i < 3; i++)
                {
                    if (d.PhaseID[iUp, i] != 0 && d.PhaseID[iUp, i] != i + 1)
                    {
                        Warn($"Unexpected phase {d.PhaseID[iUp, i]} on index {i+1}.", d);
                    }
                    if (d.PhaseID[iDown, i] != 0 && d.PhaseID[iDown, i] != i + 1)
                    {
                        Warn($"Unexpected phase {d.PhaseID[iDown, i]} on index {i+1}.", d);
                    }
                }

                //for non transformers, loads and generators check that we have the same phases on both sides
                if (d.Type != DeviceType.Transformer && d.Type != DeviceType.Load && d.Type != DeviceType.Generator && d.Type != DeviceType.ShuntCapacitor)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (d.PhaseID[iUp, i] != d.PhaseID[iDown, i])
                        {
                            Error($"Phasing on index {i+1} is not consistent on both sides of the device", d);
                        }
                    }
                }

                //for transformers, check that there isn't invalid downstream phasing
                //for three phase transformers we should see three phases on both sides
                //for single phase transformers we shouldn't see phases assigned to the unused HV phase(s)
                if (d.Type == DeviceType.Transformer)
                {
                    //TODO: remove
                    if (CountPhases(d.PhaseID, 0) == 1)
                    {
                        Error("Was not expecting SWER transformer in VS", d.Id, d.Name);
                        return;
                    }

                    //count up the HV phases to determine if the transformer is three phase
                    bool threephase = true;
                    for (int i = 0; i < 3; i++)
                    {
                        if (d.PhaseID[iUp, i] == 0)
                        {
                            threephase = false;
                        }
                    }

                    //loop through the phases
                    for (int i = 0; i < 3; i++)
                    {
                        if (threephase)
                        {
                            if (d.PhaseID[iUp, i] != d.PhaseID[iDown, i])
                            {
                                Error($"Phasing on index {i+1} is not consistent on both sides of the device", d);
                            }
                        }
                        else
                        {
                            if (d.PhaseID[iUp, i] == 0 && d.PhaseID[iDown, i] != 0)
                            {
                                //this is an error because it will cause DPF to crash
                                Error($"Phasing on upstream side of transformer on index {i+1} is unset, but downstream side is {d.PhaseID[iDown, i]}", d);
                            }
                        }
                    }
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
                            Error($"Phase {i + 1} on side {d.Upstream} doesn't agree with upstream device {us.Name}", d);
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

        public void ExportExtraDeviceInfo()
        {
            XDocument doc = new XDocument();
            XElement data = new XElement("data", new XAttribute("type", "Electric Distribution Extra"), new XAttribute("timestamp", "TODO"), new XAttribute("format", "1.0"));
            XElement groups = new XElement("groups");
            doc.Add(data);
            data.Add(groups);
            var devices = (from i in Devices.Values where i.Type == DeviceType.Switch || i.Type == DeviceType.Load select i).GroupBy(i => i.GroupId);
            foreach (var group in devices)
            {
                XElement xgroup = new XElement("group", new XAttribute("id", group.Key));
                groups.Add(xgroup);
                foreach (var device in group)
                {
                    if (device.Geometry.Any())
                    {
                        XElement element = new XElement("element", new XAttribute("type", "Device"), new XAttribute("id", device.Id), new XAttribute("latitude", device.Geometry[0].Y.ToString()), new XAttribute("longitude", device.Geometry[0].X.ToString()));
                        xgroup.Add(element);
                    }
                }
            }
            doc.Save(Path.Combine(Enricher.I.Options.OutputPath, "DeviceInfo.xml"));
        }

        /// <summary>
        /// Exports the model to two shape files (Devices.shp and Lines.shp)
        /// </summary>
        /// <param name="dir">The directory to export to</param>
        public void ExportToShapeFile(string dir)
        {
            DbfFieldDesc[] deviceFields = new DbfFieldDesc[15];
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

            deviceFields[14] = new DbfFieldDesc
            {
                FieldName = "Feeder",
                FieldLength = 10,
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
                    string[] fieldData = new string[15];
                    fieldData[0] = d.Node1?.Id ?? "-";
                    fieldData[1] = d.Node2?.Id ?? "-";
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
                    fieldData[14] = d.NominalFeeder?.FeederName ?? "-";

                    if (d.Geometry == null)
                    {
                        Warn("Not exporting device to shape file due to null geometry", d);
                        continue; 
                    }
                    if (d.Geometry.Count == 0)
                    {
                        Warn("Not exporting device to shape file due to missing geometry", d);
                        continue;
                    }

                    if (d.Type == DeviceType.Line)
                    {
                        sfwLines.AddRecord(Util.PointToPointD(d.Geometry), d.Geometry.Count, fieldData);
                    }
                    else
                    {
                        sfwDevices.AddRecord(new PointD[] { d.Geometry[0].PointD }, 1, fieldData);
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
            //File.WriteAllText(file, "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]");
            File.WriteAllText(file, "GEOGCS[\"GCS_WGS_1984\", DATUM[\"D_WGS_1984\", SPHEROID[\"WGS_1984\", 6378137, 298.257223563]], PRIMEM[\"Greenwich\", 0], UNIT[\"Degree\", 0.017453292519943295]]");
        }

        public void OverrideSinglePhasing()
        {
            foreach (Device d in Devices.Values)
            {
                if (d.Type == DeviceType.Transformer && CountPhases(d.PhaseID, 1) == 1)
                {
                    //TraceSinglePhasing(d);
                }
            }
        }

        private void ClearTrace()
        {
            foreach (Device d in Devices.Values)
            {
                d.Trace = false;
            }
        }

        private int CountPhases(short[,] array, short index)
        {
            int result = 0;
            for (int i = 0; i < 3; i++)
            {
                if (array[index, i] != 0)
                    result++;
            }
            return result;
        }

        /// <summary>
        /// Traces downstream of a single phse transformer and overwrites all phasing along the way
        /// </summary>
        /// <param name="d"></param>
        private void TraceSinglePhasing(Device d)
        {
            if (CountPhases(d.PhaseID, 1) != 1 || d.Type != DeviceType.Transformer)
            {
                Warn("Not a single phase transformer", d.Id, d.Name);
                return;
            }

            ///RW transformer
            if (d.PhaseID[0,0] == 1 && d.PhaseID[0, 1] == 2 && d.PhaseID[0, 2] == 0)
            {
                d.PhaseID[1, 0] = 1;
                d.PhaseID[1, 1] = 0;
                d.PhaseID[1, 2] = 0;
            }

            ///WB transformer
            else if (d.PhaseID[0, 0] == 0 && d.PhaseID[0, 1] == 2 && d.PhaseID[0, 2] == 3)
            {
                d.PhaseID[1, 0] = 0;
                d.PhaseID[1, 1] = 2;
                d.PhaseID[1, 2] = 0;
            }

            ///BR transformer
            else if (d.PhaseID[0, 0] == 1 && d.PhaseID[0, 1] == 0 && d.PhaseID[0, 2] == 3)
            {
                d.PhaseID[1, 0] = 0;
                d.PhaseID[1, 1] = 0;
                d.PhaseID[1, 2] = 3;
            }
            ///R SWER
            else if (d.PhaseID[0, 0] == 1 && d.PhaseID[0, 1] == 0 && d.PhaseID[0, 2] == 0)
            {
                d.PhaseID[1, 0] = 1;
                d.PhaseID[1, 1] = 0;
                d.PhaseID[1, 2] = 0;
            }
            ///W SWER
            else if (d.PhaseID[0, 0] == 0 && d.PhaseID[0, 1] == 2 && d.PhaseID[0, 2] == 0)
            {
                d.PhaseID[1, 0] = 0;
                d.PhaseID[1, 1] = 2;
                d.PhaseID[1, 2] = 0;
            }
            ///B SWER
            else if (d.PhaseID[0, 0] == 0 && d.PhaseID[0, 1] == 0 && d.PhaseID[0, 2] == 3)
            {
                d.PhaseID[1, 0] = 0;
                d.PhaseID[1, 1] = 0;
                d.PhaseID[1, 2] = 3;
            }
            else
            {
                Error("Unknown HV phasing", d.Id, d.Name);
                return;
            }

            long loop = 0;

            //each stack item is (device to trace through, node that 
            Stack<(Device d, Node n, Device ud)> stack = new Stack<(Device, Node, Device)>();
            stack.Push((d, d.Node1, null));
            do
            {

                loop++;
                var set = stack.Pop();
                Node traceNode = null;
                //if we haven't visited this node from source s, add new PFDetails for this source
                if (set.d.Trace)
                    continue;
                else
                    set.d.Trace = true;

                var dontprocess = (!set.d.SwitchState && set.d.Type == DeviceType.Switch) || (set.d.Type == DeviceType.Transformer && d != set.d);
                if (dontprocess)
                    return;

                if (!d.PhaseID.Equals(set.d.PhaseID))
                {
                    set.d.PhaseID[0, 0] = set.d.PhaseID[1, 0] = d.PhaseID[1, 0];
                    set.d.PhaseID[0, 1] = set.d.PhaseID[1, 1] = d.PhaseID[1, 1];
                    set.d.PhaseID[0, 2] = set.d.PhaseID[1, 2] = d.PhaseID[1, 2];                    
                    Warn("Overwriting phasing", set.d.Id, set.d.Name);
                }

                //we are coming in from side 1
                if (set.d?.Node1 == set.n)
                {
                    traceNode = set.d.Node2;
                }
                else if (set.d?.Node2 == set.n)
                {
                    traceNode = set.d.Node1;
                }
                
                foreach (Device dd in traceNode?.Devices ?? Enumerable.Empty<Device>())
                {
                    if (dd != set.d)
                    {
                        stack.Push((dd, traceNode, set.d));
                    }
                }
            }
            while (stack.Count > 0);
            Debug($"Trace took {loop} loops", d.Id, d.Name);
        }

        /// <summary>
        /// Calculates the nominal feeder for all devices
        /// </summary>
        public void CalculateNominalFeeders()
        {
            Info("Calculating nominal feeders...");
            DateTime start = DateTime.Now;
            ClearTrace();

            foreach (var source in Sources.Values)
            {
                if (Devices.ContainsKey(source.DeviceId))
                    TraceNominalFeeders(Devices[source.DeviceId], source);
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Feeder tracing runtime: {runtime.TotalSeconds}s");
        }

        /// <summary>
        /// Trace though the network starting at this device, and propagating feeders down the network
        /// </summary>
        /// <param name="d">The device we are tracing into</param>
        /// <param name="s">The source that is energizing this trace</param>
        private void TraceNominalFeeders(Device d, ModelSource s)
        {
            long loop = 0;
            //The stack keeps track of all the branches
            //The tuple items are:
            //d - the device we are tracing into
            //n - the node we are tracing in from
            //ud - the device the trace came from
            //TODO: make this a queue
            Stack<(Device d, Node n, Device ud, ModelFeeder feeder)> stack = new Stack<(Device, Node, Device, ModelFeeder)>();
            stack.Push((d, d.Node1, null, null));
            do
            {
                loop++;
                var set = stack.Pop();
                Node traceNode = null;
                ModelFeeder currentFeeder = set.feeder;

                //if we have been here before then continue
                if (set.d.Trace)
                    continue;
                else
                    set.d.Trace = true;

                var openSwitch = !set.d.SwitchState && set.d.Type == DeviceType.Switch;

                //check if there is a feeder attached to this device
                var feeder = (from f in Feeders.Values where f.DeviceId == set.d.Id select f).FirstOrDefault();
                if (feeder != null)
                {
                    currentFeeder = feeder;
                }
                //set device feeder
                set.d.NominalFeeder = currentFeeder;
                SetElementNominalFeeder(set.d);

                if (openSwitch)
                    continue;

                //we are coming in from side 1
                if (set.d.Node1 == set.n)
                {
                    traceNode = set.d.Node2;
                }
                else if (set.d.Node2 == set.n)//we are coming in from side 2
                {
                    traceNode = set.d.Node1;
                }
                //else single sided device
                
                if (traceNode != null)
                {
                    foreach (Device dd in traceNode.Devices)
                    {
                        if (dd != set.d && dd.UpstreamNode == traceNode)
                        {
                            stack.Push((dd, traceNode, set.d, currentFeeder));
                        }
                    }
                }
            }
            while (stack.Count > 0);
            Info($"Feeder trace took {loop} loops for source {s.Name}");
        }

        private void SetElementNominalFeeder(Device d)
        {
            if (d.NominalFeeder == null || d.IdfDevice == null)
                return;

            d.IdfDevice.Node.SetAttributeValue("nominalFeeder", d.NominalFeeder.FeederId);
        }

        private void Debug(string message, Device d, [CallerMemberName]string caller = "")
        {
            Debug(message, d.Id, d.Name, caller);
        }
        private void Info(string message, Device d, [CallerMemberName]string caller = "")
        {
            Info(message, d.Id, d.Name, caller);
        }
        private void Warn(string message, Device d, [CallerMemberName]string caller = "")
        {
            Warn(message, d.Id, d.Name, caller);
        }
        private void Error(string message, Device d, [CallerMemberName]string caller = "")
        {
            Error(message, d.Id, d.Name, caller);
        }
        private void Fatal(string message, Device d, [CallerMemberName]string caller = "")
        {
            Fatal(message, d.Id, d.Name, caller);
        }

    }
}
