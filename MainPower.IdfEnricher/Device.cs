using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    [Serializable]
    [MessagePackObject]
    public class Device
    {
        [Key(0)]
        public Node Node1 { get; set; }
        [Key(1)]
        public Node Node2 { get; set; }
        [Key(2)]
        public string Id { get; set; }
        [Key(3)]
        public string GroupId { get; set; }
        [JsonIgnore]
        [Key(4)]
        public bool ConnectivityMark { get; set; }
        [JsonIgnore]
        [Key(5)]
        public bool PFMark { get; set; }
        [JsonIgnore]
        [Key(6)]
        public int Upstream { get; set; }
        [JsonIgnore]
        [Key(7)]
        public Source ClosestUpstreamSource { get; set; }
        [Key(8)]
        public DeviceType Type { get; set; }
        //true for closed, false for open
        [Key(9)]
        public bool SwitchState { get; set; }
        [Key(10)]
        public string Name { get; set; }
        [Key(11)]
        public double Length { get; set; } = 0;
        [JsonIgnore]
        [Key(12)]
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

    public enum DeviceType
    {
        Line,
        Switch,
        Transformer,
        Load,
        Regulator,
    }
}
