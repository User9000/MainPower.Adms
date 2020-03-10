using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MainPower.Adms.Enricher
{
    public class SqliteSource : TableDataSource
    {
        public string Database { get; set; }
        public string Table { get; set; }

        protected override bool OnInitialize()
        {
            try
            {
                SQLiteConnection con = new SQLiteConnection(@$"Data Source={Path.Combine(Program.Options.DataPath, Database)};Version=3;", true);
                con.Open();
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {Table}";
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                Data = new DataTable();
                adapter.Fill(Data);
                con.Close();
                return true;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return false;
            }
        }
    }
}
