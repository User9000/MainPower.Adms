using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MainPower.Osi.Enricher
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

        private Dictionary<string, Dataset> Datasets { get; set; } = new Dictionary<string, Dataset>();        

        /// <summary>
        /// Request a data record using the default index column
        /// </summary>
        /// <typeparam name="T">The datatype of the record being requested</typeparam>
        /// <param name="id">The id of the record</param>
        /// <returns>A record of type T if the record was found, or null</returns>
        public T RequestRecordById<T>(string id) where T: DataType, new()
        {
            
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecordByIndex<T>(id);
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
        public T RequestRecordByColumn<T>(string column, string id, bool exact = false) where T : DataType, new()
        {
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecordByColumn<T>(column, id, exact);
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
                Util.SerializeNewtonsoft(Path.Combine(Enricher.I.Options.DataPath, file), Datasets, s);
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
                    Datasets = Util.DeserializeNewtonsoft<Dictionary<string, Dataset>>(file);
                }
                catch (Exception ex)
                {
                    Fatal($"Failed to deserialize DataManager from file [{file}].  {ex.Message}");
                    return false;
                }
            }
            else
            {
                DataSource source = new CsvSource()
                {
                    Name = "T1Disconnectors",
                    FileName = "T1Disconnectors.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                var dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "T1Disconnectors",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(T1Disconnector), dataset);

                source = new CsvSource()
                {
                    Name = "T1Fuses",
                    FileName = "T1Fuses.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "T1Fuses",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(T1Fuse), dataset);

                source = new CsvSource()
                {
                    Name = "T1HvCircuitBreakers",
                    IndexColumn = "Asset Number",
                    FileName = "T1HvCircuitBreakers.csv",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "T1HvCircuitBreakers",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(T1HvCircuitBreaker), dataset);

                source = new CsvSource()
                {
                    Name = "T1RingMainUnits",
                    FileName = "T1RingMainUnits.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "T1RingMainUnits",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(T1RingMainUnit), dataset);

                source = new CsvSource()
                {
                    Name = "T1Transformers",
                    FileName = "T1Transformers.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "T1Transformers",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(T1Transformer), dataset);

                source = new CsvSource()
                {
                    Name = "TranspowerTransformers",
                    FileName = "TranspowerTransformers.csv",
                    IndexColumn = "id",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "id",
                    Name = "TranspowerTransformers",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(TranspowerTransformer), dataset);

                source = new CsvSource()
                {
                    Name = "ScadaStatus",
                    FileName = "ScadaStatus.csv",
                    IndexColumn = "Key",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Key",
                    Name = "ScadaStatus",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(OsiScadaStatus), dataset);

                source = new CsvSource()
                {
                    Name = "ScadaAnalog",
                    FileName = "ScadaAnalog.csv",
                    IndexColumn = "Key",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Key",
                    Name = "ScadaAnalog",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(OsiScadaAnalog), dataset);

                source = new CsvSource()
                {
                    Name = "ScadaAccumulator",
                    IndexColumn = "Key",
                    FileName = "ScadaAccumulator.csv",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Key",
                    Name = "ScadaAccumulator",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(OsiScadaAccumulator), dataset);

                source = new CsvSource()
                {
                    Name = "ScadaSetpoint",
                    IndexColumn = "Key",
                    FileName = "ScadaSetpoint.csv",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Key",
                    Name = "ScadaSetpoint",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(OsiScadaSetpoint), dataset);

                source = new CsvSource()
                {
                    Name = "AdmsSwitch",
                    FileName = "AdmsSwitch.csv",
                    IndexColumn = "Switch Number",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Switch Number",
                    Name = "AdmsSwitch",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(AdmsSwitch), dataset);

                source = new CsvSource()
                {
                    Name = "AdmsTransformer",
                    FileName = "AdmsTransformer.csv",
                    IndexColumn = "Asset Number",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "Asset Number",
                    Name = "AdmsTransformer",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(AdmsTransformer), dataset);

                source = new CsvSource()
                {
                    Name = "ICPs",
                    FileName = "ICPs.csv",
                    IndexColumn = "ICP",
                    InitializeFailIsFatal = true
                };
                dataset = new Dataset()
                {
                    IndexColumn = "ICP",
                    Name = "ICPs",
                    Datasource = source,
                    Table = null
                };
                Datasets.Add(nameof(Icp), dataset);

            }
            foreach (var d in Datasets.Values)
            {
                if (!d.Datasource.Initialize() && d.Datasource.InitializeFailIsFatal)
                {
                    return false;
                }
            }
            return true;
        }
    }
  
}
