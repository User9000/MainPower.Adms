using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    public class DataManager : ErrorReporter
    {
        public static DataManager I { get; } = new DataManager();

        public Dictionary<string, Dataset> Datasets { get; set; } = new Dictionary<string, Dataset>();        

        public T RequestRecordById<T>(string id) where T: DataType, new()
        {
            
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecord<T>(id);
            else
                return null;

        }

        public T RequestRecordByColumn<T>(string column, string id, bool exact = false) where T : DataType, new()
        {
            if (Datasets.ContainsKey(typeof(T).Name))
                return Datasets[typeof(T).Name].RequestRecordByColumn<T>(column, id, exact);
            else
                return null;

        }

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
                    Name = "ScadaStatus",
                    FileName = "ScadaStatus.csv",
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
                    Name = "AdmsSwitch",
                    FileName = "AdmsSwitch.csv",
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
    /*
    internal enum DataSourceType
    {
        Csv,
        SqlDatabase,
        RestApi
    }
    */

    

    


   

    
}
