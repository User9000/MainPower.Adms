using System.Threading.Tasks;

namespace MainPower.Osi.Enricher
{
    public abstract class DataSource : ErrorReporter
    {
        public string Name { get; set; }
        public bool InitializeFailIsFatal { get; set; }

        public bool Initialize()
        {
            Info($"Initializing DataSource {Name}...");
            return OnInitialize();
        }

        protected abstract bool OnInitialize();
        public abstract T RequestRecord<T>(string id) where T : DataType, new();
        public abstract T RequestRecord<T>(string resourceIndexValue, string id, bool exact) where T : DataType, new();
        public abstract bool SetVale<T>(object indexValue, string columnName, object val) where T : DataType, new();
        public abstract bool Save<T>() where T : DataType, new();

    }
}