using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        internal static string FormatLogString(LogLevel level, string code, string id, string name, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    Enricher.I.Debugs++;
                    break;
                case LogLevel.Error:
                    Enricher.I.Errors++;
                    break;
                case LogLevel.Fatal:
                    Enricher.I.Fatals++;
                    Enricher.I.FatalErrorOccurred = true;
                    break;
                case LogLevel.Info:
                    Enricher.I.Infos++;
                    break;
                case LogLevel.Severe:
                    Enricher.I.Severes++;
                    break;
                case LogLevel.Warn:
                    Enricher.I.Warns++;
                    break;
            }
            //TODO: loglevel stats counters
            if (level == LogLevel.Error)
                return string.Format("{0,-21},{1,-40},{2,-20},{3,-80}", code, id, name, $"\"{message}\"");
            else
                return string.Format("{0,-22},{1,-40},{2,-20},{3,-80}", code, id, name, $"\"{message}\"");
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
}