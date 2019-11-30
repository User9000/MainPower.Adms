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
                        if (type.EndsWith("OH"))
                            type = "Overhead";
                        else if (type.EndsWith("UG"))
                            type = "Underground";
                        else
                            type = "Busbar";



                        LineParameters? p = GetLeikaData(row["Leika"] as string, path);

                        if (p != null)
                        {
                            XElement element = new XElement("element",
                                new XAttribute("type", "Line Type"),
                                new XAttribute("id", id),
                                new XAttribute("name", name),
                                new XAttribute("lineType", type),
                                new XAttribute("calcMode", "None"),
                                new XAttribute("chargingBase", voltage),
                                new XAttribute("reactance1-1", p?.S1?.Reactance),
                                new XAttribute("reactance1-2", p?.M12?.Reactance),
                                new XAttribute("reactance1-3", p?.M13?.Reactance),
                                new XAttribute("reactance2-2", p?.S2?.Reactance),
                                new XAttribute("reactance2-3", p?.M23?.Reactance),
                                new XAttribute("reactance3-3", p?.S3?.Reactance),
                                new XAttribute("resistance1-1", p?.S1?.Resistance),
                                new XAttribute("resistance1-2", p?.M12?.Resistance),
                                new XAttribute("resistance1-3", p?.M13?.Resistance),
                                new XAttribute("resistance2-2", p?.S2?.Resistance),
                                new XAttribute("resistance2-3", p?.M23?.Resistance),
                                new XAttribute("resistance3-3", p?.S3?.Resistance));
                            xgroup.Add(element);
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
                            LineParameters p = new LineParameters
                            {
                                S1 = GetImpedance("1L1", lines[i + 9]),
                                S2 = GetImpedance("1L2", lines[i + 9]),
                                S3 = GetImpedance("1L3", lines[i + 9]),
                                M12 = GetImpedance("1L1-1L2", lines[i + 17]),
                                M13 = GetImpedance("1L1-1L3", lines[i + 18]),
                                M23 = GetImpedance("1L2-1L3", lines[i + 18])
                            };

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


        private static Impedance? GetImpedance(string marker, string text)
        {
            int start = text.IndexOf(marker) + marker.Length;
            string substr = text.Substring(start);
            Regex r = new Regex("(-?)(0|([1-9][0-9]*))(\\.[0-9]+)?");
            var matches = r.Matches(substr);
            if (matches.Count < 2)
                return null;
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
        public Impedance? S1 { get; set; }
        public Impedance? S2 { get; set; }
        public Impedance? S3 { get; set; }
        public Impedance? M12 { get; set; }
        public Impedance? M13 { get; set; }
        public Impedance? M23 { get; set; }     
    }

}
