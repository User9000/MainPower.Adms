using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace MainPower.Osi.Enricher
{
    static class Util
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Reads a CSV file and returns a DataTable
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
        internal static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            path = Path.GetFullPath(path);
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + ";FMT=Delimited\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable
                {   
                    Locale = CultureInfo.CurrentCulture
                };
                adapter.Fill(dataTable);
                return dataTable;
            }
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
                ErrorReporter.StaticFatal(ex.Message, typeof(Util));
            }
        }

        public static void SerializeMessagePack(string file, object obj)
        {
            using (var f = File.OpenWrite(file))
            {
                LZ4MessagePackSerializer.Serialize(f, obj);
            }
        }

        public static void SerialzeBinaryFormatter(string file, object obj)
        {
            using (var f = File.OpenWrite(file))
            {
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(f, obj);
            }
        }

        public static void SerializeNewtonsoft(string file, object obj, JsonSerializer s = null)
        {

            using (var f = File.CreateText(file))
            {
                if (s == null)
                {
                    s = new JsonSerializer
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        Formatting = Formatting.None
                    };
                }
                s.Serialize(f, obj);
            }
        }



        public static T DeserializeMessagePack<T>(string file)
        {

            using (var f = File.OpenRead(file))
            {
                T m = LZ4MessagePackSerializer.Deserialize<T>(f);
                return m;
            }
        }

        public static T DeserializeNewtonsoft<T>(string file)
        {
            using (var f = File.OpenText(file))
            {
                JsonTextReader r = new JsonTextReader(f);
                JsonSerializer s = new JsonSerializer()
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };
                return s.Deserialize<T>(r);
            }
        }

        public static T DeserializeBinaryFormatter<T>(string file) where T : class
        {

            using (var f = File.OpenRead(file))
            {
                BinaryFormatter b = new BinaryFormatter();
                return b.Deserialize(f) as T;
            }
        }
    }

    internal enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Severe,
        Fatal
    }

    public enum DeviceType
    {
        Line,
        Switch,
        Transformer,
        Load,
        Regulator,
        ShuntCapacitor,
    }

    internal enum IdfFileType
    {
        Data,
        Graphics,
        ImportConfig
    }

    public class PFDetail
    {
        public short[] PhaseId1 { get; set; } = new short[3];
        public short[] PhaseId2 { get; set; } = new short[3];
        public short[] PhaseAngle1 { get; set; } = new short[3];
        public short[] PhaseAngle2 { get; set; } = new short[3];

        public double N1ExtDistance { get; set; } = double.NaN;
        public double N1IntDistance { get; set; } = double.NaN;
        public double N2ExtDistance { get; set; } = double.NaN;
        public double N2IntDistance { get; set; } = double.NaN;
    }
}