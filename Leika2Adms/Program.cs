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
using CommandLine;
using System.Diagnostics;

namespace MainPower.Adms.Leika2Adms
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = Parser.Default.ParseArguments<Options>(args)
               .WithParsed(o =>
               {
                   try
                   {
                       //load the conductor table
                       DataTable conductor = GetDataTableFromCsv(o.Conductors, true);

                       //create the skeleton of the output xml file
                       XDocument doc = new XDocument();
                       XElement data = new XElement("data", new XAttribute("type", "Electric Distribution"), new XAttribute("timestamp", "TODO"), new XAttribute("format", "1.0"));
                       XElement groups = new XElement("groups");
                       doc.Add(data);
                       data.Add(groups);
                       XElement xgroup = new XElement("group", new XAttribute("id", "Line Types"));
                       groups.Add(xgroup);

                       var dict = new HashSet<string>();

                       //loop through all the entries in the csv file and convert them to IDF
                       foreach (DataRow row in conductor.Rows)
                       {
                           try
                           {
                               if (row["ADMS"] as string == "TRUE")
                               {
                                   string id = row["ID"] as string;
                                   string name = row["Name"] as string;
                                   string voltage = row["Voltage"] as string;
                                   string type = row["Type"] as string;
                                   string phases = row["Phases"] as string;
                                   string leika = row["Leika"] as string;
                                   if (type.EndsWith("OH"))
                                       type = "Overhead";
                                   else if (type.EndsWith("UG"))
                                       type = "Underground";
                                   else
                                       type = "Busbar";

                                   if (dict.Contains(id) || string.IsNullOrWhiteSpace(id))
                                   {
                                       continue;
                                   }
                                   else
                                   {
                                       dict.Add(id);
                                   }

                                   Console.WriteLine($"Processing Leika model {leika}");
                                   LineParameters? p = GetLeikaData(leika, o.LeikaPath, int.Parse(phases));

                                   if (p != null)
                                   {
                                       //for each phasing combination we need to create a different line type
                                       List<(string id, string name, LineParameters p)> phasecombos = new List<(string, string, LineParameters)>();
                                       if (phases == "3")
                                           phasecombos.Add((id.Replace("lType_", "lType_ABC_"), $"ABC-{name}", p.Value));
                                       else if (phases == "2")
                                       {
                                           phasecombos.Add((id.Replace("lType_", "lType_AB_"), $"AB-{name}", p.Value));
                                           phasecombos.Add((id.Replace("lType_", "lType_BC_"), $"BC-{name}", SwapIndexes(p.Value, 0, 2)));
                                           phasecombos.Add((id.Replace("lType_", "lType_AC_"), $"AC-{name}", SwapIndexes(p.Value, 1, 2)));
                                       }
                                       else if (phases == "1")
                                       {
                                           phasecombos.Add((id.Replace("lType_", "lType_A_"), $"A-{name}", p.Value));
                                           phasecombos.Add((id.Replace("lType_", "lType_B_"), $"B-{name}", SwapIndexes(p.Value, 0, 1)));
                                           phasecombos.Add((id.Replace("lType_", "lType_C_"), $"C-{name}", SwapIndexes(p.Value, 0, 2)));
                                       }
                                       //write the phase combinations to xml/idf
                                       foreach (var combo in phasecombos)
                                       {
                                           XElement element = new XElement("element",
                                               new XAttribute("type", "Line Type"),
                                               new XAttribute("id", combo.id),
                                               new XAttribute("name", combo.name),
                                               new XAttribute("lineType", type),
                                               new XAttribute("calcMode", "None"),
                                               new XAttribute("chargingBase", voltage),
                                               new XAttribute("reactance1-1", combo.p.SelfImpedance[0].Reactance),
                                               new XAttribute("reactance1-2", combo.p.MutualImpedance[0].Reactance),
                                               new XAttribute("reactance1-3", combo.p.MutualImpedance[1].Reactance),
                                               new XAttribute("reactance2-2", combo.p.SelfImpedance[1].Reactance),
                                               new XAttribute("reactance2-3", combo.p.MutualImpedance[2].Reactance),
                                               new XAttribute("reactance3-3", combo.p.SelfImpedance[2].Reactance),
                                               new XAttribute("resistance1-1", combo.p.SelfImpedance[0].Resistance),
                                               new XAttribute("resistance1-2", combo.p.MutualImpedance[0].Resistance),
                                               new XAttribute("resistance1-3", combo.p.MutualImpedance[1].Resistance),
                                               new XAttribute("resistance2-2", combo.p.SelfImpedance[1].Resistance),
                                               new XAttribute("resistance2-3", combo.p.MutualImpedance[2].Resistance),
                                               new XAttribute("resistance3-3", combo.p.SelfImpedance[2].Resistance));
                                           xgroup.Add(element);
                                       }
                                   }
                                   else
                                   {
                                       Console.WriteLine($"Could not get line parameters for model {leika}");
                                   }
                               }
                           }
                           catch (Exception ex)
                           {
                               Console.WriteLine(ex.ToString());
                           }
                       }
                       doc.Save(o.Output);
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                   }
               });
         }

        /// <summary>
        /// Swaps the indexes of a line parameter set
        /// </summary>
        /// <param name="p"></param>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Looks for a leika file on the path, and tries to extract the impedance data from it
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static LineParameters? GetLeikaData(string fileName, string path, int phases)
        {
            try
            {
                if (phases < 1 || phases > 3)
                {
                    Console.WriteLine($"Invalid number of phases for file {fileName}");
                    return null;
                }
                    //get all the report files
                var files = Directory.GetFiles(path, "*.txt");

                //what is this?
                //TODO: just get the file we need
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
                            Impedance zero = new Impedance { Reactance = 0, Resistance = 0 };
                            LineParameters p = new LineParameters();
                            //TODO: we should be handling the per phase cases here
                            p.Initialize();
                            p.SelfImpedance[0] = GetImpedance("1L1", lines[i + 9]);
                            if (phases >= 2)
                                p.SelfImpedance[1] = GetImpedance("1L2", lines[i + 9]);
                            else
                                p.SelfImpedance[1] = zero;
                            if (phases == 3)
                                p.SelfImpedance[2] = GetImpedance("1L3", lines[i + 9]);
                            else
                                p.SelfImpedance[2] = zero;
                            if (phases >= 2)
                                p.MutualImpedance[0] = GetImpedance("1L1-1L2", lines[i + 17]);
                            else
                                p.MutualImpedance[0] = zero;
                            if (phases == 3)
                            {
                                p.MutualImpedance[1] = GetImpedance("1L1-1L3", lines[i + 18]);
                                p.MutualImpedance[2] = GetImpedance("1L2-1L3", lines[i + 18]);
                            }
                            else
                            {
                                p.MutualImpedance[1] = zero;
                                p.MutualImpedance[2] = zero;
                            }
                            return p;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"File {fileName}.txt was not found on the leika path");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Parses an impedance from a line in the report file
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Impedance GetImpedance(string marker, string text)
        {
            int index = text.IndexOf(marker);
            if (index == -1)
                return new Impedance { Reactance = 0, Resistance = 0 };
            int start = index + marker.Length;
            string substr = text.Substring(start);
            Regex r = new Regex("(-?)(0|([1-9][0-9]*))(\\.[0-9]+)?");
            var matches = r.Matches(substr);
            if (matches.Count < 2)
            {
                throw new Exception($"Failed to find impedance [{marker}] in string [{text}]");
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
