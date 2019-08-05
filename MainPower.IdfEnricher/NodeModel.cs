using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    [Serializable]
    [MessagePackObject]
    public class NodeModel : ErrorReporter
    {
        [Key(0)]
        public Dictionary<string, Device> Devices = new Dictionary<string, Device>();

        [Key(1)]
        public Dictionary<string, Node> Nodes = new Dictionary<string, Node>();

        [Key(2)]
        public Dictionary<string, Source> Sources = new Dictionary<string, Source>();

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

        private void RemoveDevice(Device d)
        {
            lock (Devices)
            {
                d.Node1.Devices.Remove(d);
                d.Node2.Devices.Remove(d);
                Devices.Remove(d.Id);
            }
        }

        public void AddDevice(string id, string n1id, string n2id, string gid, string name, DeviceType type, double base1kv, double base2kv, bool state = false, double length = 0)
        {
            if (Devices.ContainsKey(id))
            {
                //TODO error
                return;
            }
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
                Device d = new Device()
                {
                    Name = name,
                    Node1 = n1,
                    Node2 = n2,
                    Id = id,
                    GroupId = gid,
                    Type = type,
                    SwitchState = state,
                    Length = length,
                    Base1kV = base1kv,
                    Base2kV = base2kv
                };

                Devices.Add(id, d);
                n1.Devices.Add(d);
                n2.Devices.Add(d);
            }
        }

        public void AddSource(string id, string gid, string name, string did)
        {
            lock (Sources)
            {
                Source s = new Source()
                {
                    DeviceId = did,
                    Id = id,
                    GroupId = gid,
                    Name = name
                };
                Sources.Add(s.Id, s);
            }
        }

        public void AddSource(XElement node, string gid)
        {
            AddSource(node.Attribute("id").Value, gid, node.Attribute("name").Value, node.Attribute("device").Value);
        }

        public void CheckVoltageConsistency()
        {
            long count = 0;
            foreach (var node in Nodes.Values)
            {
                var basekv = double.NaN;
                foreach (var device in node.Devices)
                {
                    double nodevoltage;
                    int side;
                    if (device.Node1 == node)
                    {
                        nodevoltage = device.Base1kV;
                        side = 1;
                    }
                    else
                    {
                        nodevoltage = device.Base2kV;
                        side = 2;
                    }
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
                    Warn($"Device [{device.Id}\\{device.Name} is connected to a dirty node [{node.Id}] with inconsistent voltages");
                }
            }
            Info($"There were {count} nodes with inconsistent base voltages");
        }

        public void AddDevice(XElement node, string gid, DeviceType type)
        {
            AddDevice(node.Attribute("id").Value,
                node.Attribute("s1node").Value,
                node.Attribute("s2node").Value,
                gid, node.Attribute("name").Value,
                type,
                type == DeviceType.Transformer? double.Parse(node.Attribute("s1baseKV").Value): double.Parse(node.Attribute("baseKV").Value),
                type == DeviceType.Transformer ? double.Parse(node.Attribute("s2baseKV").Value) : double.Parse(node.Attribute("baseKV").Value),
                type == DeviceType.Switch? bool.Parse(node.Attribute("nominalState1").Value) : false,
                type== DeviceType.Line? double.Parse(node.Attribute("length").Value): 0);
        }

        /// <summary>
        /// Trace connectivity downstream from the first connected source
        /// </summary>
        public void DoConnectivity()
        {
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

        }

        private void TraceNodeConnectivity(Device d, Node n)
        {
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
        /// Trace power flow downstream from all sources, assumes no parallel sources
        /// </summary>
        public void DoPowerFlow()
        {
            
            if (Sources.Count == 0)
                return;
            foreach (var source in Sources.Values)
            {
                if (Devices.ContainsKey(source.DeviceId))
                    TraceNodePowerFlow(Devices[source.DeviceId], Devices[source.DeviceId].Node1, source);
            }
            foreach (Device dd in Devices.Values)
            {
                dd.CalculateUpstreamSide();
            }
        }

        private void TraceNodePowerFlow(Device d, Node n, Source s, double distance = 0)
        {
            Node traceNode;
            if (!d.SP2S.ContainsKey(s))
                d.SP2S.Add(s, new PFDetail());
            d.SP2SMark = true;

            if (d.Node1 == n)
            {
                if (d.SP2S[s].Node1Mark)
                {
                    if (distance >= d.SP2S[s].Node1Distance)
                        return;
                }
                d.SP2S[s].Node1Mark = true;
                d.SP2S[s].Node1Distance = distance;
                traceNode = d.Node2;
            }
            else
            {
                if (d.SP2S[s].Node2Mark)
                {
                    if (distance >= d.SP2S[s].Node2Distance)
                        return;
                }
                d.SP2S[s].Node2Mark = true;
                d.SP2S[s].Node2Distance = distance;
                traceNode = d.Node1;
            }
            if (d.SwitchState || d.Type != DeviceType.Switch)
            {
                foreach (Device dd in traceNode.Devices)
                {
                    if (dd != d)
                    {
                        TraceNodePowerFlow(dd, traceNode, s, distance + d.Length);
                    }
                }
            }
        }

        public int GetDisconnectedCount()
        {
            var devices = from d in Devices.Values where !d.ConnectivityMark select d;
            return devices.Count();
        }

        public int GetDeenergizedCount()
        {
            var devices = from d in Devices.Values where !d.SP2SMark select d;
            return devices.Count();
        }

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

        public int GetUpstreamSideByName(string name)
        {
            var d = (from i in Devices.Values where i.Name == name select i).FirstOrDefault();
            if (d == null)
                return 0;
            else
                return d.Upstream;
        }

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

        public void Serialize(string file)
        {
            try
            {
                Util.SerializeMessagePack(file, this);
            }
            catch (Exception ex)
            {
                Fatal($"Failed to serialize model to file [{file}]. {ex.Message}");
            }
        }
        

        public static NodeModel Deserialize(string file)
        {
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

        private void RebuildNodeReferences()
        {
            foreach (var device in Devices.Values)
            {
                device.Node1.Devices.Add(device);
                device.Node2.Devices.Add(device);
            }
        }

    }
}
