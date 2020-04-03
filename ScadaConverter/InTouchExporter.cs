using LumenWorks.Framework.IO.Csv;
using MainPower.Adms.ScadaConverter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MainPower.Adms.ScadaConverter
{
    public class InTouchExporter
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //The various sections of an intouch database dump
        private const string _ioaccess = "IOAccess";
        private const string _alarmgroup = "AlarmGroup";
        private const string _memorydisc = "MemoryDisc";
        private const string _iodisc = "IODisc";
        private const string _memoryint = "MemoryInt";
        private const string _ioint = "IOInt";
        private const string _memoryreal = "MemoryReal";
        private const string _ioreal = "IOReal";
        private const string _memorymsg = "MemoryMsg";
        private const string _iomsg = "IOMsg";
        private const string _groupvar = "GroupVar";
        private const string _historytrend = "HistoryTrend";
        private const string _tagid = "TagID";
        private const string _indirectdisc = "IndirectDisc";
        private const string _indirectanalog = "IndirectAnalog";
        private const string _indirectmsg = "IndirectMsg";

        //the path to put our output files
        private readonly string _outputPath;
        private readonly string _tempPath;
        //class that does the heavy lifting
        private readonly TagTransmogrifier _t;

        //discrete io tags that are to be translated into calculated points
        //i.e. references to intouch view client
        private DataTable discretecalcs = null;
        //int io tags that are to be translated to calculated points
        private DataTable intcalcs = null;

        //real io points that are to be translated to calculated points
        private DataTable realcalcs = null;

        //summary table of all converted points
        private DataTable _allPoints = new DataTable();
        //generic tags
        private DataTable _genericTags;
        private Options _options;

        /// <summary>
        /// Copy the RTUs file to the output directory
        /// Removes RTUs that are not required to be imported at this stage
        /// </summary>
        internal void CopyRtus()
        {
            var dt = Util.GetDataTableFromCsv($"{_options.Input}\\_rtus.csv", true);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var name = dt.Rows[i]["OSIName"] as string;
                if (name == "Calculation" || name.StartsWith("CVT_") || name.StartsWith("THW_") || name == "MQTT")
                {
                    dt.Rows[i].Delete();
                }
            }
            dt.AcceptChanges();
            Util.ExportDatatable(dt, $"{_options.Output}\\export_rtus.csv");
            File.Copy("log.log", $"{_options.Output}\\log.txt", true);
        }

        #region Validation Routines

        /// <summary>
        /// Checks that the io address isn't used more than once per RTU
        /// </summary>
        internal void CheckForDuplicateIO()
        {
            Dictionary<(string, string), string> io = new Dictionary<(string, string), string>();
            for (int i = 0; i < _allPoints.Rows.Count; i++)
            {
                var rtu = _allPoints.Rows[i]["RTU"] as string;
                var item = _allPoints.Rows[i]["ItemName"] as string;
                var regtype = _allPoints.Rows[i]["ModbusRegisterType"] as string;
                var point = _allPoints.Rows[i]["OldTag"] as string;
                if (!string.IsNullOrEmpty(rtu))
                {
                    if (!string.IsNullOrWhiteSpace(regtype))
                        item = $"{regtype}.{item}";
                    else
                        item = Util.SanitizeDnpAddress(item);

                    var tuple = (rtu, item);
                    if (io.ContainsKey(tuple))
                    {
                        _log.Error($"Tag {point} and {io[tuple]} both exist on RTU {tuple.rtu} with dnp address {tuple.Item2}");
                    }
                    else
                    {
                        io.Add(tuple, point);
                    }
                }
            }
        }

        /// <summary>
        /// Checks that Archive and AlarmGroups are consistent for combined points
        /// </summary>
        internal void CheckArchiveAndAlarmGroups(DataTable dt)
        {
            Dictionary<string, (string, string)> points = new Dictionary<string, (string, string)>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var point = dt.Rows[i]["PointName"] as string;
                var alarmgroup = dt.Rows[i]["AlarmGroup"] as string;
                var archive = dt.Rows[i]["Archive"] as string;

                if (!string.IsNullOrEmpty(point))
                {
                    if (points.ContainsKey(point))
                    {
                        if (alarmgroup != points[point].Item1)
                        {
                            _log.Warn($"Alarm groups on point {point} are not consistent [{points[point].Item1}/{alarmgroup}], choosing {points[point].Item1}");
                            dt.Rows[i]["AlarmGroup"] = points[point].Item1;
                        }
                        if (archive != points[point].Item2)
                        {
                            _log.Warn($"Archive flags on point {point} are not consistent [{points[point].Item2}/{archive}], choosing {points[point].Item2}");
                            dt.Rows[i]["Archive"] = points[point].Item2;
                        }
                    }
                    else
                    {
                        points.Add(point, (alarmgroup, archive));
                    }
                }
            }
        }

    

        /// <summary>
        /// Checks that the normal states labels are consistent, and for quads that the on/off states labels are complementary
        /// </summary>
        /// <param name="dt"></param>
        internal void CheckStatesConsistency(DataTable dt)
        {
            Dictionary<string, (string, string, string)> io = new Dictionary<string, (string,string, string)>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var item = dt.Rows[i]["ItemName"] as string;
                var point = dt.Rows[i]["PointName"] as string;
                var normal = dt.Rows[i]["NormalState"] as string;
                var onlabel = dt.Rows[i]["OnLabel"] as string;
                var offlabel = dt.Rows[i]["OffLabel"] as string;

                if (!string.IsNullOrEmpty(item))
                {
                    //only really applies to dnp input points
                    if (item.StartsWith("1."))
                    {
                        if (io.ContainsKey(point))
                        {
                            if (normal != io[point].Item1)
                            {
                                _log.Warn($"Normal states on point {point} are not consistent [{io[point].Item1}/{normal}], choosing {io[point].Item1}");
                                dt.Rows[i]["NormalState"] = io[point].Item1;
                            }
                            if (onlabel != io[point].Item3)
                            {
                                _log.Error($"Onlabel/Offlabel states on point {point} are not consistent {io[point].Item3}/{onlabel}");
                            }
                            if (offlabel != io[point].Item2)
                            {
                                _log.Error($"Onlabel/Offlabel states on point {point} are not consistent {io[point].Item2}/{offlabel}");
                            }
                        }
                        else
                        {
                            io.Add(point, (normal, onlabel, offlabel));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks that the same point name isn't used multiple times
        /// Note that it is not possible to make a foolproof algorithm for this
        /// </summary>
        internal void CheckForDuplicateNames()
        {
            Dictionary<string, string> io = new Dictionary<string, string>();
            for (int i = 0; i < _allPoints.Rows.Count; i++)
            {
                var name = _allPoints.Rows[i]["PointName"] as string;
                var item = _allPoints.Rows[i]["PointType"] as string;
                if (io.ContainsKey(name))
                {
                    if (io[name] == "T_I&C")
                    {
                        if (item == "T_I&C")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else if (io[name] == "T_R/L")
                    {
                        if (item == "T_R/L")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else if (io[name] == "T_IND")
                    {
                        if (item == "T_IND")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else if (io[name] == "T_CTL")
                    {
                        if (item == "T_CTL")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else if (io[name] == "T_ANLG")
                    {
                        if (item == "T_STPNT")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else if (io[name] == "T_STPNT")
                    {
                        if (item == "T_ANLG")
                            //this is ok
                            continue;
                        else
                            //this is not ok
                            _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                    else
                    {
                        _log.Error($"Point {name} already exists with an incompatible type.");
                    }
                }
                else
                {
                    io.Add(name, item);
                }
            }
        }

        /// <summary>
        /// Checks that there are no points with the same name that are T_CTRL and T_IND
        /// </summary>
        internal void CheckCombinationPointsAreCombined()
        {
            Dictionary<string, bool> io = new Dictionary<string, bool>();
            for (int i = 0; i < _allPoints.Rows.Count; i++)
            {
                var name = _allPoints.Rows[i]["PointName"] as string;
                var item = _allPoints.Rows[i]["PointType"] as string;
                if (item == "T_I&C")
                {
                    if (io.ContainsKey(name))
                    {
                        io[name] = true;
                    }
                    else
                    {
                        io.Add(name, false);
                    }
                }
            }
            foreach (var kvp in io)
            {
                if (kvp.Value == false)
                {
                    _log.Error($"Point {kvp.Key} has not been combined.");
                }
            }
        }

        /// <summary>
        /// Checks that feedback points actually exist in the database
        /// </summary>
        internal void ValidateFeebackPoints()
        {

            for (int i = 0; i < _allPoints.Rows.Count; i++)
            {
                var rl = _allPoints.Rows[i]["RemoteLocalPoint"] as string;
                var rlf = _allPoints.Rows[i]["RLFeedbackPoint"] as string;
                var point = _allPoints.Rows[i]["PointName"] as string;

                if (!string.IsNullOrWhiteSpace(rl))
                {
                    if (_allPoints.Select($"[PointName] = '{rl}'").Length == 0)
                        _log.Warn($"Point {point} has an invalid remote local point {rl}.");
                }
                if (!string.IsNullOrWhiteSpace(rlf))
                {
                    if (_allPoints.Select($"[PointName] = '{rlf}'").Length == 0)
                        _log.Warn($"Point {point} has an invalid raise/lower feedback point {rlf}.");
                }
            }
        }
        #endregion

        public InTouchExporter(Options o)
        {
            _options = o;
            _outputPath = o.Output;
            _tempPath = o.Temp;
            SplitDB($"{o.Input}{o.InTouchDatabse}");
            _t = new TagTransmogrifier(o.Input);
            //setup the summary datatable
            _allPoints.Columns.Add("RTU");
            _allPoints.Columns.Add("Station");
            _allPoints.Columns.Add("RtuTag");
            _allPoints.Columns.Add("OldTag");
            _allPoints.Columns.Add("Equipment");
            _allPoints.Columns.Add("ItemName");
            _allPoints.Columns.Add("PointType");
            _allPoints.Columns.Add("PointType2");
            _allPoints.Columns.Add("PointName");
            _allPoints.Columns.Add("NormalState");
            _allPoints.Columns.Add("OnLabel");
            _allPoints.Columns.Add("OffLabel");
            _allPoints.Columns.Add("Scale");
            _allPoints.Columns.Add("Offset");
            _allPoints.Columns.Add("Units");
            _allPoints.Columns.Add("RLFeedbackPoint");
            _allPoints.Columns.Add("RemoteLocalPoint");
            _allPoints.Columns.Add("Limit1Enabled");
            _allPoints.Columns.Add("Limit1Low");
            _allPoints.Columns.Add("Limit1High");
            _allPoints.Columns.Add("Limit2Enabled");
            _allPoints.Columns.Add("Limit2Low");
            _allPoints.Columns.Add("Limit2High");
            _allPoints.Columns.Add("Limit3Enabled");
            _allPoints.Columns.Add("Limit3Low");
            _allPoints.Columns.Add("Limit3High");
            _allPoints.Columns.Add("Limit4Enabled");
            _allPoints.Columns.Add("Limit4Low");
            _allPoints.Columns.Add("Limit4High");
            _allPoints.Columns.Add("ReasonabilityEnabled");
            _allPoints.Columns.Add("ReasonabilityHigh");
            _allPoints.Columns.Add("ReasonabilityLow");
            _allPoints.Columns.Add("DnpControlOnCode");
            _allPoints.Columns.Add("DnpControlOffCode");
            _allPoints.Columns.Add("ModbusRegisterType");
            _allPoints.Columns.Add("Archive");
            _allPoints.Columns.Add("AlarmGroup");
            _allPoints.DefaultView.Sort = "PointName, ItemName";
        }

        /// <summary>
        /// Splits the database into separate csv files for each data type
        /// </summary>
        private void SplitDB(string file)
        {
            _log.Info("Splitting the InTouch database...");
            TextReader stream = new StreamReader(file, Encoding.Default);
            string line;
            while ((line = stream.ReadLine()) != null)
            {
                //the order of these blocks is important
                if (line.StartsWith($":{_ioaccess}"))
                    line = CopyData(stream, line, _ioaccess);
                if (line.StartsWith($":{_alarmgroup}"))
                    line = CopyData(stream, line, _alarmgroup);
                if (line.StartsWith($":{_memorydisc}"))
                    line = CopyData(stream, line, _memorydisc);
                if (line.StartsWith($":{_iodisc}"))
                    line = CopyData(stream, line, _iodisc);
                if (line.StartsWith($":{_memoryint}"))
                    line = CopyData(stream, line, _memoryint);
                if (line.StartsWith($":{_ioint}"))
                    line = CopyData(stream, line, _ioint);
                if (line.StartsWith($":{_memoryreal}"))
                    line = CopyData(stream, line, _memoryreal);
                if (line.StartsWith($":{_ioreal}"))
                    line = CopyData(stream, line, _ioreal);
                if (line.StartsWith($":{_memorymsg}"))
                    line = CopyData(stream, line, _memorymsg);
                if (line.StartsWith($":{_iomsg}"))
                    line = CopyData(stream, line, _iomsg);
                if (line.StartsWith($":{_groupvar}"))
                    line = CopyData(stream, line, _groupvar);
                if (line.StartsWith($":{_historytrend}"))
                    line = CopyData(stream, line, _historytrend);
                if (line.StartsWith($":{_tagid}"))
                    line = CopyData(stream, line, _tagid);
                if (line.StartsWith($":{_indirectdisc}"))
                    line = CopyData(stream, line, _indirectdisc);
                if (line.StartsWith($":{_indirectanalog}"))
                    line = CopyData(stream, line, _indirectanalog);
                if (line.StartsWith($":{_indirectmsg}"))
                    line = CopyData(stream, line, _indirectmsg);
            }
        }

        /// <summary>
        /// Copies one section of data from the intouch database into an individual file
        /// Writes @firstline, then reads lines from the stream until a line starting with ':' 
        /// is reached.
        /// TODO: This algo is confusing and can probably be simplified
        /// </summary>
        /// <param name="stream">The open stream to the database file</param>
        /// <param name="firstline">This line should be written to @file before reading from @stream</param>
        /// <param name="file">The output file to write to</param>
        /// <returns>The first line of the next database section</returns>
        private string CopyData(TextReader stream, string firstline, string file)
        {
            string line;
            //TODO: need to test the output path for existance and create if required
            using (TextWriter wstream = new StreamWriter(new FileStream($"{_tempPath}\\{file}.csv", FileMode.Create, FileAccess.ReadWrite), Encoding.Default))
            {
                wstream.WriteLine(firstline);

                while ((line = stream.ReadLine()) != null)
                {
                    if (line.StartsWith(":"))
                    {
                        return line;
                    }
                    else
                    {
                        wstream.WriteLine(line);
                    }
                }
            }
            return "";
        }

        internal void ProcessIoDiscreteData()
        {
            _log.Info("Processing IO Discrete Data...");
            ProcessData(_iodisc, true);
            
        }

        internal void ProcessMemoryRealData()
        {
            _log.Info("Processing Memory Real Data...");
            ProcessData(_memoryreal);
        }

        internal void ProcessMemoryIntegerData()
        {
            _log.Info("Processing Memory Integer Data...");
            ProcessData(_memoryint);
        }

        internal void ProcessMemoryDiscreteData()
        {
            _log.Info("Processing Memory Discrete Data...");
            ProcessData(_memorydisc, true);
        }

        internal void ProcessIoIntegerData()
        {
            _log.Info("Processing IO Integer Data...");
            ProcessData(_ioint);
        }

        internal void ProcessIoRealData()
        {
            _log.Info("Processing IO Real Data...");
            ProcessData(_ioreal);
        }

        /// <summary>
        /// Where the magic happens...
        /// </summary>
        /// <param name="file"></param>
        /// <param name="discrete"></param>
        public DataTable ProcessData(string file, bool discrete = false)
        {
            //Create new data table from input csv data, then add/reorder columns
            DataTable dt = Util.GetDataTableFromCsv($"{_tempPath}\\{file}.csv", true);


            dt.Columns[$":{file}"].ColumnName = "OldTag";
            dt.Columns.Add("Location");
            dt.Columns.Add("Device");
            dt.Columns.Add("Property");
            dt.Columns.Add("PointName");
            dt.Columns.Add("PointName2");
            dt.Columns.Add("PointType");
            dt.Columns.Add("StationName");
            dt.Columns.Add("AlarmGroup");
            dt.Columns.Add("Archive");
            if (discrete)
            {
                dt.Columns.Add("NormalState");
                dt.Columns.Add("OnLabel");
                dt.Columns.Add("OffLabel");
            }
            int colIndex = 0;
            bool memoryTagFile = true;
            if (dt.Columns.Contains("AccessName"))
            {
                memoryTagFile = false;
                dt.Columns["AccessName"].SetOrdinal(colIndex++);
                dt.Columns.Add("RtuName");
                dt.Columns["RtuName"].SetOrdinal(colIndex++);
                dt.Columns.Add("Equipment");
                dt.Columns["Equipment"].SetOrdinal(colIndex++);
                dt.Columns.Add("RtuTag");
                dt.Columns.Add("ModbusRegisterType");
                if (discrete)
                {
                    dt.Columns.Add("DnpControlOnCode");
                    dt.Columns.Add("DnpControlOffCode");
                    dt.Columns.Add("PointType2");
                    dt.Columns.Add("RLFeedbackPoint");
                    dt.Columns.Add("RemoteLocalPoint");

                }
            }
            dt.Columns["OldTag"].SetOrdinal(colIndex++);
            dt.Columns["Location"].SetOrdinal(colIndex++);
            dt.Columns["Device"].SetOrdinal(colIndex++);
            dt.Columns["Property"].SetOrdinal(colIndex++);
            dt.Columns["PointName"].SetOrdinal(colIndex++);
            dt.Columns["PointName2"].SetOrdinal(colIndex++);
            dt.Columns["PointType"].SetOrdinal(colIndex++);
            dt.Columns["StationName"].SetOrdinal(colIndex++);
            if (discrete)
            {
                dt.Columns["NormalState"].SetOrdinal(colIndex++);
                dt.Columns["OnLabel"].SetOrdinal(colIndex++);
                dt.Columns["OffLabel"].SetOrdinal(colIndex++);

            }
            else
            {
                dt.Columns.Add("Offset");
                dt.Columns.Add("Scale");
                dt.Columns.Add("Limit1Enabled");
                dt.Columns.Add("Limit1Low");
                dt.Columns.Add("Limit1High");
                dt.Columns.Add("Limit2Enabled");
                dt.Columns.Add("Limit2Low");
                dt.Columns.Add("Limit2High");
                dt.Columns.Add("Limit3Enabled");
                dt.Columns.Add("Limit3Low");
                dt.Columns.Add("Limit3High");
                dt.Columns.Add("Limit4Enabled");
                dt.Columns.Add("Limit4Low");
                dt.Columns.Add("Limit4High");
                dt.Columns.Add("ReasonabilityEnabled");
                dt.Columns.Add("ReasonabilityHigh");
                dt.Columns.Add("ReasonabilityLow");
                dt.Columns["Offset"].SetOrdinal(colIndex++);
                dt.Columns["Scale"].SetOrdinal(colIndex++);
            }

            //This helps to speed up select queries in FetchGenericTags
            DataView dv = new DataView(dt)
            {
                Sort = "OldTag"
            };

            FetchGenericTags(dt, file);

            //Loop the DataTable, convert each row into PointDetail,
            //then process the point into a new point for OSI using the TagTransmogrifier.
            double r1 = 0, r2 = 0, e1 = 0, e2 = 0;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                PointDetail p = new PointDetail
                {
                    PointName = dt.Rows[i]["OldTag"] as string,
                    Location = dt.Rows[i]["Location"] as string //generics
                };

                if (!memoryTagFile)
                {
                    p.RtuName = dt.Rows[i]["AccessName"] as string;
                    p.Address = dt.Rows[i]["ItemName"] as string;
                }
                if (discrete)
                {
                    p.NormalState = dt.Rows[i]["InitialDisc"] as string;
                    p.OnLabel = dt.Rows[i]["OnMsg"] as string;
                    p.OffLabel = dt.Rows[i]["OffMsg"] as string;

                    if (!memoryTagFile)
                    {
                        p.Inverted = dt.Rows[i]["DConversion"] as string == "Reverse" ? true : false;
                    }
                }
                else
                {
                    p.Units = dt.Rows[i]["EngUnits"] as string;
                    p.ScaleFactor = 1;
                    if (!memoryTagFile)
                    {
                        r1 = double.Parse(dt.Rows[i]["MinRaw"].ToString());
                        r2 = double.Parse(dt.Rows[i]["MaxRaw"].ToString());
                        e1 = double.Parse(dt.Rows[i]["MinEU"].ToString());
                        e2 = double.Parse(dt.Rows[i]["MaxEU"].ToString());
                        p.ScaleFactor = ((e2 - e1) / (r2 - r1));
                        p.Offset = (e1 - p.ScaleFactor * r1).RoundToSignificantDigits(3);
                        p.ScaleFactor = p.ScaleFactor.RoundToSignificantDigits(4);
                    }
                    //alarms
                    p.WwLimits[0] = dt.Rows[i]["LoLoAlarmState"].ToString() == "On";
                    p.WwLimits[1] = dt.Rows[i]["LoAlarmState"].ToString() == "On";
                    p.WwLimits[2] = dt.Rows[i]["HiAlarmState"].ToString() == "On";
                    p.WwLimits[3] = dt.Rows[i]["HiHiAlarmState"].ToString() == "On";

                    if (p.WwLimits[0])
                    {
                        //LoLo maps to Limit3.Low
                        p.LowLimits[PointDetail.LIMIT3] = dt.Rows[i]["LoLoAlarmValue"].ToString();// double.Parse(dt.Rows[i]["LoLoAlarmValue"].ToString()).ToString("N4");
                        p.ELimits[PointDetail.LIMIT3] = true;
                    }
                    if (p.WwLimits[1])
                    {
                        //Lo maps to Limit2.Low
                        p.LowLimits[PointDetail.LIMIT2] = dt.Rows[i]["LoAlarmValue"].ToString();// double.Parse(dt.Rows[i]["LoAlarmValue"].ToString()).ToString("N4"); ;
                        p.ELimits[PointDetail.LIMIT2] = true;
                    }
                    if (p.WwLimits[2])
                    {
                        //Hi maps to Limit2.High
                        p.HighLimits[PointDetail.LIMIT2] = dt.Rows[i]["HiAlarmValue"].ToString();// double.Parse(dt.Rows[i]["HiAlarmValue"].ToString()).ToString("N4"); ;
                        p.ELimits[PointDetail.LIMIT2] = true;
                    }
                    if (p.WwLimits[3])
                    {
                        //HiHi maps to Limit3.High
                        p.HighLimits[PointDetail.LIMIT3] = dt.Rows[i]["HiHiAlarmValue"].ToString();//double.Parse(dt.Rows[i]["HiHiAlarmValue"].ToString()).ToString("N4"); ;
                        p.ELimits[PointDetail.LIMIT3] = true;
                    }
                }
                 

                PointDetail result = _t.GetPoint(p, discrete, !memoryTagFile);
                //result.OriginalPoint = p;

                //If the result was null, the TT does not want the point to be used in the OSI system.
                if (result == null)
                {
                    dt.Rows.RemoveAt(i);
                    i--;//decrement the counter, as we have removed the current row, so need to process it again
                    continue;
                }
                
                //check if the scaling was overridden, if so we must recalculate the MinEU/MaxEU
                if (!memoryTagFile && !discrete)
                {
                    if (p.ScaleFactor != result.ScaleFactor)
                    {
                        //TODO this behaviour is probably not obvious, and will catch someone out!
                        dt.Rows[i]["MinEU"] = Math.Floor(r1 * result.ScaleFactor + result.Offset);
                        dt.Rows[i]["MaxEU"] = Math.Ceiling(r2 * result.ScaleFactor + result.Offset);
                    }
                }

                //Otherwise, overwrite the DataTable row with the returned PointDetail info.
                dt.Rows[i]["Location"] = result.Location;
                dt.Rows[i]["Device"] = result.Device;
                dt.Rows[i]["Property"] = result.Property;
                dt.Rows[i]["PointName"] = result.PointName;
                dt.Rows[i]["PointName2"] = result.PointGroup;
                dt.Rows[i]["PointType"] = result.PointType;
                dt.Rows[i]["StationName"] = result.StationName;
                dt.Rows[i]["AlarmGroup"] = result.AlarmGroup;
                dt.Rows[i]["Archive"] = result.Archive;
                if (discrete)
                {
                    dt.Rows[i]["NormalState"] = result.NormalState;
                    dt.Rows[i]["OnLabel"] = result.OnLabel;
                    dt.Rows[i]["OffLabel"] = result.OffLabel;
                    if (!memoryTagFile)
                    {
                        dt.Rows[i]["DnpControlOnCode"] = result.DnpControlOnCode;
                        dt.Rows[i]["DnpControlOffCode"] = result.DnpControlOffCode;
                        dt.Rows[i]["RLFeedbackPoint"] = GetRLFeedbackPoint(result.PointGroup);
                        dt.Rows[i]["RemoteLocalPoint"] = GetRemoteLocalPoint(result);
                    }
                }
                else
                {
                   
                    
                    dt.Rows[i]["EngUnits"] = result.Units;
                    dt.Rows[i]["Scale"] = result.ScaleFactor.ToString("N7");
                    dt.Rows[i]["Offset"] = result.Offset.ToString("N7");

                    dt.Rows[i]["Limit1Enabled"] = result.ELimits[PointDetail.LIMIT1].ToString();
                    dt.Rows[i]["Limit1Low"] = result.LowLimits[PointDetail.LIMIT1];
                    dt.Rows[i]["Limit1High"] = result.HighLimits[PointDetail.LIMIT1];
                    dt.Rows[i]["Limit2Enabled"] = result.ELimits[PointDetail.LIMIT2].ToString();
                    dt.Rows[i]["Limit2Low"] = result.LowLimits[PointDetail.LIMIT2];
                    dt.Rows[i]["Limit2High"] = result.HighLimits[PointDetail.LIMIT2];
                    dt.Rows[i]["Limit3Enabled"] = result.ELimits[PointDetail.LIMIT3].ToString();
                    dt.Rows[i]["Limit3Low"] = result.LowLimits[PointDetail.LIMIT3];
                    dt.Rows[i]["Limit3High"] = result.HighLimits[PointDetail.LIMIT3];
                    dt.Rows[i]["Limit4Enabled"] = result.ELimits[PointDetail.LIMIT4].ToString();
                    dt.Rows[i]["Limit4Low"] = result.LowLimits[PointDetail.LIMIT4];
                    dt.Rows[i]["Limit4High"] = result.HighLimits[PointDetail.LIMIT4];
                    dt.Rows[i]["ReasonabilityEnabled"] = result.ELimits[PointDetail.REASON].ToString();
                    dt.Rows[i]["ReasonabilityHigh"] = result.HighLimits[PointDetail.REASON];
                    dt.Rows[i]["ReasonabilityLow"] = result.LowLimits[PointDetail.REASON];

                }

                if (!memoryTagFile)
                {
                    dt.Rows[i]["ItemName"] = result.Address;
                    dt.Rows[i]["RtuName"] = result.RtuName;
                    dt.Rows[i]["Equipment"] = result.Equipment;
                    dt.Rows[i]["RtuTag"] = result.RTUTag?.TagName;
                    dt.Rows[i]["ModbusRegisterType"] = result.ModbusFunctionCode;
                    //Remove points here that are calcs, but not a calc file
                    if (result?.RtuName == "Calculation")
                    {
                        _log.Debug($"Deferring calculation point {result.PointName} to memory export file");
                        DeferRowToMemoryFile(dt, dt.Rows[i], file);
                        dt.Rows.RemoveAt(i);
                        i--;//decrement the counter, as we have removed the current row, so need to process it again
                        continue;
                    }
                }

            }

            //Add points that were previously deferred
            AddDeferredPoints(file, dt);

            //sort out our combined state and control points
            if (!memoryTagFile && discrete)
            {
                CombineStatusAndControls(dt);
                CombineDuplicateDnpControls(dt);
                OsieriseDnpControls(dt);
                CheckStatesConsistency(dt);
                CheckArchiveAndAlarmGroups(dt);
            }

            //do another loop to copy the data to the summary table
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var row = _allPoints.NewRow();
                row["Station"] = dt.Rows[i]["StationName"];
                row["OldTag"] = dt.Rows[i]["OldTag"];
                row["PointName"] = dt.Rows[i]["PointName"];
                row["PointType"] = dt.Rows[i]["PointType"];
                row["AlarmGroup"] = dt.Rows[i]["AlarmGroup"];
                row["Archive"] = dt.Rows[i]["Archive"];
                if (!memoryTagFile)
                {
                    row["RTU"] = dt.Rows[i]["RtuName"];
                    row["Equipment"] = dt.Rows[i]["Equipment"];
                    row["ItemName"] = dt.Rows[i]["ItemName"];
                    row["RtuTag"] = dt.Rows[i]["RtuTag"];
                    row["ModbusRegisterType"] = dt.Rows[i]["ModbusRegisterType"];
                    if (discrete)
                    {
                        row["RLFeedbackPoint"] = dt.Rows[i]["RLFeedbackPoint"];
                        row["RemoteLocalPoint"] = dt.Rows[i]["RemoteLocalPoint"];
                        row["PointType2"] = dt.Rows[i]["PointType2"];
                        row["DnpControlOnCode"] = dt.Rows[i]["DnpControlOnCode"];
                        row["DnpControlOffCode"] = dt.Rows[i]["DnpControlOffCode"];
                    }
                }
                if (discrete)
                {
                    row["NormalState"] = dt.Rows[i]["NormalState"];
                    row["OnLabel"] = dt.Rows[i]["OnLabel"];
                    row["OffLabel"] = dt.Rows[i]["OffLabel"];
                }
                else
                {
                    row["Units"] = dt.Rows[i]["EngUnits"];
                    row["Scale"] = dt.Rows[i]["Scale"];
                    row["Offset"] = dt.Rows[i]["Offset"];

                    row["Limit1Enabled"] = dt.Rows[i]["Limit1Enabled"];
                    row["Limit1Low"] = dt.Rows[i]["Limit1Low"];
                    row["Limit1High"] = dt.Rows[i]["Limit1High"];
                    row["Limit2Enabled"] = dt.Rows[i]["Limit2Enabled"];
                    row["Limit2Low"] = dt.Rows[i]["Limit2Low"];
                    row["Limit2High"] = dt.Rows[i]["Limit2High"];
                    row["Limit3Enabled"] = dt.Rows[i]["Limit3Enabled"];
                    row["Limit3Low"] = dt.Rows[i]["Limit3Low"];
                    row["Limit3High"] = dt.Rows[i]["Limit3High"];
                    row["Limit4Enabled"] = dt.Rows[i]["Limit4Enabled"];
                    row["Limit4Low"] = dt.Rows[i]["Limit4Low"];
                    row["Limit4High"] = dt.Rows[i]["Limit4High"];
                    row["ReasonabilityEnabled"] = dt.Rows[i]["ReasonabilityEnabled"];
                    row["ReasonabilityHigh"] = dt.Rows[i]["ReasonabilityHigh"];
                    row["ReasonabilityLow"] = dt.Rows[i]["ReasonabilityLow"];
                }
                
                _allPoints.Rows.Add(row);
                var pname = dt.Rows[i]["PointName"] as string;
                if (pname.Length > 32)
                    _log.Warn($"Point '{pname}' violates length restriction ({pname.Length} chars).");

            }
            if (memoryTagFile)
                dv.Sort = "PointName";
            else
                dv.Sort = "PointName, ItemName";

            Util.ExportDatatable(dv.ToTable(), $"{_outputPath}\\export_{file}.csv");

            return dt;
        }

        /// <summary>
        /// Combine InTouch tags which have duplicate dnp 
        /// </summary>
        /// <param name="dt"></param>
        private void CombineDuplicateDnpControls(DataTable dt)
        {
            Dictionary<(string,string), int> io = new Dictionary<(string, string), int>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var rtu = dt.Rows[i]["RTUName"] as string;
                var name = dt.Rows[i]["PointName"] as string;
                var item2 = dt.Rows[i]["ItemName"] as string;
                var point2 = dt.Rows[i]["OldTag"] as string;
                
                if (!string.IsNullOrEmpty(rtu))
                {
                    var address = Util.SanitizeDnpAddress(item2);
                    if (address.StartsWith("12."))
                    {
                        var tuple = (name, address);
                        if (io.ContainsKey(tuple))
                        {
                            var row1 = io[tuple];
                            var point1 = dt.Rows[row1]["OldTag"] as string;
                            var oncode1 = dt.Rows[row1]["DnpControlOnCode"] as string;
                            var oncode2 = dt.Rows[i]["DnpControlOnCode"] as string;
                            var offcode1 = dt.Rows[row1]["DnpControlOffCode"] as string;
                            var offcode2 = dt.Rows[i]["DnpControlOffCode"] as string;
                            var onlabel1 = dt.Rows[row1]["OnLabel"] as string;
                            var onlabel2 = dt.Rows[i]["OnLabel"] as string;
                            var offlabel1 = dt.Rows[row1]["OffLabel"] as string;
                            var offlabel2 = dt.Rows[i]["OffLabel"] as string;
                            
                            if (!((onlabel1 == "-" && offlabel2 == "-") || (onlabel2 == "-" && offlabel1 == "-")))
                            {
                                _log.Warn($"Duplicate dnp controls found on {point1} and {point2} but they couldn't be combined.");
                                continue;
                            }

                            if (onlabel1 == "On" || onlabel1 == "Close")
                            {
                                _log.Info($"Combining duplicate dnp controls on points {point1} and {point2}");
                                dt.Rows[row1]["DnpControlOffCode"] = oncode2;
                                dt.Rows[row1]["OffLabel"] = offlabel2;
                                dt.Rows[row1]["PointName2"] += " (Comb.)";
                                //dt.Rows[row1]["ItemName"] = address;
                                dt.Rows.RemoveAt(i);
                                i--;
                            }
                            else if (onlabel2 == "On" || onlabel2 == "Close")
                            {
                                _log.Info($"Combining duplicate dnp controls on points {point1} and {point2}");
                                dt.Rows[row1]["DnpControlOnCode"] = oncode2;
                                dt.Rows[row1]["OnLabel"] = onlabel2;
                                dt.Rows[row1]["PointName2"] += " (Comb.)";
                                //dt.Rows[row1]["ItemName"] = address;
                                dt.Rows.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                _log.Warn($"Duplicate dnp controls found on {point1} and {point2} but couldn't detemine the correct label orientation.");
                                continue;
                            }
                        }
                        else
                        {
                            io.Add(tuple, i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks for dnp controls which are 'Open'/'Lower'/'Off' only controls and moves them to the OffLabel column
        /// </summary>
        /// <param name="dt"></param>
        private void OsieriseDnpControls(DataTable dt)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var rtu = dt.Rows[i]["RTUName"] as string;
                var name = dt.Rows[i]["PointName"] as string;
                var name2 = dt.Rows[i]["PointName2"] as string;
                var item = dt.Rows[i]["ItemName"] as string;
                var point = dt.Rows[i]["OldTag"] as string;
                var type = dt.Rows[i]["PointType"] as string;

                if (item.StartsWith("2.2."))
                {
                    dt.Rows[i]["ItemName"] = item.Replace("2.2.","1.2.");
                    _log.Warn($"Point {point} has a binary change EVENT address ({item}) - should be a static variation (1.x.x)");
                }

                if (!string.IsNullOrEmpty(rtu))
                {
                    var address = Util.SanitizeDnpAddress(item);
                    if ((address.StartsWith("12.") || (name2?.EndsWith("Ctl") ?? false)) && (type == "T_CTL" || type == "T_R/L" || type == "T_I&C"))
                    {

                        var oncode = dt.Rows[i]["DnpControlOnCode"] as string;
                        var offcode = dt.Rows[i]["DnpControlOffCode"] as string;
                        var onlabel = dt.Rows[i]["OnLabel"] as string;
                        var offlabel = dt.Rows[i]["OffLabel"] as string;

                        if (offlabel == "-")
                        {
                            if (onlabel == "Off" || onlabel == "Open" || onlabel == "Lower")
                            {
                                _log.Info($"Swapping the states on point {name}/{point}");
                                dt.Rows[i]["DnpControlOnCode"] = offcode;
                                dt.Rows[i]["DnpControlOffCode"] = oncode;
                                dt.Rows[i]["OnLabel"] = offlabel;
                                dt.Rows[i]["OffLabel"] = onlabel;
                            }
                        }
                    }
                }
            }
        }

        private void CombineStatusAndControls(DataTable dt)
        {
            var rows = dt.Select("[PointName2] <> ''", "PointName2");
            int gStart = 0;
            int gEnd;
            
            bool hasStatus = false;
            bool hasControl = false;

            string currentGroup = rows[0]["PointName2"] as string;
            for (int i = 0; i < rows.Length; i++)
            {
                if ((string)rows[i]["PointName2"] == currentGroup)
                {

                    if ((string)rows[i]["PointType"] == "T_IND")
                    {
                        hasStatus = true;
                    }
                    else if ((string)rows[i]["PointType"] == "T_CTL")
                    {
                        hasControl = true;
                    }
                }
                else //we are starting a new point group
                {
                    //first process the last point group
                    gEnd = i - 1;
                    if (hasStatus && hasControl)
                    {
                        SetCombinedStatusType(rows, gStart, gEnd, true);
                    }
                    else
                    {
                        SetCombinedStatusType(rows, gStart, gEnd, false);
                    }

                    //now start a new point group
                    gStart = i;
                    hasStatus = hasControl = false;
                    currentGroup = (string)rows[i]["PointName2"];
                    if ((string)rows[i]["PointType"] == "T_IND")
                    {
                        hasStatus = true;
                    }
                    else if ((string)rows[i]["PointType"] == "T_CTL")
                    {
                        hasControl = true;
                    }
                }
            }
            //remember to process the last group!
            //TODO probably shouold do some bounds checking here
            gEnd = rows.Length - 1;
            if (hasStatus && hasControl)
            {
                SetCombinedStatusType(rows, gStart, gEnd, true);
            }
            else
            {
                SetCombinedStatusType(rows, gStart, gEnd, false);
            }
        }

        private void SetCombinedStatusType(DataRow[] rows, int gStart, int gEnd, bool mode)
        {
            for (int i = gStart; i<=gEnd; i++)
            {
                //mode is true for T_I&C processing, otherwise false
                if (mode)
                {
                    rows[i]["PointType2"] = rows[i]["PointType"];
                    rows[i]["PointType"] = "T_I&C";
                }
                if (mode || (rows[i]["PointType"] as string) == "T_R/L" || (rows[i]["PointType"] as string) == "T_IND" || (rows[i]["PointType"] as string) == "T_CTL")
                {
                    string temp = rows[i]["PointName2"] as string;
                    temp = temp.Replace("!", "").Trim();

                    rows[i]["PointName2"] = rows[i]["PointName"];
                    rows[i]["PointName"] = temp;
                }
            }
        }

        public void FetchGenericTags(DataTable dt, string file)
        {
            if (_genericTags == null)
            {
                using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream($"{_options.Input}\\_newpoints.csv", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
                {
                    _genericTags = new DataTable();
                    _genericTags.Columns.Add("Tag");
                    _genericTags.Columns.Add("ItemName");
                    _genericTags.Columns.Add("Type");
                    _genericTags.Columns.Add("MinEU");
                    _genericTags.Columns.Add("MaxEU");
                    _genericTags.Columns.Add("MinRaw");
                    _genericTags.Columns.Add("MaxRaw");
                    _genericTags.Columns.Add("Units");
                    _genericTags.Columns.Add("OnLabel");
                    _genericTags.Columns.Add("OffLabel");

                    while (csv.ReadNextRecord())
                    {
                        if (csv["Skip"].ToLower() == "true")
                            continue;
                        string[] locations = (csv["Locations"] as string).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var loc in locations)
                        {
                            var r = _genericTags.NewRow();
                            r["Tag"] = $"{loc}{csv["TagName"]}";
                            r["ItemName"] = csv["ItemName"];
                            r["Type"] = csv["Type"];
                            r["MinEU"] = csv["MinEU"];
                            r["MaxEU"] = csv["MaxEU"];
                            r["MinRaw"] = csv["MinRaw"];
                            r["MaxRaw"] = csv["MaxRaw"];
                            r["OnLabel"] = csv["OnLabel"];
                            r["OffLabel"] = csv["OffLabel"];
                            r["Units"] = csv["Units"];
                            _genericTags.Rows.Add(r);
                        }
                    }
                }
            }

            DataRow[] result;
            switch (file)
            {
                case _memorydisc:
                    result = _genericTags.Select("Type IN ('C_IND', 'M_CTL')");
                    break;
                case _iodisc:
                    result = _genericTags.Select("Type IN ('T_IND','T_I&C', 'T_CTL', 'T_R/L')");
                    break;
                case _memoryreal:
                    result = _genericTags.Select("Type IN ('C_ANLG')");
                    break;
                case _ioreal:
                    result = _genericTags.Select("Type IN ('T_ANLG', 'T_STPNT')");
                    break;
                default:
                    result = new DataRow[0];
                    break;
            }
            foreach (var row in result)
            {
                var r = dt.NewRow();
                var tag = row["Tag"] as string;
                r["OldTag"] = tag;
                r["PointType"] = row["Type"];
                if (dt.Columns.Contains("ItemName"))
                {
                    r["ItemName"] = row["ItemName"];
                }
                r["Location"] = tag.Substring(0, tag.IndexOf('_'));
                if (dt.Columns.Contains("MinEu"))
                {
                    r["MinEU"] = row["MinEU"];
                    r["MaxEU"] = row["MaxEU"];
                    r["MinRaw"] = row["MinRaw"];
                    r["MaxRaw"] = row["MaxRaw"];
                }
                if (dt.Columns.Contains("OnMsg"))
                {
                    r["OnMsg"] = row["OnLabel"];
                    r["OffMsg"] = row["OffLabel"];
                }
                if (dt.Columns.Contains("Units"))
                {
                    r["Units"] = row["Units"];
                }
                if (dt.Select($"[OldTag] = '{tag}'").Length == 0)
                {
                    dt.Rows.Add(r);
                }
                else
                {
                    _log.Info($"Did not add dynamic tag '{tag}' as it already existed in the database.");
                }
                
            }
        }

        /// <summary>
        /// Add any points that were deferred from io to calculations
        /// </summary>
        /// <param name="file"></param>
        /// <param name="dt"></param>
        private void AddDeferredPoints(string file, DataTable dt)
        {
            if (file == _memorydisc)
            {
                AddDeferredPoints(discretecalcs, dt);
            }
            else if (file == _memoryint)
            {
                AddDeferredPoints(intcalcs, dt);
            }
            else if (file == _memoryreal)
            {
                AddDeferredPoints(realcalcs, dt);
            }
        }

        /// <summary>
        /// Add any points that were deferred from io to calculations
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        private void AddDeferredPoints(DataTable source, DataTable dest)
        {
            if (source == null)
                return;
            for (int i = 0; i < source.Rows.Count; i++)
            {
                var row = dest.NewRow();
                for (int j = 0; j < dest.Columns.Count; j++)
                {
                    if (source.Columns.Contains(dest.Columns[j].ColumnName))
                        row[j] = source.Rows[i][dest.Columns[j].ColumnName];
                }
                dest.Rows.Add(row);
            }
        }

        /// <summary>
        /// Defer a row from an io file to storage for later adding to a memory tag DataTable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="dataRow"></param>
        /// <param name="file"></param>
        private void DeferRowToMemoryFile(DataTable dataTable, DataRow dataRow, string file)
        {
            if (file == _iodisc)
            {
                if (discretecalcs == null)
                    CreateDtFromTemplate(ref discretecalcs, dataTable);
                CopyRowToDt(discretecalcs, dataRow);
            }
            else if (file == _ioint)
            {
                if (intcalcs == null)
                    CreateDtFromTemplate(ref intcalcs, dataTable);
                CopyRowToDt(intcalcs, dataRow);
            }
            else if (file == _ioreal)
            {
                if (realcalcs == null)
                    CreateDtFromTemplate(ref realcalcs, dataTable);
                CopyRowToDt(realcalcs, dataRow);
            }
        }

        /// <summary>
        /// Copy the @row to the @dataTable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="dataRow"></param>
        private void CopyRowToDt(DataTable dataTable, DataRow dataRow)
        {
            dataTable.Rows.Add(dataRow.ItemArray);
        }

        /// <summary>
        /// Create a new DataTable from a template DataTable
        /// </summary>
        /// <param name="newTable"></param>
        /// <param name="templateTable"></param>
        private void CreateDtFromTemplate(ref DataTable newTable, DataTable templateTable)
        {
            newTable = new DataTable();
            foreach (DataColumn col in templateTable.Columns)
            {
                newTable.Columns.Add(col.ColumnName, col.DataType);
            }
        }

        /// <summary>
        /// Export the summary point table to csv
        /// </summary>
        internal void ExportCompleteTagList()
        {
            Util.ExportDatatable(_allPoints.DefaultView.ToTable(), $"{_outputPath}\\export_AllPoints.csv");
        }

        private string GetRemoteLocalPoint (PointDetail point)
        {
            if (string.IsNullOrWhiteSpace(point.PointGroup) || point.PointType != "T_CTL")//this is before controls are combined into T_I&C
            {
                return "";
            }
            //Regex r = new Regex(@"(((CB\s|DIS\s|LBS\s)[A-Z]{1,3}[0-9]{2,3})|((CB|DIS)[0-9]{1,4}))$");//transpower doesn't have supervisory!
            Regex r = new Regex(@"(CB\s|DIS\s|LBS\s)[A-Z]{1,3}[0-9]{2,3}$");
            var p2 = point.PointGroup.TrimEnd("!").Trim();
            var m = r.Match(p2);
            if (m.Success)
            {
                return p2 + " Supervisory";
            }
            return "";
        }

        private string GetRLFeedbackPoint (string point)
        {
            if (string.IsNullOrWhiteSpace(point))
            {
                return "";
            }
            if (point.EndsWith("OLTC"))
                return point.Substring(0, point.Length - 4) + "Tap Position";
            else return "";
        }
    }
}
