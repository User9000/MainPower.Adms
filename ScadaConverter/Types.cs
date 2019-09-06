using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MainPower.Osi.ScadaConverter
{
    /// <summary>
    /// Contains key details for a point or tag
    /// </summary>
    public class PointDetail
    {
        public const int LIMIT1 = 0;
        public const int LIMIT2 = 1;
        public const int LIMIT3 = 2;
        public const int LIMIT4 = 3;
        public const int REASON = 4;

        public string PointName;
        public string Location;
        public string StationName;
        public string PointGroup;
        public string Property;
        public string NormalState;
        public string OnLabel;
        public string OffLabel;
        public string Units;
        public string RtuName;
        public bool Discrete;
        public string Device;
        public string PointType;
        public double ScaleFactor;
        public double Offset;
        public string Equipment;
        public string Address;
        public RtuTagInfo RTUTag;
        public string DnpControlOnCode;
        public string DnpControlOffCode;
        //public string ReasonabilityLow;
        //public string ReasonabilityHigh;
        public string ModbusFunctionCode;
        public string AlarmGroup;
        public string Archive;
        //LoLo, Lo, Hi, HiHi
        public bool[] WwLimits = new bool[4];

        //limits are 1,2,3,4,reasonability-
        public bool[] ELimits = new bool[5];
        public string[] LowLimits = new string[5];
        public string[] HighLimits = new string[5];
    }

    /// <summary>
    /// Maps a tag to a new tag name. 
    /// NewTagName - Renamed tag input name
    /// Location - Location override for the Point
    /// Device - Device override for the point
    /// Property - Property override for the point
    /// OnLabel - OnLabel override for the point
    /// OffLabel - OffLabel override for the point
    /// Units - Units override for the point
    /// NormalState - NormalState override for the point
    /// </summary>
    public class TagOverrideInfo
    {
        public string NewTagName;
        public string Location;
        public string Device;
        public string Property;
        public string PointType;
        public string OnLabel;
        public string OffLabel;
        public string Units;
        public string NormalState;
        public string ReasonabilityLow;
        public string ReasonabilityHigh;
        public string Archive;
        public string AlarmGroup;
    }

    /// <summary>
    /// NewRtuName - The new RTU name
    /// Location - The location short code
    /// Device - Device override for all points in the RTU
    /// DefaultEquipment - The default equipment to specifiy if none provided by the RTU
    /// </summary>
    public class RtuInfo
    {
        public string NewRtuName;
        public string Location;
        public string Device;
        public string RtuConfig;
        public string DefaultEquipment;
        public string Station;
        public List<(int, int, int, int)> ModbusConfiguration = new List<(int, int, int, int)>();
    }

    /// <summary>
    /// Maps an InTouch tag name to a Property name.  
    /// Priority is used to give precedence where there is more than one match (lower is higher precedence
    /// OsiType is the osi data type
    /// PointGroup is used to link related tags
    /// Substitutions are used to standardize On/Off Status Labels for discrete tags
    /// Units are used to standardize units for analog tags
    /// ScaleFactor is what the default scaling should be 
    /// ToDelete instead of remapping the tag, delete it from the database
    /// ForceNormalState Force the normal state to be 0=none,1=substitions[0],2=substitutions[1]
    /// DeviceOverride override the device for the point.  !=blank,<str=trim 'str' from the device
    /// </summary>
    public class TagMapInfo
    {
        public string TagName;
        public int Priority;
        public string OsiType;
        public string PointGroup;
        public List<(string, string[])> Substitutions;
        public string Units;
        public Dictionary<string,double> ScaleOverrides;
        public bool ToDelete;
        public string ForceNormalState;
        public string DeviceOverride;
        public string AlarmGroup;
        public string Archive;
        public string ReasonabilityLow;
        public string ReasonabilityHigh;
    }

    /// <summary>
    /// Tag information extracted from SCD5200/C50 configuration files
    /// </summary>
    public class RtuTagInfo
    {
        public string RtuConfig;
        public string TagName;
        public string Type;
        public string Equipment;
        public string Index;
    }

    /// <summary>
    /// Misc functions and extension methods 
    /// </summary>
    public static class Util
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Removes any additional information from a dnp address string
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string SanitizeDnpAddress(string address)
        {
            //TODO: this can probably be easily done in a single regex....
            Regex r = new Regex(@"[0-9]+\.[0-9]+\.[0-9]+");
            var m = r.Match(address);
            if (m.Success)
            {
                return m.Value;
            }
            else
                return address;
        }

        /// <summary>
        /// Trim a string from a string
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static string Trim(this string target, string trimString)
        {
            return target.TrimStart(trimString).TrimEnd(trimString);
        }

        /// <summary>
        /// Trim a string from the start of a string.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static string TrimStart(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString)) return target;

            string result = target;
            while (result.StartsWith(trimString))
            {
                result = result.Substring(trimString.Length);
            }

            return result;
        }

        /// <summary>
        /// Trim a string from the end of a string
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static string TrimEnd(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString)) return target;

            string result = target;
            while (result.EndsWith(trimString))
            {
                result = result.Substring(0, result.Length - trimString.Length);
            }

            return result;
        }

        /// <summary>
        /// Export a DataTable to CSV
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="file"></param>
        public static void ExportDatatable(DataTable dt, string file)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                  string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            try
            {
                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Reads a dnp address and returns the data type:
        /// </summary>
        /// <param name="address"></param>
        /// <returns> One of: 
        /// DI - Digital Input
        /// DO - Digital Output
        /// AI - Analog Input
        /// AO - Analog Output
        /// CO - Counter
        /// NS - Default (Not Sure!)</returns>
        public static string DetermineDataType(string address)
        {
            if (address.StartsWith("1.") || address.StartsWith("2.") || address.StartsWith("10."))
            {
                return "DI";
            }
            else if (address.StartsWith("12."))
            {
                return "DO";
            }
            else if (address.StartsWith("30.") || address.StartsWith("31.") || address.StartsWith("32."))
            {
                return "AI";
            }
            else if (address.StartsWith("41."))
            {
                return "AO";
            }
            else if (address.StartsWith("20."))
            {
                return "CO";
            }
            else
                //the "not sure" data type
                return "NS";
        }

        /// <summary>
        /// Looks for a dnp address in @address in the format x.x.x where x is an integer number, 
        /// and returns the last number in the address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static int ExtractDnpIndex(string address)
        {
            //TODO: this can probably be easily done in a single regex....
            Regex r = new Regex(@"[0-9]+\.[0-9]+\.[0-9]+");
            var m = r.Match(address);
            if (m.Success)
            {
                //attempt to extract the dnp index from the address, if it fails then that's ok, it probably wasn't a dnp address so set the index to -1 to ignore it.
                int i;
                try
                {
                    var add = m.Value;
                    i = int.Parse(add.Substring(add.LastIndexOf('.') + 1, add.Length - add.LastIndexOf('.') - 1));
                }
                catch
                {
                    i = -1;
                }
                return i;
            }
            else
                return -1;
        }

        /// <summary>
        /// Reads a CSV file and returns a DataTable
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            path = Path.GetFullPath(path);
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + ";FMT=Delimited\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable
                {
                    Locale = CultureInfo.CurrentCulture
                };
                adapter.Fill(dataTable);
                return dataTable;
            }
        }
    }
}

