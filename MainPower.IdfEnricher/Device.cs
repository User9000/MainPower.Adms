using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        [IgnoreMember]
        public bool ConnectivityMark { get; set; }

        [JsonIgnore]
        [IgnoreMember]
        public bool SP2SMark { get; set; }

        [JsonIgnore]
        [IgnoreMember]
        public int Upstream { get; set; }

        [JsonIgnore]
        [IgnoreMember]
        public Source ClosestUpstreamSource { get; set; }

        [Key(4)]
        public DeviceType Type { get; set; }

        //true for closed, false for open
        [Key(5)]
        public bool SwitchState { get; set; }

        [Key(6)]
        public string Name { get; set; }

        [Key(7)]
        public double Length { get; set; } = 0;

        [JsonIgnore]
        [IgnoreMember]
        public Dictionary<Source, PFDetail> SP2S { get; set; } = new Dictionary<Source, PFDetail>();

        public void CalculateUpstreamSide()
        {
            double d = double.MaxValue;
            foreach (var kvp in SP2S)
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
            foreach (var kvp in SP2S)
            {
                Console.WriteLine($"\tSource [{kvp.Key.Name}]: Node1 distance:{kvp.Value.Node1Distance} Node2 distance:{kvp.Value.Node2Distance}");
            }
            Console.WriteLine($"\tClosest upstream source is {ClosestUpstreamSource?.Name} on side {Upstream}");
        }
    }

   
}
