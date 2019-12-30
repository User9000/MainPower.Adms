using System.Threading.Tasks;

namespace MainPower.Adms.Enricher
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
        public abstract T RequestRecord<T>(string resourceIndexValue, string id, SearchMode searchMode = SearchMode.Exact) where T : DataType, new();
    }
}