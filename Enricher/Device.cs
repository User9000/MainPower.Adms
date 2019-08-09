using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MainPower.Osi.Enricher
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
        public Node UpstreamNode
        {
            get
            {
                if (Upstream == 1)
                    return Node1;
                else if (Upstream == 2)
                    return Node2;
                else return null;
            }
        }

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

        [Key(8)]
        public double Base1kV { get; set; }

        [Key(9)]
        public double Base2kV { get; set; }

        /// <summary>
        /// Phase shift of transformer or regulator, in clock units
        /// </summary>
        [Key(10)]
        public int PhaseShift { get; set; }

        /// <summary>
        /// Phase Identifiers
        /// </summary>
        [Key(11)]
        public short[,] PhaseID { get; set; } = new short[2,3];


        [JsonIgnore]
        [IgnoreMember]
        public Dictionary<Source, PFDetail> SP2S { get; set; } = new Dictionary<Source, PFDetail>();

        /// <summary>
        /// Calculates the upstream side of the device, based on the shorted path to source calculations
        /// </summary>
        public void CalculateUpstreamSide()
        {
            double d = double.MaxValue;
            foreach (var kvp in SP2S)
            {
                if (kvp.Value.N1ExtDistance < d)
                {
                    d = kvp.Value.N1ExtDistance;
                    Upstream = 1;
                    ClosestUpstreamSource = kvp.Key;
                }
                if (kvp.Value.N2ExtDistance < d)
                {
                    d = kvp.Value.N2ExtDistance;
                    Upstream = 2;
                    ClosestUpstreamSource = kvp.Key;
                }
            }
        }

        /// <summary>
        /// Prints the results of the shortest path to source calculations
        /// </summary>
        public void PrintPFResults()
        {
            Console.WriteLine($"Power flow results for device [{Name}]:");
            foreach (var kvp in SP2S)
            {
                Console.WriteLine($"\tSource [{kvp.Key.Name}]: N1IntDistance:{kvp.Value.N1IntDistance} N1ExtDistance:{kvp.Value.N1ExtDistance} N2IntDistance:{kvp.Value.N2IntDistance} N2ExtDistance:{kvp.Value.N2ExtDistance}");
            }
            Console.WriteLine($"\tClosest upstream source is {ClosestUpstreamSource?.Name} on side {Upstream}");
        }

        /// <summary>
        /// Returns all devices that are upstream of this device
        /// </summary>
        /// <returns>A List of Devices</returns>
        public List<Device> GetUpstreamDevices()
        {
            List<Device> result = new List<Device>();
            var devices = UpstreamNode?.Devices;
            if (devices != null)
            {
                foreach (var device in devices)
                {
                    //only return devices that are upstream
                    if (device.UpstreamNode != UpstreamNode && device != this)
                        result.Add(device);
                }
            }
            return result;
        }
    }

   
}
