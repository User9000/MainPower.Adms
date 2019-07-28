using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class NodeModel
    {
        public Dictionary<string, Node> Nodes = new Dictionary<string, Node>();
        public Dictionary<string, Device> Devices = new Dictionary<string, Device>();
        public Dictionary<string, Source> Sources = new Dictionary<string, Source>();

        private void CleanOrphanNodes()
        {
            var nodes = from n in Nodes.Values where n.Devices.Count == 0 select n;
            foreach (var node in nodes)
            {
                Nodes.Remove(node.Id);
            }
        }

        private void RemoveDevice(Device d)
        {
            d.Node1.Devices.Remove(d);
            d.Node2.Devices.Remove(d);
            Devices.Remove(d.Id);
        }

        public void AddDevice(string id, string n1id, string n2id, string gid, string name, DeviceType type, bool state = false, double length = 0)
        {
            if (Devices.ContainsKey(id))
            {
                //error
                return;
            }
            lock (Devices)
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
                    Length = length
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

        public void AddDevice(XElement node, string gid, DeviceType type)
        {
            AddDevice(node.Attribute("id").Value, node.Attribute("s1node").Value, node.Attribute("s2node").Value, gid, node.Attribute("name").Value, type, type == DeviceType.Switch? bool.Parse(node.Attribute("nominalState1").Value) : false, type== DeviceType.Line? double.Parse(node.Attribute("length").Value): 0);
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
            if (!d.PF.ContainsKey(s))
                d.PF.Add(s, new PFDetail());
            d.PFMark = true;

            if (d.Node1 == n)
            {
                if (d.PF[s].Node1Mark)
                {
                    if (distance >= d.PF[s].Node1Distance)
                        return;
                }
                d.PF[s].Node1Mark = true;
                d.PF[s].Node1Distance = distance;
                traceNode = d.Node2;
            }
            else
            {
                if (d.PF[s].Node2Mark)
                {
                    if (distance >= d.PF[s].Node2Distance)
                        return;
                }
                d.PF[s].Node2Mark = true;
                d.PF[s].Node2Distance = distance;
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
            var devices = from d in Devices.Values where !d.PFMark select d;
            return devices.Count();
        }

        public void RemoveGroup(string id)
        {
            var devices = from d in Devices.Values where d.GroupId == id select d;
            foreach (Device d in devices)
            {
                RemoveDevice(d);
            }
            var sources = from s in Sources.Values where s.GroupId == id select s;
            foreach (Source s in sources)
            {
                Sources.Remove(s.Id);
            }
            CleanOrphanNodes();
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
    }
}
