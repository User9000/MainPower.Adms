using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MainPower.Adms.ScadaConverter
{
    public class SCD5200TagReader
    {
        private readonly string _config;
        private string[] _configData;
        private Dictionary<string, string> _tagDeviceMap = new Dictionary<string, string>();
        private string _rtuName;
        private bool _scd6000 = false;

        public SCD5200TagReader(string filename)
        {
            _scd6000 = filename.EndsWith("_scd6000.cfg");
            _config = filename;
            _configData = File.ReadAllLines(_config);
            _rtuName = _configData[0].Trim(new char[] { '"', '1', ' ' });
        }

        public void ProcessTags(DataTable dt)
        {
            for (int i = 0; i < _configData.Length; i++)
            {
                string line = _configData[i];
                if (line.StartsWith("29 "))
                {
                    string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length == 3 || items.Length == 4)
                    {
                        int offset =  26;//offset for an ethernet config
                        string isiteth = _configData[i + 3];
                        if (isiteth.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 2)
                        {
                            //we have serial
                            offset = _scd6000 ? 25 : 22;
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
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "DI";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[0]; j < itemcounts2[1]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "DO";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[1]; j < itemcounts2[2]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "CO";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[2]; j < itemcounts2[3]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "FC";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[3]; j < itemcounts2[4]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "AI";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[4]; j < itemcounts2[5]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "NS";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        for (int j = itemcounts2[5]; j < itemcounts2[6]; j++)
                        {
                            string[] tag = _configData[j].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var row = dt.NewRow();
                            row["RTUName"] = _rtuName;
                            row["TagName"] = tag[8].Trim(new char[] { '"' });
                            row["Type"] = "AO";
                            row["Index"] = int.Parse(tag[0]);
                            row["RelayName"] = "";
                            row["RelayAddress"] = "";
                            row["ScaleFactor"] = "";
                            row["DeviceIndex"] = LookupTagDevice(row["TagName"] as string);
                            dt.Rows.Add(row);
                        }
                        break;
                    }
                }
            }
        }

        private object LookupTagDevice(string name)
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
            return "RTU";
        }

        public void ReadIeds()
        {
            //Note: This is real hacky.  If we start to want too much more detail it 
            //would probably be better to invest the time to create a full config parser.

            //Stage 1: Hunt for Modbus devices
            Regex r1 = new Regex("^1\r\n[A-Z0-9a-z]{1,3} 0 \"([^\"]*)\"", RegexOptions.Multiline);
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
            bool eth = _configData[v + 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length == 3;
            Regex r2 = new Regex("\"([^\"]*)\"");
            int offset = eth ? 3 : 2;

            for (int i = 0; i < 5; i++)
            {
                string line = _configData[v + offset + i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' });

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    //Console.WriteLine($"{tag}");
                    _tagDeviceMap.Add(tag.ToLower(), $"{_rtuName}.{device}");
                }

            }

            for (int i = v + 15 + offset; i < _configData.Length; i++)
            {
                if (_configData[i].Split().Length < 7)
                    break;

                string line = _configData[i];
                foreach (Match m in r2.Matches(line))
                {
                    string tag = m.Value.Trim(new char[] { '"' });

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        //Console.WriteLine($"{tag}");
                        _tagDeviceMap.Add(tag.ToLower(), $"{_rtuName}.{device}");
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
            //Console.WriteLine($"Parsing Ethernet Dnp Device {_rtuName}.{deviceName}...");
            ParseDnpDevicePoints(v + 8, deviceName);

        }

        private void ParseDnpSerialDevice(int v)
        {
            string deviceName = _configData[v + 1].Trim(new char[] { '"' });
            //Console.WriteLine($"Parsing Serial Dnp Device {_rtuName}.{deviceName}...");
            ParseDnpDevicePoints(v + 5, deviceName);
        }

        private void ParseDnpDevicePoints(int v, string device)
        {
            Regex r1 = new Regex("^[0-9]+");
            Regex r2 = new Regex("\"([^\"]*)\"");

            for (int i = 0; i <= 9; i++)
            {
                string line = _configData[v + i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' });

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    //Console.WriteLine($"{tag}");
                    _tagDeviceMap.Add(tag.ToLower(), $"{_rtuName}.{device}");
                }
            }
            int offset = _scd6000 ? 34 : 15;
            string[] data = _configData[v + offset].Trim().Split();
            int[] idata = new int[data.Length];
            int totaltags = 0;

            for (int i = 0; i < data.Length; i++)
            {
                idata[i] = int.Parse(data[i]);
                totaltags += idata[i];
            }
            totaltags -= idata[0]; //the first one is the offset to start of data

            for (int i = v + offset + 1 + idata[0]; i < v + offset + 1 + idata[0] + totaltags; i++)
            {
                string line = _configData[i];
                string tag = r2.Match(line).Value.Trim(new char[] { '"' }); ;

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    //Console.WriteLine($"{tag}");
                    _tagDeviceMap.Add(tag.ToLower(), $"{_rtuName}.{device}");
                }
            }
        }

        public static void GenerateRTUTagInfo(string path = @".\")
        {
            var overrideFile = path + "_equipmentoverrides.csv";
            DataTable dt = new DataTable();
            dt.Columns.Add("RTUName");
            dt.Columns.Add("TagName");
            dt.Columns.Add("Type");
            dt.Columns.Add("Index");
            dt.Columns.Add("RelayName");
            dt.Columns.Add("RelayAddress");
            dt.Columns.Add("ScaleFactor");
            dt.Columns.Add("DeviceIndex");

            var files = Directory.GetFiles(path + @"rtu\", "*.cfg");
            foreach (var file in files)
            {
                SCD5200TagReader rtu = new SCD5200TagReader(file);
                rtu.ReadIeds();
                rtu.ProcessTags(dt);
            }

            //override the equipment from the equipment map
            Dictionary<string, string> overrides = new Dictionary<string, string>();
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(overrideFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                while (csv.ReadNextRecord())
                {
                    overrides.Add(csv["OldEquipment"], csv["NewEquipment"]);
                }
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string tagName = dt.Rows[i]["TagName"] as string;
                string equipment = dt.Rows[i]["DeviceIndex"] as string;

                if (overrides.ContainsKey(equipment))
                    dt.Rows[i]["RelayName"] = overrides[equipment];
                else
                    //TODO: won't need this after sorting out deviceindexdebarcle
                    dt.Rows[i]["RelayName"] = equipment;
            }

            Util.ExportDatatable(dt, path + "_rtutaginfo.csv");
        }
    }
}
             
             
             