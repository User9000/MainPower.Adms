using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Leika2Adms
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Users\hsc\Downloads\adms\enricher\data\Leika";
            string conductorFile = @"C:\Users\hsc\Downloads\adms\enricher\data\Conductors.csv";
            string outputFile = @"C:\Users\hsc\Downloads\adms\enricher\data\Conductors.xml";
            try
            {
                //load the conductor table
                DataTable conductor = GetDataTableFromCsv(conductorFile, true);

                XDocument doc = new XDocument();
                XElement data = new XElement("data", new XAttribute("type", "Electric Distribution"), new XAttribute("timestamp", "TODO"), new XAttribute("format", "1.0"));
                XElement groups = new XElement("groups");
                doc.Add(data);
                data.Add(groups);
                XElement xgroup = new XElement("group", new XAttribute("id", "Line Types"));
                groups.Add(xgroup);

                foreach (DataRow row in conductor.Rows)
                {
                    if (row["ADMS"] as string == "TRUE")
                    {
                        string id = row["ID"] as string;
                        string name = row["Name"] as string;
                        string voltage = row["Voltage"] as string;
                        string type = row["Type"] as string;
                        string phases = row["Phases"] as string;
                        if (type.EndsWith("OH"))
                            type = "Overhead";
                        else if (type.EndsWith("UG"))
                            type = "Underground";
                        else
                            type = "Busbar";

                        LineParameters? p = GetLeikaData(row["Leika"] as string, path);

                        if (p != null)
                        {
                            List<(string, LineParameters)> combos = new List<(string, LineParameters)>();
                            if (phases == "3")
                                combos.Add((id.Replace("lineType_", "lineType_ABC_"), p.Value));
                            else if (phases == "2")
                            {
                                combos.Add((id.Replace("lineType_", "lineType_AB_"), p.Value));
                                combos.Add((id.Replace("lineType_", "lineType_BC_"), SwapIndexes(p.Value, 0, 2)));
                                combos.Add((id.Replace("lineType_", "lineType_AC_"), SwapIndexes(p.Value, 1, 2)));
                            }
                            else if (phases == "1")
                            {
                                combos.Add((id.Replace("lineType_", "lineType_A_"), p.Value));
                                combos.Add((id.Replace("lineType_", "lineType_B_"), SwapIndexes(p.Value, 0, 2)));
                                combos.Add((id.Replace("lineType_", "lineType_C_"), SwapIndexes(p.Value, 1, 2)));
                            }
                            foreach (var p2 in combos)
                            {
                                XElement element = new XElement("element",
                                    new XAttribute("type", "Line Type"),
                                    new XAttribute("id", p2.Item1),
                                    new XAttribute("name", name),
                                    new XAttribute("lineType", type),
                                    new XAttribute("calcMode", "None"),
                                    new XAttribute("chargingBase", voltage),
                                    new XAttribute("reactance1-1", p2.Item2.SelfImpedance[0].Reactance),
                                    new XAttribute("reactance1-2", p2.Item2.MutualImpedance[0].Reactance),
                                    new XAttribute("reactance1-3", p2.Item2.MutualImpedance[1].Reactance),
                                    new XAttribute("reactance2-2", p2.Item2.SelfImpedance[1].Reactance),
                                    new XAttribute("reactance2-3", p2.Item2.MutualImpedance[2].Reactance),
                                    new XAttribute("reactance3-3", p2.Item2.SelfImpedance[2].Reactance),
                                    new XAttribute("resistance1-1", p2.Item2.SelfImpedance[0].Resistance),
                                    new XAttribute("resistance1-2", p2.Item2.MutualImpedance[0].Resistance),
                                    new XAttribute("resistance1-3", p2.Item2.MutualImpedance[1].Resistance),
                                    new XAttribute("resistance2-2", p2.Item2.SelfImpedance[1].Resistance),
                                    new XAttribute("resistance2-3", p2.Item2.MutualImpedance[2].Resistance),
                                    new XAttribute("resistance3-3", p2.Item2.SelfImpedance[2].Resistance));
                                xgroup.Add(element);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Line parameters were null");
                        }
                    }
                }

                doc.Save(outputFile);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey();
         }

        private static LineParameters SwapIndexes(LineParameters p, int i1, int i2)
        {
            LineParameters p2 = new LineParameters();
            p2.Initialize();
            p.MutualImpedance.CopyTo(p2.MutualImpedance, 0);
            p.SelfImpedance.CopyTo(p2.SelfImpedance, 0);

            p2.SelfImpedance[i1] = p.SelfImpedance[i2];
            p2.SelfImpedance[i2] = p.SelfImpedance[i1];

            p2.MutualImpedance[i1] = p.MutualImpedance[i2];
            p2.MutualImpedance[i2] = p.MutualImpedance[i1];

            return p2;
        }

        private static LineParameters? GetLeikaData(string fileName, string path)
        {
            try
            {
                //get all the report files
                var files = Directory.GetFiles(path, "*.txt");

                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = Path.GetFileName(files[i]);
                }
                if (files.Contains(fileName + ".txt"))
                {
                    var lines = File.ReadAllLines(Path.Combine(path, fileName) +".txt");

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("IMPEDANCES AFTER ELIMINATION OF EARTHED CONDUCTORS"))
                        {
                            if (i + 18 > lines.Length)
                            {
                                Console.WriteLine("Was expecting more lines in file");
                                break;
                            }
                            LineParameters p = new LineParameters();
                            p.Initialize();
                            p.SelfImpedance[0] = GetImpedance("1L1", lines[i + 9]);
                            p.SelfImpedance[1] = GetImpedance("1L2", lines[i + 9]);
                            p.SelfImpedance[2] = GetImpedance("1L3", lines[i + 9]);
                            p.MutualImpedance[0] = GetImpedance("1L1-1L2", lines[i + 17]);
                            p.MutualImpedance[1] = GetImpedance("1L1-1L3", lines[i + 18]);
                            p.MutualImpedance[2] = GetImpedance("1L2-1L3", lines[i + 18]);
                           
                            return p;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }


        private static Impedance GetImpedance(string marker, string text)
        {
            int start = text.IndexOf(marker) + marker.Length;
            string substr = text.Substring(start);
            Regex r = new Regex("(-?)(0|([1-9][0-9]*))(\\.[0-9]+)?");
            var matches = r.Matches(substr);
            if (matches.Count < 2)
            {
                //TODO: throw an error here
                Console.WriteLine("Impedance was null");
                return new Impedance();
            }
            Impedance result = new Impedance
            {
                Reactance = double.Parse(matches[1].Value),
                Resistance = double.Parse(matches[0].Value)
            };

            return result;
        }

        /// <summary>
        /// Reads a CSV file and returns a DataTable
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            DataTable csvTable = new DataTable();
            using (CsvReader csvReader =
                new CsvReader(new StreamReader(path), true))
            {
                csvTable.Load(csvReader);
            }
            return csvTable;
        }

        /// <summary>
        /// Export a DataTable to CSV
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="file"></param>
        public static void ExportDatatable(DataTable dt, string file)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                  string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            try
            {
                File.WriteAllText(file, sb.ToString(), Encoding.ASCII);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public struct Impedance
    {
        public double Resistance { get; set; }
        public double Reactance { get; set; }
    }

    public struct LineParameters 
    {
        public Impedance[] SelfImpedance { get; set; }
        public Impedance[] MutualImpedance {get;set; }     

        public void Initialize()
        {
            SelfImpedance = new Impedance[3];
            MutualImpedance = new Impedance[3];
        }
    }

}
