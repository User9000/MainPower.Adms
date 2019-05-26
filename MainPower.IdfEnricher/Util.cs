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
    }
}
