using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    internal class DataManager
    {
        Dictionary<Type, IDataset> Datasets = new Dictionary<Type, IDataset>();

        public void Save(string file)
        {

        }

        public static void Load(string file)
        {

        }
    }

    internal enum DataSourceType
    {
        Csv,
        SqlDatabase,
        RestApi
    }

    internal abstract class DataSource : ErrorReporter
    {
        public DataSourceType Type { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        internal abstract T RequestRecord<T>(string resourceIndexName, string resourceIndexValue, string id) where T : class, IData, new();

    }

    internal interface IDataset
    {

    }

    internal class Dataset<T> : IDataset where T : class, IData, new()
    {
        public DataSource Datasource { get; set; }
        public string Table { get; set; }
        public string IndexColumn { get; set; }
        public string Name { get; set; }

        public T RequestRecord(string id)
        {
            return Datasource.RequestRecord<T>(null, IndexColumn, id);
        }
    }

    internal interface IData
    {
        IData FromCsv(DataRow r);
        void FromSql();
        void FromRestApi();
        void FromOsiDatabase();
    }

    internal class T1Disconnector : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }

    internal class T1Fuse : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class T1HvCircuitBreaker : IData
    {
        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class T1RingMainUnit : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }

    internal class T1Transformer : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class OsiScadaStatus : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class OsiScadaAnalog : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class AdmsSwitch : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class AdmsTransformer : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }
    internal class Icp : IData
    {
        public void FromCsv()
        {
            throw new NotImplementedException();
        }

        public IData FromCsv(DataRow r)
        {
            throw new NotImplementedException();
        }

        public void FromOsiDatabase()
        {
            throw new NotImplementedException();
        }

        public void FromRestApi()
        {
            throw new NotImplementedException();
        }

        public void FromSql()
        {
            throw new NotImplementedException();
        }
    }

    internal class CsvSource : DataSource
    {
        private DataTable Data;

        public bool Initialize()
        {
            try
            {
                Data = Util.GetDataTableFromCsv(Url, true);
                return true;
            }
            catch (Exception ex)
            {
                Fatal(ex.Message);
                return false;
            }
        }

        internal override T RequestRecord<T>(string table, string columnName, string id)
        {
            var trueforstringfalseforint = true;
            var s = trueforstringfalseforint ? "'" : "";
            //TODO: auto wrap based on column data type

            try
            {
                var result = Data.Select($"[{columnName}] = {s}{id}{s}");
                if (result.Length == 0)
                {
                    Debug($"{table}: Not found with {columnName}:{id}");
                    return null;
                }
                else if (result.Length > 1)
                {
                    Warn($"{table}: More than one {table} found with {columnName}:{id}");
                }
                return new T().FromCsv(result[0]) as T;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }
        }
    }

    internal class SqlSource : DataSource
    {
        internal override T RequestRecord<T>(string resourceIndexName, string resourceIndexValue, string id)
        {
            throw new NotImplementedException();
        }
    }
    internal class SqliteSource : DataSource
    {
        internal override T RequestRecord<T>(string resourceIndexName, string resourceIndexValue, string id)
        {
            throw new NotImplementedException();
        }
    }
    internal class OsiDatabaseSource : DataSource
    {
        internal override T RequestRecord<T>(string resourceIndexName, string resourceIndexValue, string id)
        {
            throw new NotImplementedException();
        }
    }
    internal class MsSqlSource : DataSource
    {

        internal override T RequestRecord<T>(string resourceIndexName, string resourceIndexValue, string id)
        {
            throw new NotImplementedException();
        }
    }

}
