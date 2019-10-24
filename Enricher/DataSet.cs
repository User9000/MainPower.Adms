using System.Threading.Tasks;

namespace MainPower.Osi.Enricher
{
    public class Dataset
    {
        public DataSource Datasource { get; set; }
        public string Table { get; set; }
        public string IndexColumn { get; set; }
        public string Name { get; set; }

        public T RequestRecordByIndex<T>(string id) where T : DataType, new()
        {
            return Datasource.RequestRecord<T>(null, IndexColumn, id, true);
        }

        public T RequestRecordByColumn<T>(string column, string id, bool exact) where T : DataType, new()
        {
            return Datasource.RequestRecord<T>(null, column, id, exact);
        }

        public bool SetVale<T>(object indexValue, string columnName, object val) where T : DataType, new()
        {
            return Datasource.SetVale<T>(indexValue, columnName, val);
        }

        public bool Save<T>() where T : DataType, new()
        {
            return Datasource.Save<T>();
        }

    }
}
