using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    internal class Device
    {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }
        public string Id { get; set; }
        public string GroupId { get; set; }
        public bool ConnectivityMark { get; set; }
        public bool PFMark { get; set; }
        public int Upstream { get; set; }
        public Source ClosestUpstreamSource { get; set; }
        public DeviceType Type { get; set; }
        //true for closed, false for open
        public bool SwitchState { get; set; }
        public string Name { get; set; }
        public double Length { get; set; } = 0;
        public Dictionary<Source, PFDetail> PF { get; set; } = new Dictionary<Source, PFDetail>();

        public void CalculateUpstreamSide()
        {
            double d = double.MaxValue;
            foreach (var kvp in PF)
            {
                if (kvp.Value.Node1Distance < d)
                {
                    d = kvp.Value.Node1Distance;
                    Upstream = 1;
                    ClosestUpstreamSource = kvp.Key;
                }
                if (kvp.Value.Node2Distance < d)
                {
                    d = kvp.Value.Node2Distance;
                    Upstream = 2;
                    ClosestUpstreamSource = kvp.Key;
                }
            }
        }

        public void PrintPFResults()
        {
            Console.WriteLine($"Power flow results for device [{Name}]:");
            foreach (var kvp in PF)
            {
                Console.WriteLine($"\tSource [{kvp.Key.Name}]: Node1 distance:{kvp.Value.Node1Distance} Node2 distance:{kvp.Value.Node2Distance}");
            }
            Console.WriteLine($"\tClosest upstream source is {ClosestUpstreamSource?.Name} on side {Upstream}");
        }
    }

    internal enum DeviceType
    {
        Line,
        Switch,
        Transformer,
        Load,
        Regulator,
    }
}
