using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// Marshals external data requests to the appropriate DataSource
    /// </summary>
    public class DataManager : ErrorReporter
    {
        /// <summary>
        /// Singleton for global access to the DataManager
        /// </summary>
        public static DataManager I { get; } = new DataManager();

        private Dictionary<string, DataSource> Datasets { get; set; } = new Dictionary<string, DataSource>();        

        /// <summary>
        /// Request a data record using the index column
        /// </summary>
        /// <typeparam name="T">The datatype of the record being requested</typeparam>
        /// <param name="id">The id of the record</param>
        /// <returns>A record of type T if the record was found, or null</returns>
        public T RequestRecordById<T>(string id) where T: DataType, new()
        {
            
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecord<T>(id);
            else
                return null;

        }

        /// <summary>
        /// Sets a value on an object
        /// TODO: move this to the DataType class?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexValue">The index value of the object</param>
        /// <param name="columnName">The name of the column to set</param>
        /// <param name="val">The column value </param>
        /// <returns></returns>
        public bool SetVale<T>(object indexValue, string columnName, object val) where T : DataType, new()
        {
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].SetVale<T>(indexValue, columnName, val);
            else
                return false;
        }

        /// <summary>
        /// Saves any changes to a DataSet for a DataType
        /// TODO: move this to the DataType class?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Save<T>() where T : DataType, new()
        {
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].Save<T>();
            else
                return false;
        }

        /// <summary>
        /// Request a data record using a named column
        /// </summary>
        /// <typeparam name="T">The datatype of the record being requested</typeparam>
        /// <param name="column">The name of the column used to lookup the data</param>
        /// <param name="id">The id of the record</param>
        /// <param name="exact">Match the id exactly, or use 'like'</param>
        /// <returns>A record of type T if the record was found, or null</returns>
        public T RequestRecordByColumn<T>(string column, string id, SearchMode searchMode = SearchMode.Exact) where T : DataType, new()
        {
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecord<T>(column, id, searchMode);
            else
                return null;

        }

        /// <summary>
        /// Saves the DataManager configuration
        /// </summary>
        /// <param name="file">The file to save to</param>
        public void Save(string file)
        {
            try
            {
                var s = new JsonSerializer
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                };
                Util.SerializeNewtonsoft(Path.Combine(Program.Options.DataPath, file), Datasets, s);
            }
            catch (Exception ex)
            {
                Fatal($"Failed to serialize DataManager to file [{file}].  {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the DataManager configuration
        /// </summary>
        /// <param name="file">The file to load from</param>
        /// <param name="loadDefaults">Load the default parameters rather than loading from file</param>
        /// <returns></returns>
        public bool Load(string file, bool loadDefaults = false)
        {
            Info("Loading DataManager configuration...");
            if (!loadDefaults)
            {
                try
                {
                    Datasets = Util.DeserializeNewtonsoft<Dictionary<string, DataSource>>(file);
                }
                catch (Exception ex)
                {
                    Fatal($"Failed to deserialize DataManager from file [{file}].  {ex.Message}");
                    return false;
                }
            }
            else
            {
                DataSource source = new CsvDataSource()
                {
                    Name = "T1Disconnectors",
                    FileName = "T1Disconnectors.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };                
                Datasets.Add(nameof(T1Disconnector), source);

                source = new CsvDataSource()
                {
                    Name = "T1Fuses",
                    FileName = "T1Fuses.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(T1Fuse), source);

                source = new CsvDataSource()
                {
                    Name = "T1HvCircuitBreakers",
                    IndexColumn = "Asset Number",
                    FileName = "T1HvCircuitBreakers.csv",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(T1HvCircuitBreaker), source);

                source = new CsvDataSource()
                {
                    Name = "T1RingMainUnits",
                    FileName = "T1RingMainUnits.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(T1RingMainUnit), source);

                source = new CsvDataSource()
                {
                    Name = "T1Transformers",
                    FileName = "T1Transformers.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(T1Transformer), source);

                source = new CsvDataSource()
                {
                    Name = "TranspowerTransformers",
                    FileName = "TranspowerTransformers.csv",
                    IndexColumn = "id",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(TranspowerTransformer), source);

                source = new CsvDataSource()
                {
                    Name = "ScadaStatus",
                    FileName = "ScadaStatus.csv",
                    IndexColumn = "Key",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(OsiScadaStatus), source);

                source = new CsvDataSource()
                {
                    Name = "ScadaAnalog",
                    FileName = "ScadaAnalog.csv",
                    IndexColumn = "Key",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(OsiScadaAnalog), source);

                source = new CsvDataSource()
                {
                    Name = "ScadaAccumulator",
                    IndexColumn = "Key",
                    FileName = "ScadaAccumulator.csv",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(OsiScadaAccumulator), source);

                source = new CsvDataSource()
                {
                    Name = "ScadaSetpoint",
                    IndexColumn = "Key",
                    FileName = "ScadaSetpoint.csv",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(OsiScadaSetpoint), source);
                source = new SqliteSource()
                {
                    Database = "adms.db",
                    Table = "Switch",
                    Name = "AdmsSwitch",
                    IndexColumn = "SwitchNumber",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(AdmsSwitch), source);
                source = new SqliteSource()
                {
                    Database = "adms.db",
                    Table = "Transformer",
                    Name = "AdmsTransformer",
                    IndexColumn = "AssetNumber",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(AdmsTransformer), source);
                source = new SqliteSource()
                {
                    Database = "adms.db",
                    Table = "Source",
                    Name = "AdmsSource",
                    IndexColumn = "Name",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(AdmsSource), source);

                source = new CsvDataSource()
                {
                    Name = "ICPs",
                    FileName = "ICPs.csv",
                    IndexColumn = "Name",
                    InitializeFailIsFatal = true
                };
                Datasets.Add(nameof(Icp), source);

            }
            foreach (var d in Datasets.Values)
            {
                if (!d.Initialize() && d.InitializeFailIsFatal)
                {
                    return false;
                }
            }
            return true;
        }
    }
  
}
