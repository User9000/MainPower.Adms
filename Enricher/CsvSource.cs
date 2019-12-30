using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace MainPower.Adms.Enricher
{
    public class CsvDataSource : TableDataSource
    {
        public string FileName { get; set; }
        public bool IsOsiFormat { get; set; } = false;

        protected override bool OnInitialize()
        {
            try
            {
                if (IsOsiFormat)
                    Data = Util.GetDataTableFromOsiDbdump(Path.Combine(Program.Options.DataPath, FileName));
                else 
                    Data = Util.GetDataTableFromCsv(Path.Combine(Program.Options.DataPath, FileName), true);

                if (!string.IsNullOrWhiteSpace(IndexColumn))
                {
                    Data.PrimaryKey = new DataColumn[1] { Data.Columns[IndexColumn] };
                    DataView dv = new DataView(Data)
                    {
                        Sort = IndexColumn
                    };
                }
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