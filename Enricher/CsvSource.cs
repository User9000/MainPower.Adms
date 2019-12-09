using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace MainPower.Adms.Enricher
{
    public class CsvDataSource : TableDataSource
    {
        public string FileName { get; set; }

        protected override bool OnInitialize()
        {
            try
            {
                Data = Util.GetDataTableFromCsv(Path.Combine(Program.Options.DataPath, FileName), true);
                //speed 'select'
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

        public override bool Save<T>()
        {
            try
            {
                Data.AcceptChanges();
                Util.ExportDatatable(Data, Path.Combine(Program.Options.DataPath, FileName));
                return true;
            }
            catch (Exception ex)
            {
                Err(ex.Message);
                return false;
            }


        }
    }
    
}