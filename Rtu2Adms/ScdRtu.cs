using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MainPower.Adms.Rtu2Adms
{
    public class RtuPoint : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public PointType Type { get; set; }
        public int Index { get; set; }
        public Device SourceDevice { get; set; }
        public int SourceIndex { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum PointType
    {
        DI,
        DO,
        AI,
        AO,
        CO,
        NS,
        FC
    }

    public enum DeviceType
    {
        DnpSlaveSerial,
        DnpSlaveEthernet,
        DnpSerialIed,
        DnpEthernetIed,
        ModbusSerialIed,
        ModbusEthernetIed,
        Rtu,
    }

    public class Device : INotifyPropertyChanged
    {
        public DeviceType Type { get; set; }
        public string Name { get; set; }
        public ObservableCollection<RtuPoint> Points { get; private set; } = new ObservableCollection<RtuPoint>();

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ScdRtu  : INotifyPropertyChanged
    {
        public ObservableCollection<Device> DnpSlaves { get; set; } = new ObservableCollection<Device>();
        public ObservableCollection<Device> Ieds { get; set; } = new ObservableCollection<Device>();
        public string RtuName { get; private set; }
        public string RtuConfigFile { get; private set; }
        
        private string[] _configData;

        private Dictionary<string, Device> _tagDeviceMap = new Dictionary<string, Device>();

        private static readonly Device Rtu = new Device() { Name = "RTU", Type = DeviceType.Rtu };

        public event PropertyChangedEventHandler PropertyChanged;

        public ScdRtu(string filename)
        {
            RtuConfigFile = filename;
            _configData = File.ReadAllLines(RtuConfigFile);
            RtuName = _configData[0].Trim(new char[] { '"', '1', ' ' });
            ReadIeds();
            ProcessTags();
        }

        private void ProcessTags()
        {
            for (int i = 0; i < _configData.Length; i++)
            {
                string line = _configData[i];
                if (line.StartsWith("29 "))
                {
                    string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length == 3 || items.Length == 4)
                    {
                        Device device = new Device();
                        DnpSlaves.Add(device);
                        device.Type = DeviceType.DnpSlaveEthernet;
                        int offset = 26;//offset for an ethernet config
                        string isiteth = _configData[i + 3];
                        if (isiteth.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 2)
                        {
                            //we have serial
                            offset = 22;
                            device.Type = DeviceType.DnpSlaveSerial;
                        }
                        string[] itemcounts1 = _configData[i + offset].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        int[] itemcounts2 = new int[itemcounts1.Length];

                        itemcounts2[0] = int.Parse(itemcounts1[0]);
                        itemcounts2[1] = int.Parse(itemcounts1[1]);
                        itemcounts2[2] = int.Parse(itemcounts1[2]);
                        itemcounts2[3] = int.Parse(itemcounts1[3]);
                        itemcounts2[4] = int.Parse(itemcounts1[4]);
                        itemcounts2[5] = int.Parse(itemcounts1[5]);
                        itemcounts2[6] = int.Parse(itemcounts1[6]);

                        itemcounts2[0] += i + offset + 1;
                        itemcounts2[1] += itemcounts2[0];
                        itemcounts2[2] += itemcounts2[1];
                        itemcounts2[3] += itemcounts2[2];
                        itemcounts2[4] += itemcounts2[3];
                        itemcounts2[5] += itemcounts2[4];
                        itemcounts2[6] += itemcounts2[5];


                        for (int j = i + offset + 1; j < itemcounts2[0]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);                         
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.DI;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[0]; j < itemcounts2[1]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);                         
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.DO;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[1]; j < itemcounts2[2]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.CO;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[2]; j < itemcounts2[3]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.FC;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[3]; j < itemcounts2[4]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.AI;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[4]; j < itemcounts2[5]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.NS;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        for (int j = itemcounts2[5]; j < itemcounts2[6]; j++)
                        {
                            RtuPoint p = new RtuPoint();
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            p.Name = tag[8].Trim(new char[] { '"' });
                            p.Type = PointType.AO;
                            p.Index = int.Parse(tag[0]);
                            p.SourceDevice = LookupTagDevice(p.Name);
                            device.Points.Add(p);
                        }
                        break;
                    }
                }
            }
        }

        private Device LookupTagDevice(string name)
        {
            List<string> names = new List<string>();
            names.Add(name);
            if (name.Contains("Good_Count_A"))
            {
                names.Add(name.Replace("Good_Count_A", "Good_Count_Raw"));
                names.Add(name.Replace("Good_Count_A", "Good_Raw"));
                names.Add(name.Replace("Good_Count_A", "GCount_Raw")); 
            }
            if (name.Contains("Good_Cnt_A"))
            {
                names.Add(name.Replace("Good_Cnt_A", "Good_Count_Raw"));
                names.Add(name.Replace("Good_Cnt_A", "Good_Raw"));
                names.Add(name.Replace("Good_Cnt_A", "GCount_Raw"));
            }
            if (name.Contains("Bad_Count_A"))
            {
                names.Add(name.Replace("Bad_Count_A", "Bad_Count_Raw"));
                names.Add(name.Replace("Bad_Count_A", "Bad_Raw"));
                names.Add(name.Replace("Bad_Count_A", "BCount_Raw"));
            }
            if (name.Contains("Bad_Cnt_A"))
            {
                names.Add(name.Replace("Bad_Cnt_A", "Bad_Count_Raw"));
                names.Add(name.Replace("Bad_Cnt_A", "Bad_Raw"));
                names.Add(name.Replace("Bad_Cnt_A", "BCount_Raw"));
            }
            foreach (var n in names)
            {
                if (_tagDeviceMap.ContainsKey(n.ToLower()))
                    return _tagDeviceMap[n.ToLower()];
            }
            return Rtu;
        }

        private void ReadIeds()
        {
            //Note: This is real hacky.  If we start to want too much more detail it 
            //would probably be better to invest the time to create a full config parser.

            //Stage 1: Hunt for Modbus devices
            Regex r1 = new Regex("^1\r\n[A-Z0-9a-z] 0 \"([^\"]*)\"", RegexOptions.Multiline);
            Regex r2 = new Regex("\"([^\"]*)\"");
            var m1 = r1.Matches(string.Join("\r\n", _configData));
            var modbus = new Dictionary<string, string>();
            foreach (Match m in m1)
            {
                modbus.Add(r2.Match(m.Value).Value.Trim(new char[] { '"' }), m.Value.Substring(3));
            }

            //Stage 2: Hunt for DNP3 devices
            for (int i = 0; i < _configData.Length; i++)
            {
                if (_configData[i].Trim() == "0 0 0 0 0 0 52097")
                {
                    //we have a dnp device
                    ParseDnpDevice(i);
                }
                else
                {
                    foreach (var j in modbus)
                    {
                        if (_configData[i] == j.Value)
                            ParseModbusDevice(i, j.Key);
                    }
                }
            }
        }

        private void ParseModbusDevice(int v, string device)
        {
            Device d = new Device()
            {
                Name = device,
                Type = DeviceType.ModbusSerialIed
            };
            Ieds.Add(d);
            Regex r2 = new Regex("\"([^\"]*)\"");

            for (int i = 0; i < 5; i++)
            {
                string line = _configData[v + 2 + i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' });

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    _tagDeviceMap.Add(tag.ToLower(), d);
                }
            }

            for (int i = v + 17; i < _configData.Length; i++)
            {
                if (_configData[i].Split().Length < 7)
                    break;

                string line = _configData[i];
                foreach (Match m in r2.Matches(line))
                {
                    string tag = m.Value.Trim(new char[] { '"' });

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        _tagDeviceMap.Add(tag.ToLower(), d);
                    }
                }
            }
        }

        private void ParseDnpDevice(int v)
        {
            if (_configData[v - 2].Split(new char[] { ' ' }).Length > 1)
                ParseDnpSerialDevice(v - 2);
            else
                ParseDnpEthernetDevice(v - 5);
        }

        private void ParseDnpEthernetDevice(int v)
        {
            string deviceName = _configData[v + 4].Trim(new char[] { '"' });
            Device d = new Device()
            {
                Name = deviceName,
                Type = DeviceType.DnpSlaveEthernet
            };
            Ieds.Add(d);
            ParseDnpDevicePoints(v + 8, d);

        }

        private void ParseDnpSerialDevice(int v)
        {
            string deviceName = _configData[v + 1].Trim(new char[] { '"' });
            Device d = new Device()
            {
                Name = deviceName,
                Type = DeviceType.DnpSlaveSerial
            };
            Ieds.Add(d);
            //Console.WriteLine($"Parsing Serial Dnp Device {_rtuName}.{deviceName}...");
            ParseDnpDevicePoints(v + 5, d);
        }

        private void ParseDnpDevicePoints(int v, Device device)
        {
            Regex r1 = new Regex("^[0-9]+");
            Regex r2 = new Regex("\"([^\"]*)\"");

            for (int i = 0; i <= 9; i++)
            {
                string line = _configData[v + i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' });

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    _tagDeviceMap.Add(tag.ToLower(), device);
                }
            }

            string[] data = _configData[v + 15].Trim().Split();
            int[] idata = new int[data.Length];
            int totaltags = 0;

            for (int i = 0; i < data.Length; i++)
            {
                idata[i] = int.Parse(data[i]);
                totaltags += idata[i];
            }
            totaltags -= idata[0]; //the first one is the offset to start of data

            for (int i = v + 16 + idata[0]; i < v + 16 + idata[0] + totaltags; i++)
            {
                string line = _configData[i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' }); ;

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    //Console.WriteLine($"{tag}");
                    _tagDeviceMap.Add(tag.ToLower(), device);
                }
            }
        }

    }
}
             
             
             