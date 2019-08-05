namespace MainPower.IdfEnricher
{
    public class Dataset
    {
        public DataSource Datasource { get; set; }
        public string Table { get; set; }
        public string IndexColumn { get; set; }
        public string Name { get; set; }

        public T RequestRecord<T>(string id) where T : DataType, new()
        {
            return Datasource.RequestRecordByIndex<T>(null, IndexColumn, id);
        }

        public T RequestRecordByColumn<T>(string column, string id, bool exact) where T : DataType, new()
        {
            return Datasource.RequestRecordByColumn<T>(null, column, id, exact);
        }
    }
}
