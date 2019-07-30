using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;

namespace MainPower.IdfEnricher
{
    static class Util
    {
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
    }

    enum ScadaPointType
    {
        StatusInput,
        StatusOutput,
        AnalogInput,
        AnalogOutput
    }

    internal class GroupSet
    {
        public List<Idf> GraphicFiles { get; set; } = new List<Idf>();
        public Idf DataFile { get; set; } = null;
    }

    internal enum IdfFileType
    {
        Data,
        Graphics,
        ImportConfig
    }

    public class PFDetail
    {
        public bool Node1Mark { get; set; }
        public bool Node2Mark { get; set; }
        public double Node1Distance { get; set; } = double.NaN;
        public double Node2Distance { get; set; } = double.NaN;
    }

    class ScadaStatusPointInfo
    {
        public string PointName { get; set; } = "";
        public string Key { get; set; } = "";
        public string PointType { get; set; } = "";
        public bool QuadState { get; set; } = false;
    }

    class ScadaAnalogPointInfo
    {
        public string PointName { get; set; } = "";
        public string Key { get; set; } = "";
        public string PointType { get; set; } = "";
        public string Units { get; set; } = "";
    }
}