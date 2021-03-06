﻿using EGIS.ShapeFileLib;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace MainPower.Adms.Enricher
{
    public enum SearchMode
    {
        Exact,
        Contains,
        StartsWith,
        EndsWith
    }

    public enum SymbolPlacement 
    {
        Top,
        Bottom,
        Left, 
        Right
    }

    public static class Util
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
        public static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {

            path = Path.GetFullPath(path);

            var adapter = new GenericParsing.GenericParserAdapter(path);
            adapter.FirstRowHasHeader = true;
            DataTable dt = adapter.GetDataTable();
            return dt;
        }

        /// <summary>
        /// Reads a OSI table dbdump returns a DataTable
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromOsiDbdump(string path)
        {

            path = Path.GetFullPath(path);
            using StreamReader reader = File.OpenText(path);
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            var adapter = new GenericParsing.GenericParserAdapter(reader)
            {
                FirstRowHasHeader = true
            };
            DataTable dt = adapter.GetDataTable();
            //delete any empty keys at the end of the table
            while (string.IsNullOrWhiteSpace(dt.Rows[dt.Rows.Count - 1]["Key"].ToString()))
            {
                dt.Rows[dt.Rows.Count - 1].Delete();
            }
            
            return dt;
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
            using var f = File.OpenWrite(file);
            //LZ4MessagePackSerializer.Serialize(f, obj);
            MessagePackSerializer.Serialize(f, obj);
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

            using var f = File.OpenRead(file);
            //T m = LZ4MessagePackSerializer.Deserialize<T>(f);
            T m = MessagePackSerializer.Deserialize<T>(f);
            return m;
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

        public static PointD[] PointToPointD(List<Point> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            PointD[] result = new PointD[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                result[i] = points[i].PointD;
            }
            return result;
        }
    }

    public enum LogLevel
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
        Generator,
        EarthingTransformer,
    }

    public enum IdfFileType
    {
        Data,
        Graphics,
        ImportConfig
    }

    public class PFDetail
    {
        public short[] Phasing { get; set; } = new short[2];
        public double N1ExtDistance { get; set; } = double.NaN;
        public double N1IntDistance { get; set; } = double.NaN;
        public double N2ExtDistance { get; set; } = double.NaN;
        public double N2IntDistance { get; set; } = double.NaN;
    }
    
    [MessagePackObject]
    public struct Point : IEquatable<Point>
    {
        [Key(0)]
        public double X { get; set; }
        [Key(1)]
        public double Y { get; set; }
        [IgnoreMember]
        public PointD PointD
        {
            get
            {
                return new PointD { X = X, Y = Y };

            }
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        public bool Equals(Point other)
        {
            throw new NotImplementedException();
        }
    }
}