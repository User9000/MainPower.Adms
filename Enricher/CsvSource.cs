using System;
using System.Data;
using System.IO;

namespace MainPower.Osi.Enricher
{
    public class CsvDataSource : DataSource
    {
        private DataTable Data;

        public string FileName { get; set; }

        public string IndexColumn { get; set; }

        protected override bool OnInitialize() 
        {
            try
            {
                Data = Util.GetDataTableFromCsv(Path.Combine(Enricher.I.Options.DataPath, FileName), true);
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

        public override T RequestRecord<T>(string id)
        {
            return RequestRecord<T>(IndexColumn, id, true);
        }

        public override T RequestRecord<T>(string columnName, string id, bool exact)
        {
            try
            {
                //TODO: have a bunch of match types               
                var s = Data.Columns[columnName].DataType == typeof(string) ? "'" : "";
                //only string searches can be not exact
                if (s == "" && !exact)
                {
                    Warn("Can't have a non-exact non-string match");
                    return null;
                }

                DataRow[] result;
                if (exact)
                {
                    result = Data.Select($"[{columnName}] = {s}{id}{s}");
                }
                else
                {
                    result = Data.Select($"[{columnName}] LIKE '* {id}'");
                }
                if (result.Length == 0)
                {
                    Debug($"Record Not found with {columnName}:{id}");
                    return null;
                }
                else if (result.Length > 1)
                {
                    Warn($"More than one record found with {columnName}:{id}");
                }
                var obj = new T();
                obj.FromDataRow(result[0]);
                return obj;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return null;
            }
        }

        
        public override bool SetVale<T>(object indexValue, string columnName, object val)
        {
            try
            {
                string c = Data.Columns[IndexColumn].DataType == typeof(string) ? "'" : "";

                var result = Data.Select($"[{IndexColumn}] = {c}{indexValue.ToString()}{c}");
                foreach (var r in result)
                {
                    r[columnName] = val;
                }
                return true;
            }
            catch (Exception ex)
            {
                Err(ex.Message);
                return false;
            }

        }

        public override bool Save<T>()
        {
            try
            {
                Data.AcceptChanges();
                Util.ExportDatatable(Data, Path.Combine(Enricher.I.Options.DataPath, FileName));
                return true;
            }
            catch (Exception ex)
            {
                Err(ex.Message);
                return false;
            }


        }
    }
    /*
    public class SqlSource : DataSource
    {

    }
    public class SqliteSource : DataSource
    {

    }
    public class OsiDatabaseSource : DataSource
    {

    }
    public class MsSqlSource : DataSource
    {


    }
    */
}