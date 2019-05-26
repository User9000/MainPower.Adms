using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
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
        

        internal void LoadSourceData()
        {
            T1Disconnectors = Util.GetDataTableFromCsv($"{Options.Path}\\T1Disconnectors.csv", true);
            T1Fuses = Util.GetDataTableFromCsv($"{Options.Path}\\T1Fuses.csv", true);
            T1HvCircuitBreakers = Util.GetDataTableFromCsv($"{Options.Path}\\T1HvCircuitBreakers.csv", true);
            T1RingMainUnits = Util.GetDataTableFromCsv($"{Options.Path}\\T1RingMainUnits.csv", true);
            ScadaStatus = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaStatus.csv", true);
            ScadaAnalog = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaAnalog.csv", true);
            ScadaAccumulator = Util.GetDataTableFromCsv($"{Options.Path}\\ScadaAccumulator.csv", true);
            AdmsSwitch = Util.GetDataTableFromCsv($"{Options.Path}\\AdmsSwitch.csv", true);
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

        internal DataRow GetT1DisconnectorByAssetNumber(string id)
        {
            var data = T1Disconnectors;
            var queryColumn = "Asset Number";
            var queryName = "T1 Disconnector";
            var s = ""; 

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal DataRow GetT1DisconnectorBySwitchNumber(string id)
        {
            var data = T1Disconnectors;
            var queryColumn = "Switch Number";
            var queryName = "T1 Disconnector";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = '{id}'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal DataRow GetT1FuseByAssetNumber(string id)
        {
            var data = T1Fuses;
            var queryColumn = "Asset Number";
            var queryName = "T1 Fuse";
            var s = "";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal DataRow GetT1RingMainUnitByT1AssetNumber(string t1assetno)
        {
            throw new NotImplementedException();
        }

        internal DataRow GetT1FuseBySwitchNumber(string id)
        {
            var data = T1Fuses;
            var queryColumn = "Switch Number";
            var queryName = "T1 Fuse";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = '{id}'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal DataRow GetT1HvCircuitBreakerByAssetNumber (string id)
        {
            var data = T1HvCircuitBreakers;
            var queryColumn = "Asset Number";
            var queryName = "T1 HV Circuit Breaker";
            var s = "";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal DataRow GetT1HvCircuitBreakerBySwitchNumber(string id)
        {
            var data = T1HvCircuitBreakers;
            var queryColumn = "Switch Number";
            var queryName = "T1 HV Circuit Breaker";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = '{id}'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal ScadaPointInfo GetScadaStatusInfoByExactName(string id)
        {
            ScadaPointInfo p = new ScadaPointInfo();
            var data = ScadaStatus;
            var queryColumn = "Name";
            var queryName = "Point Name";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = '{id}'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = result[0]["Key"] as string;
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

        internal ScadaPointInfo GetScadaStatusSwitchStatusInfoBySwitchNumber(string id)
        {
            ScadaPointInfo p = new ScadaPointInfo();
            var data = ScadaStatus;
            var queryColumn = "Name";
            var queryName = "Switch Number";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id}'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:*{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = result[0]["Key"] as string;
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

        internal DataRow AdmsGetSwitch(string id)
        {
            var data = AdmsSwitch;
            var queryColumn = "Switch Number";
            var queryName = "Adms Switch";
            var s = "'";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] = {s}{id}{s}");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
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

        internal ScadaPointInfo GetScadaStatusCircuitEarthBySwitchNumber(string id)
        {
            ScadaPointInfo p = new ScadaPointInfo();
            var data = ScadaStatus;
            var queryColumn = "Name";
            var queryName = "Switch Number (Circuit Earth)";

            lock (data)
            {
                try
                {
                    var result = data.Select($"[{queryColumn}] LIKE '*{id} Circuit Earth'");
                    if (result.Length == 0)
                    {
                        _log.Warn($"{queryName} not found with {queryColumn}:*{id}");
                        return null;
                    }
                    else if (result.Length > 1)
                    {
                        _log.Warn($"More than one {queryName} found with {queryColumn}:{id}");
                    }
                    p.Key = result[0]["Key"] as string;
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
    }
}
