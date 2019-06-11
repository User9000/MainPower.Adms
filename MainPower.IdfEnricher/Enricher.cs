using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
    enum PointType
    {
        StatusInput,
        StatusOutput,
        AnalogInput, 
        AnalogOutput
    }


    class Enricher
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal static Enricher Singleton { get; } = new Enricher();

        internal Options Options { get; set; }

        internal DataTable T1Disconnectors { get; set; }
        internal DataTable T1Fuses { get; set; }
        internal DataTable T1HvCircuitBreakers { get; set; }
        internal DataTable T1RingMainUnits { get; set; }
        internal DataTable T1Transformers { get; set; }
        internal DataTable ScadaStatus { get; set; }
        internal DataTable ScadaAnalog { get; set; }
        internal DataTable ScadaAccumulator { get; set; }
        internal DataTable AdmsSwitch { get; set; }
        internal DataTable AdmsTransformer { get; set; }


        internal void LoadSourceData()
        {
            T1Disconnectors = Util.GetDataTableFromCsv($"{Options.Path}\\T1Disconnectors.csv", true);
            T1Fuses = Util.GetDataTableFromCsv($"{Options.Path}\\T1Fuses.csv", true);
            T1HvCircuitBreakers = Util.GetDataTableFromCsv($"{Options.Path}\\T1HvCircuitBreakers.csv", true);
            T1RingMainUnits = Util.GetDataTableFromCsv($"{Options.Path}\\T1RingMainUnits.csv", true);
            T1Transformers = Util.GetDataTableFromCsv($"{Options.Path}\\T1Transformers.csv", true);
            ScadaStatus = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaStatus.csv", true);
            ScadaAnalog = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaAnalog.csv", true);
            ScadaAccumulator = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaAccumulator.csv", true);
            AdmsSwitch = Util.GetDataTableFromCsv($"{Options.Path}\\AdmsSwitch.csv", true);
            AdmsTransformer = Util.GetDataTableFromCsv($"{Options.Path}\\AdmsTransformer.csv", true);
        }

        internal void ProcessImportConfiguration()
        {
            var tasks = new List<Task>();
            XmlDocument doc = new XmlDocument();
            doc.Load($"{Options.Path}\\ImportConfig.xml");
            var nodes = doc.SelectNodes("//container[@name!=\"Globals\"]/group");
            foreach (XmlNode node in nodes)
            {
                var id = node.Attributes["id"].InnerText;
                GroupProcessor p = new GroupProcessor(id);
                tasks.Add(Task.Run((Action)p.Process));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private DataRow GenericDatasetQuery(DataTable data, string queryColumn, string queryName, bool trueforstringfalseforint, string id)
        {
            var s = trueforstringfalseforint ? "'": "";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        Debug(queryName,  $"Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn(queryName,  $"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    return result[0];
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    return null;
                }
            }
        }

        internal DataRow GetT1DisconnectorByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Disconnectors, "Asset Number", "T1 Disconnector", false, id);
        }

        internal DataRow GetT1TransformerByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Transformers, "Asset Number", "T1 Transformer", false, id);
        }

        internal DataRow GetAdmsTransformerByAssetNumber(string id)
        {
            return GenericDatasetQuery(AdmsTransformer, "Asset Number", "Adms Transformer", false, id);
        }

        internal DataRow GetT1FuseByAssetNumber(string id)
        {
            return GenericDatasetQuery(T1Fuses, "Asset Number", "T1 Fuse", false, id);
        }

        internal DataRow GetT1RingMainUnitByT1AssetNumber(string id)
        {
            return GenericDatasetQuery(T1RingMainUnits, "Asset Number", "T1 RMU", false, id);
        }

        internal DataRow GetT1HvCircuitBreakerByAssetNumber (string id)
        {
            return GenericDatasetQuery(T1HvCircuitBreakers, "Asset Number", "T1 HV Circuit Breaker", false, id);
        }

        internal ScadaStatusPointInfo GetScadaStatusPointInfo(string id)
        {
            ScadaStatusPointInfo p = new ScadaStatusPointInfo();
            var data = ScadaStatus;
            var queryColumn = "Name";
            var queryName = "SCADA Point";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id}'");
                    if (result.Length == 0)
                    {
                        Debug(queryName, "Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn(queryName, $"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = (result[0]["Key"] as int?).ToString();
                    p.PointType = result[0]["Type"] as string;
                    p.PointName = result[0]["Name"] as string;
                    //a point is quad state if there are four states, which are separated by '/'
                    p.QuadState = (result[0]["pStates"] as string).Count(x => x == '/') == 3;

                    return p;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    return null;
                }
            }
        }

        internal ScadaAnalogPointInfo GetScadaAnalogPointInfo(string id)
        {
            ScadaAnalogPointInfo p = new ScadaAnalogPointInfo();
            var data = ScadaAnalog;
            var queryColumn = "Name";
            var queryName = "SCADA Point";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id}'");
                    if (result.Length == 0)
                    {
                        Debug(queryName, "Not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        Warn(queryName, $"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = (result[0]["Key"] as int?).ToString();
                    p.PointType = result[0]["Type"] as string;
                    p.PointName = result[0]["Name"] as string;
                    p.Units = result[0]["Units"] as string;

                    return p;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    return null;
                }
            }
        }

        internal DataRow GetAdmsSwitch(string id)
        {
            return GenericDatasetQuery(AdmsSwitch, "Switch Number", "Adms Switch", true, id);
        }

        protected void Debug(string code, string message)
        {
            _log.Debug($"ENRICHER,{code},,{message}");
        }

        protected void Info(string code, string message)
        {
            _log.Info($"ENRICHER,{code},,,{message}");
        }

        protected void Warn(string code, string message)
        {
            _log.Warn($"ENRICHER,{code},,{message}");
        }

        protected void Error(string code, string message)
        {
            _log.Error($"ENRICHER,{code},,{message}");
        }
    }
}
