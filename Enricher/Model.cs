using EGIS.ShapeFileLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    [Serializable]
    [MessagePackObject]
    public class Model : ErrorReporter
    {
        /// <summary>
        /// The Devices in the model, keyed by id
        /// </summary>
        [Key(0)]
        public Dictionary<string, ModelDevice> Devices { get; set; } = new Dictionary<string, ModelDevice>();

        /// <summary>
        /// The Nodes in the model, keyed by id
        /// </summary>
        [Key(1)]
        public Dictionary<string, ModelNode> Nodes { get; set; } = new Dictionary<string, ModelNode>();

        /// <summary>
        /// The Sources in the model, keyed by id
        /// </summary>
        [Key(2)]
        public Dictionary<string, ModelSource> Sources { get; set; } = new Dictionary<string, ModelSource>();

        /// <summary>
        /// The Feeders in the model, keyed by id
        /// </summary>
        [Key(3)]
        public Dictionary<string, ModelFeeder> Feeders { get; set; } = new Dictionary<string, ModelFeeder>();

        /// <summary>
        /// The number of disconnected devices
        /// </summary>
        [IgnoreMember]
        public int DisconnectedCount
        {
            get
            {
                var devices = from d in Devices.Values where !d.Connectivity select d;
                return devices.Count();
            }
        }

        /// <summary>
        /// The number of deenergized devices
        /// </summary>
        [IgnoreMember]
        public int DeenergizedCount
        {
            get
            {
                var devices = from d in Devices.Values where d.SP2S.Count == 0 select d;
                return devices.Count();
            }
        }
        [IgnoreMember]
        public int TxCount
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Transformer || d.Type == DeviceType.EarthingTransformer).Count();
            }
        }
        [IgnoreMember]
        public int LineCount
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Line).Count();
            }
        }
        [IgnoreMember]
        public int Line5Count
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Line && !d.Name.Contains("Bus") && d.Length <= 5).Count();
            }
        }
        [IgnoreMember]
        public int Line25Count
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Line && !d.Name.Contains("Bus") && d.Length <= 25).Count();
            }
        }
        [IgnoreMember]
        public int LoadCount
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Load).Count();
            }
        }
        [IgnoreMember]
        public int SwitchCount
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Switch).Count();
            }
        }
        [IgnoreMember]
        public int FeederCount
        {
            get
            {
                return Feeders.Count;            }
        }
        [IgnoreMember]
        public int RegCount
        {
            get
            {
                return Devices.Values.Where(d => d.Type == DeviceType.Regulator).Count();
            }
        }

        private readonly Random _rnd = new Random();


        /// <summary>
        /// Add a feeder to the model
        /// </summary>
        /// <param name="feederId"></param>
        /// <param name="feederName"></param>
        /// <param name="deviceId"></param>
        /// <param name="groupId"></param>
        public void AddFeeder(string feederId, string feederName, string deviceId, string groupId)
        {
            lock (Feeders)
            {
                if (Feeders.ContainsKey(feederId))
                {
                    Warn("Feeder already exists!", feederId, feederName);
                    return;
                }
                ModelFeeder f = new ModelFeeder()
                {
                    DeviceId = deviceId,
                    FeederId = feederId,
                    FeederName = feederName,
                    GroupId = groupId
                };
                Feeders.Add(feederId, f);
            }
        }

        /// <summary>
        /// Add a device to the model from an IDF XML element
        /// </summary>
        /// <param name="node">The xml element</param>
        /// <param name="gid">The group id</param>
        /// <param name="type">The type of device we are adding</param>
        /// <param name="phaseshift">The phase shift that happens from side1 to side2 of the device (applicable to transformers only)</param>
        /// <returns>true if adding the device was successful, false otherwise</returns>
        public ModelDevice AddDevice(IdfElement node, string gid, DeviceType type, string symbol = null, short phaseshift = 0, double kva = 0)
        {
            var s1nodeid = node.Node.Attribute("s1node")?.Value;
            var s2nodeid = node.Node.Attribute("s2node")?.Value;

            ModelDevice d = new ModelDevice
            {
                Id = node.Node.Attribute("id").Value,
                Name = node.Node.Attribute("name").Value,
                GroupId = gid,
                Type = type,
                //Geometry = geo,
                IdfDevice = node,
                //Internals = internals,
                SymbolName = symbol
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
                d.NominalkVA = kva;
                d.PhaseShift = phaseshift;
            }
            else if (type == DeviceType.EarthingTransformer)
            {
                d.Base1kV = double.Parse(node.Node.Attribute("s1baseKV").Value);
                d.Base2kV = double.NaN;
                d.NominalkVA = double.NaN;
                d.PhaseShift = 0;
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
        private ModelDevice AddDevice(ModelDevice d, string n1id, string n2id)
        {
            if (Devices.ContainsKey(d.Id))
            {
                Err($"Device Id already exists.", d.Id, d.Name);
                return null;
            }
            //in case someone does another operation at the same time
            lock (Devices) lock (Nodes)
                {
                    ModelNode n1, n2;
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
                        n1 = new ModelNode() { Id = n1id };
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
                        n2 = new ModelNode() { Id = n2id };
                        Nodes.Add(n2id, n2);
                    }
                    d.Node1 = n1;
                    d.Node2 = n2;

                    Devices.Add(d.Id, d);
                    n1?.Devices.Add(d);
                    n2?.Devices.Add(d);
                }
            return d;
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
            s.Phasing = (short)((short.Parse(node.Attribute("phase1Angle").Value) / 30 + 12) % 12);
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
                Err($"Source with id [{s.Id}] already exists");
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
        /// TODO: this is an expensive operation... maybe we should track groups as a separate Dictionary<groupid, list<device>> for speeeeed
        /// </summary>
        /// <param name="id">The group id</param>        
        public void RemoveGroup(string id)
        {
            lock (Devices) lock (Nodes) lock (Sources)
                    {
                        var devices = from d in Devices.Values where d.GroupId == id select d;
                        foreach (ModelDevice d in devices.ToList())
                        {
                            RemoveDevice(d);
                        }
                        var sources = from s in Sources.Values where s.GroupId == id select s;
                        foreach (ModelSource s in sources.ToList())
                        {
                            Sources.Remove(s.Id);
                        }
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
        private void RemoveDevice(ModelDevice d)
        {
            lock (Devices) lock (Nodes)
            {
                //remove the device from node1
                d.Node1?.Devices.Remove(d);
                //if node1 has no devices left, then remvoe the node
                if (d.Node1?.Devices.Count == 0)
                    Nodes.Remove(d.Node1.Id);

                d.Node2?.Devices.Remove(d);
                if (d.Node2?.Devices.Count == 0)
                    Nodes.Remove(d.Node2.Id);

                Devices.Remove(d.Id);               
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

        public List<string> GetGroups()
        {
            return (from d in Devices.Values select d.GroupId).Distinct().ToList();
        }

        /// <summary>
        /// Loads the model from a file
        /// </summary>
        /// <param name="file">The filename</param>
        /// <returns>A new NodeModel, or null if the operation fails</returns>
        public static Model Deserialize(string file)
        {
            StaticInfo("Loading model...", typeof(Model));
            try
            {
                var model = Util.DeserializeMessagePack<Model>(file);
                if (model != null)
                    model.RebuildNodeReferences();
                return model;
            }
            catch (Exception ex)
            {
                StaticFatal($"Failed to deserialize model from file [{file}]. {ex.Message}", typeof(Model));
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
                device.Node1?.Devices.Add(device);
                device.Node2?.Devices.Add(device);
            }
        }


        /// <summary>
        /// Verify that all devices are connected to a source
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
                TraceConnectivity(s);
            }

            foreach (var device in (from d in Devices.Values where !d.Connectivity select d))
            {
                Warn($"Device is disconnected {device.Type}", device.Id, device.Name);
            }

            TimeSpan runtime = DateTime.Now - start;
            Info($"Connectivity check: {DisconnectedCount} of {Devices.Count} devices disconnected ({runtime.TotalSeconds} seconds)");

        }

        /// <summary>
        /// Marks the node as being connected
        /// </summary>
        /// <param name="s">The source for the trace</param>
        private void TraceConnectivity(ModelSource s)
        {
            ModelDevice d = Devices[s.DeviceId];
            if (d == null)
                return;

            long loop = 0;
            Stack<(ModelDevice d, ModelNode n)> stack = new Stack<(ModelDevice, ModelNode)>();
            stack.Push((d, d.Node1));

            do
            {
                loop++;
                var set = stack.Pop();

                ModelNode traceNode;
                if (set.d.Connectivity)
                    continue;
                else
                    set.d.Connectivity = true;

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
                foreach (ModelDevice dd in traceNode.Devices)
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
        /// Verifies that all devices connected to each node have the same base voltage
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
                    Err($"Device is connected to a dirty node [{node.Id}]", device.Id, device.Name);
                }
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"There were {count} nodes with inconsistent base voltages ({runtime.Seconds}s)");
        }


        /// <summary>
        /// Traces outwards from each source
        /// Calculates the likely upstream side of each device
        /// Checks the phasing of each device to ensure consistency with upstream devices
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
                    TraceEnergization(source);
            }
            foreach (ModelDevice dd in Devices.Values)
            {
                dd.CalculateUpstreamSide();
                dd.CalculatePhaseShift();
            }
            ValidateDevicePhasing();
            var openpoints = from d in Devices.Values where d.Type == DeviceType.Switch && d.CalculatedPhaseShift != 0 && d.CalculatedPhaseShift != null select d;
            foreach (var openpoint in openpoints)
            {
                Info($"Switch has a phase shift of {openpoint.CalculatedPhaseShift}", openpoint);
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Power flow check: {DeenergizedCount} of {Devices.Count} devices deenergized ({runtime.TotalSeconds}s)");
        }

        /// <summary>
        /// Trace though the network starting at this device, calculating the shortest path to the source for each device
        /// </summary>
        /// <param name="d">The device we are tracing into</param>
        /// <param name="s">The source that is energizing this trace</param>
        /// <param name="distance">The distance thus far from the source</param>
        private void TraceEnergization(ModelSource s)
        {
            ModelDevice d = Devices[s.DeviceId];
            if (d == null)
                return;

            long loop = 0;
            //The stack keeps track of all the branches
            //The tuple items are:
            //d - the device we are tracing into
            //n - the node we are tracing in from
            //ud - the device the trace came from
            //distance - the distance thus far from the source
            //phase - phasing in clock units of phase 1
            Queue<(ModelDevice d, ModelNode n, ModelDevice ud, double distance, short phase)> stack = new Queue<(ModelDevice, ModelNode, ModelDevice, double, short)>();
            stack.Enqueue((d, d.Node1, null, 0, s.Phasing));
            do
            {
                loop++;
                var set = stack.Dequeue();
                ModelNode traceNode;
                //if we haven't visited this node from source s, add new PFDetails for this source
                if (!set.d.SP2S.ContainsKey(s))
                    set.d.SP2S.Add(s, new PFDetail());

                var openSwitch = !set.d.SwitchState && set.d.Type == DeviceType.Switch;

                //we are coming in from side 1
                if (set.d.Node1 == set.n)
                {
                    //if we have already been here from a shorter path, then quit
                    if (set.distance >= set.d.SP2S[s].N1IntDistance)
                        continue;
                    set.d.SP2S[s].Phasing[0] = set.phase;
                    set.d.Energization[0] = true;
                    //mark that we have been here from side 1 and update the distance
                    set.d.SP2S[s].N1ExtDistance = set.d.SP2S[s].N1IntDistance = set.distance;
                    if (!openSwitch)
                    {
                        set.d.SP2S[s].N2IntDistance = set.distance + d.Length;
                        set.d.Energization[1] = true;
                    } 
                    if (set.d.Type == DeviceType.Transformer)
                        set.d.SP2S[s].Phasing[1] = set.phase = (short)((set.phase + set.d.PhaseShift) % 12);
                    else
                        set.d.SP2S[s].Phasing[1] = set.phase;
                    traceNode = set.d.Node2;
                }
                else //we are coming in from side 2
                {
                    if (set.distance >= set.d.SP2S[s].N2IntDistance)
                        continue;
                    set.d.SP2S[s].Phasing[1] = set.phase;
                    set.d.Energization[1] = true;
                    set.d.SP2S[s].N2ExtDistance = set.d.SP2S[s].N2IntDistance = set.distance;
                    if (!openSwitch)
                    {
                        set.d.SP2S[s].N1IntDistance = set.distance + d.Length;
                        set.d.Energization[0] = true;
                    }
                    if (set.d.Type == DeviceType.Transformer)
                        set.d.SP2S[s].Phasing[0] = set.phase = (short)((set.phase + (12 - set.d.PhaseShift)) % 12);
                    else
                        set.d.SP2S[s].Phasing[0] = set.phase;
                    traceNode = set.d.Node1;
                }
                if (!openSwitch && traceNode != null)
                {
                    foreach (ModelDevice dd in traceNode.Devices)
                    {
                        if (dd != set.d)
                        {
                            stack.Enqueue((dd, traceNode, set.d, set.distance + set.d.Length, set.phase));
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
            foreach (ModelDevice d in Devices.Values)
            {
                //don't check disconnected, deenergized devices or head devices
                if (!d.Connectivity || d.Upstream == 0 || d.SP2S.Count == 0)
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
                        Warn($"Unexpected phase {d.PhaseID[iUp, i]} on index {i + 1}.", d);
                    }
                    if (d.PhaseID[iDown, i] != 0 && d.PhaseID[iDown, i] != i + 1)
                    {
                        Warn($"Unexpected phase {d.PhaseID[iDown, i]} on index {i + 1}.", d);
                    }
                }



                //for non transformers, loads and generators check that we have the same phases on both sides
                if (d.Type != DeviceType.Transformer && d.Type != DeviceType.Load && d.Type != DeviceType.Generator && d.Type != DeviceType.ShuntCapacitor && d.Type != DeviceType.EarthingTransformer)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (d.PhaseID[iUp, i] != d.PhaseID[iDown, i])
                        {
                            
                            Debugger.Launch(); 
                            Error($"Phasing on index {i + 1} is not consistent on both sides of the device", d);
                        }
                    }
                }

                //for transformers, check that there isn't invalid downstream phasing
                //for three phase transformers we should see three phases on both sides
                //for single phase transformers we shouldn't see phases assigned to the unused HV phase(s)
                if (d.Type == DeviceType.Transformer)
                {
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
                                Error($"Phasing on index {i + 1} is not consistent on both sides of the device", d);
                            }
                        }
                        else
                        {
                            if (d.PhaseID[iUp, i] == 0 && d.PhaseID[iDown, i] != 0)
                            {
                                //this is an error because it will cause DPF to crash
                                Error($"Phasing on upstream side of transformer on index {i + 1} is unset, but downstream side is {d.PhaseID[iDown, i]}", d);
                            }
                        }
                    }
                }

                //check that the phase IDs on this device match the phase IDs on all upstream devices
                foreach (ModelDevice us in d.GetUpstreamDevices())
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
                    TraceNominalFeeders(source);
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Feeder tracing runtime: {runtime.TotalSeconds}s");
        }

        /// <summary>
        /// Trace though the network starting at this device, and propagating feeders down the network
        /// </summary>
        /// <param name="s">The source that is energizing this trace</param>
        private void TraceNominalFeeders(ModelSource s)
        {
            ModelDevice d = Devices[s.DeviceId];

            Dictionary<ModelDevice, ModelFeeder> feederMap = new Dictionary<ModelDevice, ModelFeeder>();
            foreach (var f in Feeders.Values)
            {
                if (Devices.ContainsKey(f.DeviceId))
                {
                    if (!feederMap.ContainsKey(Devices[f.DeviceId]))
                    {
                        feederMap.Add(Devices[f.DeviceId], f);
                    }
                }
            }

            long loop = 0;
            //The stack keeps track of all the branches
            //The tuple items are:
            //d - the device we are tracing into
            //n - the node we are tracing in from
            //ud - the device the trace came from
            //feeder - the current feeder
            //tx - set when the trace goes through a 400V transformer, and reset when leaving internals
            //c - the current feeder colour
            Stack<(ModelDevice d, ModelNode n, ModelDevice ud, ModelFeeder feeder, bool tx, Color c)> stack = new Stack<(ModelDevice, ModelNode, ModelDevice, ModelFeeder, bool, Color)>();
            stack.Push((d, d.Node1, null, null, false, Color.Green)) ;
            do
            {
                loop++;
                var set = stack.Pop();
                ModelNode traceNode = null;
                ModelFeeder currentFeeder = set.feeder;
                Color currentColor = set.c;
                bool tx = set.tx;

                //if we have been here before then continue
                if (set.d.Trace)
                    continue;
                else
                    set.d.Trace = true;

                var openSwitch = !set.d.SwitchState && set.d.Type == DeviceType.Switch;

                //check if there is a feeder attached to this device
                if (feederMap.ContainsKey(set.d))
                {
                    currentFeeder = feederMap[set.d];
                    currentColor = RandomColor();
                }

                //if we are going through a distribution transformer, then set the tx flag to true
                if (set.d.Type == DeviceType.Transformer && set.d.Base2kV == 0.4 && set.d.Downstream == 2)
                    tx = true;

                //if we are not in internals, then clear the tx flag
                if (!set.d.Internals)
                    tx = false;

                //if the tx flag is set, then we are still in internals, and every
                //switch we go through should change the line color
                //this is required for the case where there are incomer switches
                if (set.d.Type == DeviceType.Switch && tx)
                {
                    //if the switch is already in the model with a different color, then use the existing one
                   if (set.d.Color != ColorTranslator.ToHtml(currentColor) && !string.IsNullOrWhiteSpace(set.d.Color))
                         currentColor = ColorTranslator.FromHtml(set.d.Color);
                    else
                        currentColor = RandomColor();
                }
                //var t = Color.Empty;

                //set device feeder and color
                set.d.NominalFeeder = currentFeeder;
                set.d.Color = ColorTranslator.ToHtml(currentColor);

                if (set.d.NominalFeeder != null && set.d.IdfDevice != null)
                    set.d.IdfDevice.Node.SetAttributeValue("nominalFeeder", set.d.NominalFeeder.FeederId);


                //if (set.d.Type == DeviceType.Line && set.d.IdfDevice != null)
                //    set.d.IdfDevice.ParentGroup.SetLineColor(set.d.Id, currentColor);

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
                    foreach (ModelDevice dd in traceNode.Devices)
                    {
                        if (dd != set.d && dd.UpstreamNode == traceNode)
                        {
                            stack.Push((dd, traceNode, set.d, currentFeeder, tx, currentColor));
                        }
                    }
                }
            }
            while (stack.Count > 0);
            Info($"Feeder trace took {loop} loops for source {s.Name}");
        }

        /// <summary>
        /// Traces to loads and allocates the nominal kVA based on the size of the upstream transformer and the number of loads attached to it
        /// </summary>
        public void TraceLoadAllocation()
        {
            Info("Calculating load allocation...");
            DateTime start = DateTime.Now;
            ClearTrace();

            foreach (var source in Sources.Values)
            {
                if (Devices.ContainsKey(source.DeviceId))
                    TraceLoadAllocation(source);
            }
            TimeSpan runtime = DateTime.Now - start;
            Info($"Load allocation runtime: {runtime.TotalSeconds}s");
        }

        /// <summary>
        /// Trace downstream from a source, calculating the load allocation to each load
        /// </summary>
        /// <param name="s">The source that is energizing this trace</param>
        private void TraceLoadAllocation(ModelSource s)
        {
            ModelDevice d = Devices[s.DeviceId];

            long loop = 0;

            Dictionary<ModelDevice, List<ModelDevice>> trannyMap = new Dictionary<ModelDevice, List<ModelDevice>>();
            //The stack keeps track of all the branches
            //The tuple items are:
            //d - the device we are tracing into
            //n - the node we are tracing in from
            //ud - the device the trace came from
            //tx - the nearest upstream transformer
            //single - are we on a single phase network?
            Queue<(ModelDevice d, ModelNode n, ModelDevice ud, ModelDevice tx, bool single)> stack = new Queue<(ModelDevice, ModelNode, ModelDevice, ModelDevice, bool)>();
            stack.Enqueue((d, d.Node1, null, null, false));
            do
            {
                //trace boilerplace start
                loop++;
                var set = stack.Dequeue();
                ModelNode traceNode = null;
                bool single = set.single;
                //if we have been here before then continue
                if (set.d.Trace)
                    continue;
                else
                    set.d.Trace = true;
                var openSwitch = !set.d.SwitchState && set.d.Type == DeviceType.Switch;
                //trace boilerplate end

                ModelDevice tx = set.tx;
                //if we are going through a distribution transformer, then set tx
                //TODO: should we filter on secondary voltage here?
                if (set.d.Type == DeviceType.Transformer)
                {
                    tx = set.d;

                    if (!single && CountPhases(set.d.PhaseID, (short)(set.d.Upstream - 1)) == 1)
                        Warn("Was not expecting SWER transformer here", set.d);

                    //if the downstream side of the transformer has only one phase, then we are now on a single phase network
                    if (CountPhases(set.d.PhaseID, (short)(set.d.Downstream - 1)) == 1)
                        single = true;
                    else
                        single = false;
                }
                //if we are going through a load, then add it to the tranny map
                else if (set.d.Type == DeviceType.Load)
                {
                    if (tx == null)
                        Warn("Was not expecting tranny to be null", set.d);
                    else
                    {
                        if (!trannyMap.ContainsKey(tx))
                            trannyMap.Add(tx, new List<ModelDevice>());
                        trannyMap[tx].Add(set.d);
                    }
                }

                //trace boilerplate start
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
                    foreach (ModelDevice dd in traceNode.Devices)
                    {
                        if (dd != set.d && dd.UpstreamNode == traceNode)
                        {
                            stack.Enqueue((dd, traceNode, set.d, tx, single));
                        }
                    }
                }
                //trace boilerplate end
            }
            while (stack.Count > 0);

            foreach (var kvp in trannyMap)
            {
                double loads = kvp.Value.Count;
                foreach (var load in kvp.Value)
                {
                    load.NominalkVA = kvp.Key.NominalkVA / loads;
                    //apply a blanket 30% reduction - because when closing onto a dead circuit dpf will take this as gospel!
                    load.NominalkVA *= 0.3;
                    if (load.IdfDevice is IdfLoad)
                        ((IdfLoad)load.IdfDevice).SetNominalLoad(load.NominalkVA);
                }
            }
            Info($"Feeder trace took {loop} loops for source {s.Name}");
        }

        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns></returns>
        private Color RandomColor()
        {
            return Color.FromArgb(_rnd.Next(256), _rnd.Next(256), _rnd.Next(256));
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
        /// Export the Lat\Long for all switches and loads.  Used by OMS to add locations to devices.
        /// </summary>
        public void ExportDeviceCoordinates()
        {
            XDocument doc = new XDocument();
            XElement data = new XElement("data", new XAttribute("type", "Electric Distribution Extra"), new XAttribute("timestamp", DateTime.UtcNow.ToString("s")), new XAttribute("format", "1.0"));
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
            doc.Save(Path.Combine(Program.Options.OutputPath, "DeviceInfo.exml"));
        }

        /// <summary>
        /// Exports the model to two shape files (Devices.shp and Lines.shp)
        /// </summary>
        /// <param name="dir">The directory to export to</param>
        public void ExportToShapeFile(string dir)
        {
            DbfFieldDesc[] deviceFields = new DbfFieldDesc[17];
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
                FieldLength = 15,
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
                FieldLength = 20,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[9] = new DbfFieldDesc
            {
                FieldName = "BasekV",
                FieldLength = 20,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[10] = new DbfFieldDesc
            {
                FieldName = "Phases",
                FieldLength = 10,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[11] = new DbfFieldDesc
            {
                FieldName = "PhaseShift",
                FieldLength = 10,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };

            deviceFields[12] = new DbfFieldDesc
            {
                FieldName = "Feeder",
                FieldLength = 10,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[13] = new DbfFieldDesc
            {
                FieldName = "Color",
                FieldLength = 10,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[14] = new DbfFieldDesc
            {
                FieldName = "NominalkVA",
                FieldLength = 10,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[15] = new DbfFieldDesc
            {
                FieldName = "Phasing",
                FieldLength = 20,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            deviceFields[16] = new DbfFieldDesc
            {
                FieldName = "Flags",
                FieldLength = 5,
                RecordOffset = 0,
                FieldType = DbfFieldType.Character,
            };
            ShapeFileWriter sfwDevices = ShapeFileWriter.CreateWriter(dir, "Devices", ShapeType.Point, deviceFields);
            ExportWebMercatorProjectionFile(Path.Combine(dir, "Devices.prj"));
            ShapeFileWriter sfwLines = ShapeFileWriter.CreateWriter(dir, "Lines", ShapeType.PolyLine, deviceFields);
            ExportWebMercatorProjectionFile(Path.Combine(dir, "Lines.prj"));
            try
            {
                foreach (ModelDevice d in Devices.Values)
                {
                    string[] fieldData = new string[17];
                    fieldData[0] = d.Node1?.Id ?? "-";
                    fieldData[1] = d.Node2?.Id ?? "-";
                    fieldData[2] = d.Id;
                    fieldData[3] = d.Name;
                    fieldData[4] = d.GroupId;
                    fieldData[5] = d.Connectivity ? "1" : "0";
                    fieldData[6] = $"{d.Energization[0]}/{d.Energization[1]}";
                    fieldData[7] = d.Upstream.ToString();
                    fieldData[8] = d.Type.ToString();
                    fieldData[9] = $"{d.Base1kV.ToString("F1")}/{d.Base2kV.ToString("F1")}";
                    fieldData[10] = $"{d.PhaseID[0, 0]}{d.PhaseID[0, 1]}{d.PhaseID[0, 2]}/{d.PhaseID[1, 0]}{d.PhaseID[1, 1]}{d.PhaseID[1, 2]}";
                    fieldData[11] = $"{d.PhaseShift}/{d.CalculatedPhaseShift}";
                    fieldData[12] = d.NominalFeeder?.FeederName ?? "-";
                    fieldData[13] = d.Color ?? "";
                    fieldData[14] = d.NominalkVA.ToString("N5") ?? "";
                    fieldData[15] = $"{d.Phasing[0]}-{(d.Phasing[0] + 4) % 12}-{(d.Phasing[0] + 8) % 12}/{d.Phasing[1]}-{(d.Phasing[1] + 4) % 12}-{(d.Phasing[1] + 8) % 12}";
                    fieldData[16] = d.Flags.ToString();
                    
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
        private static void ExportWebMercatorProjectionFile(string file)
        {
            File.WriteAllText(file, "GEOGCS[\"GCS_WGS_1984\", DATUM[\"D_WGS_1984\", SPHEROID[\"WGS_1984\", 6378137, 298.257223563]], PRIMEM[\"Greenwich\", 0], UNIT[\"Degree\", 0.017453292519943295]]");
        }

        /// <summary>
        /// Clear the trace flag from all devices
        /// </summary>
        private void ClearTrace()
        {
            foreach (ModelDevice d in Devices.Values)
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


        #region Log Functions
        private void Debug(string message, ModelDevice d, [CallerMemberName]string caller = "")
        {
            Debug(message, d.Id, d.Name, caller);
        }
        private void Info(string message, ModelDevice d, [CallerMemberName]string caller = "")
        {
            Info(message, d.Id, d.Name, caller);
        }
        private void Warn(string message, ModelDevice d, [CallerMemberName]string caller = "")
        {
            Warn(message, d.Id, d.Name, caller);
        }
        private void Error(string message, ModelDevice d, [CallerMemberName]string caller = "")
        {
            Err(message, d.Id, d.Name, caller);
        }
        private void Fatal(string message, ModelDevice d, [CallerMemberName]string caller = "")
        {
            Fatal(message, d.Id, d.Name, caller);
        }
        #endregion

    }
}
