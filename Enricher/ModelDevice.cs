using EGIS.ShapeFileLib;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public class ModelDevice : ErrorReporter
    {
        [Key(0)]
        public ModelNode Node1 { get; set; }

        [Key(1)]
        public ModelNode Node2 { get; set; }

        [Key(2)]
        public string Id { get; set; }

        [Key(3)]
        public string GroupId { get; set; }

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
        public short PhaseShift { get; set; }

        /// <summary>
        /// Phase Identifiers
        /// </summary>
        [Key(11)]
        public short[,] PhaseID { get; set; } = new short[2, 3];

        /// <summary>
        /// The Geometry of the device
        /// </summary>
        [Key(12)]
        public List<Point> Geometry { get; set; } = new List<Point>();

        [Key(13)]
        public string SymbolName { get; set; }
    
        [Key(14)]
        public bool Internals { get; set; }

        [Key(15)]
        public string Color { get; set; }

        [Key(16)]
        public double NominalkVA { get; set; }

        /// <summary>
        /// Phasing (in clock units) of phase index 1
        /// </summary>
        [IgnoreMember]
        public short[] Phasing { get; set; } = new short[2];

        [IgnoreMember]
        public short? CalculatedPhaseShift { get; set; }

        [IgnoreMember]
        public bool[] Energization { get; set; } = new bool[2];

        [IgnoreMember]
        public bool Connectivity { get; set; }

        [IgnoreMember]
        public int Upstream { get; set; }
        [IgnoreMember]
        public int Downstream
        {
            get
            {
                return Upstream == 0 ? 0 : Upstream == 1 ? 2 : 1;
            }
        }

        [IgnoreMember]
        public ModelNode UpstreamNode
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

        [IgnoreMember]
        public ModelSource ClosestUpstreamSource { get; set; }
      
        [IgnoreMember]
        public bool Trace { get; set;} = false;

        [IgnoreMember]
        public ModelFeeder NominalFeeder { get; set; } = null;

        [IgnoreMember]
        public Dictionary<ModelSource, PFDetail> SP2S { get; set; } = new Dictionary<ModelSource, PFDetail>();

        [IgnoreMember]
        public IdfElement IdfDevice { get; set; } = null;


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
        public List<ModelDevice> GetUpstreamDevices()
        {
            List<ModelDevice> result = new List<ModelDevice>();
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

        public void CalculatePhaseShift()
        {
            short? s1 = null;
            short? s2 = null;
            foreach (var dpf in SP2S)
            {
                if (!dpf.Value.N1IntDistance.Equals(double.NaN))
                {
                    if (s1.HasValue)
                    {
                        if (s1 != dpf.Value.Phasing[0])
                        {
                            Warn("Device has inconsistent phasing between parallel sources", Id, Name);
                        }
                    }
                    else
                    {
                        s1 = dpf.Value.Phasing[0];
                    }
                }
                if (!dpf.Value.N2IntDistance.Equals(double.NaN))
                {
                    if (s2.HasValue)
                    {
                        if (s2 != dpf.Value.Phasing[1])
                        {
                            Warn("Device has inconsistent phasing between parallel sources", Id, Name);
                        }
                    }
                    else
                    {
                        s2 = dpf.Value.Phasing[1];
                    }
                }
            }
            if (s1 == null || s2 == null)
                CalculatedPhaseShift = null;
            else
                CalculatedPhaseShift = (short)((s1 - s2) % 12);
            Phasing[0] = s1 ?? 0;
            Phasing[1] = s2 ?? 0;
        }
    }

   
}
