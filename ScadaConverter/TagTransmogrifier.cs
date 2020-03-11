using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MainPower.Adms.ScadaConverter
{
    /// <summary>
    /// The TagTransmogrifier converts an InTouch tag name, and converts it into an OSI PointName
    /// </summary>
    public class TagTransmogrifier
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //the input path where the configurations files can be found
        private readonly string _input;
        //deviceregexes: a list of regular expressions to use to extract device names from tag names
        private readonly string[] _deviceregexs;
        //tagmap: key is the search string, value is (replacement tagname, priority of match)
        //eg a more specific match will have a higher priority and should take preference if there is a conflict
        private Dictionary<string, TagMapInfo> _tagMap = new Dictionary<string, TagMapInfo>();
        //tag overrides: key is the intouch tag to match, value is the replacement tagname (or empty string to remove from database)
        private Dictionary<string, TagOverrideInfo> _tagOverrides = new Dictionary<string, TagOverrideInfo>();
        //rtunames: key is the InTouch accessname to find, value is (rtu name to replace AccessName, short location code to use in tagnames, device name override)
        private Dictionary<string, RtuInfo> _rtuNames = new Dictionary<string, RtuInfo>();
        //devicemap: key is "LOC.DEVICE", value is NEW DEVICE
        private Dictionary<string, string> _deviceOverrides = new Dictionary<string, string>();
        //locationStationMap: maps location short codes to station names
        private Dictionary<string, string> _locationToStationMap = new Dictionary<string, string>();
        //rtu info:  key=DO.Index (where DO = the point type (DI/DO/AI/AO)
        private Dictionary<string, RtuTagInfo> _rtuTags = new Dictionary<string, RtuTagInfo>();
        //global rename switchgear:  key=old switch number, value= new switch number
        private Dictionary<string, string> _grswitchgear = new Dictionary<string, string>();
        //global rename locations: key= old location, value = new locations
        private Dictionary<string, string> _grlocations = new Dictionary<string, string>();


        /// <summary>
        /// Load the information from the configuration files
        /// </summary>
        /// <param name="inputFileLocation"></param>
        public TagTransmogrifier(string inputFileLocation)
        {
            _input = inputFileLocation;
            _deviceregexs = File.ReadAllLines($"{_input}_deviceexpressions.csv");
            LoadTagMap($"{_input}_tagmap.csv");
            LoadTagOverrides($"{_input}_tagoverrides.csv");
            LoadRtus($"{_input}_rtus.csv");
            LoadDeviceOverrides($"{_input}_deviceoverrides.csv");
            LoadRtuTags($"{_input}_rtutaginfo.csv");
            LoadGlobalRenames($"{_input}_globalrenames.csv");
        }

        #region Load input information
        /// <summary>
        /// Load all the RTU tag information so we can export the equipment type to OSI
        /// </summary>
        /// <param name="tagFile"></param>
        private void LoadRtuTags(string tagFile)
        {
            _rtuTags.Clear();
            
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(tagFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                while (csv.ReadNextRecord())
                {
                    RtuTagInfo rtu = new RtuTagInfo
                    {
                        TagName = csv["TagName"],
                        Equipment = csv["RelayName"],
                        RtuConfig = csv["RTUName"],
                        Type = csv["Type"],
                        Index = csv["Index"]
                    };
                    var key = $"{rtu.RtuConfig}.{rtu.Type}.{rtu.Index}";

                    if (_rtuTags.ContainsKey(key))
                    {
                        _log.Error($"Dictionary RtuInfo already contains the key {key}");
                    }
                    else
                    {
                        _rtuTags.Add(key, rtu);
                    }
                }
            }
        }

        /// <summary>
        /// Load the global rename information.  This is used to map old SCADA location identifiers to the new standard identifiers
        /// It is also used to prepend the appropriate switchgear abbreviation to the switch number (eg CB, DIS, ES)
        /// </summary>
        /// <param name="file"></param>
        private void LoadGlobalRenames(string file)
        {
            _grswitchgear.Clear();
            _grlocations.Clear();

            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                while (csv.ReadNextRecord())
                {
                    if (csv["OldLocation"] !="")
                    {
                        _grlocations.Add(csv["OldLocation"], csv["NewLocation"]);
                    }
                    if (csv["OldSwitch"] != "")
                    {
                        _grswitchgear.Add(csv["OldSwitch"], csv["NewSwitch"]);
                    }
                }
            }
        }

        /// <summary>
        /// Load the device override information.  This is used map the portions of matched information from the tagname
        /// into a nicer string for OSI.  eg converts "Ion1_RTU" into "X21 Met Comms"
        /// </summary>
        /// <param name="file"></param>
        private void LoadDeviceOverrides(string file)
        {
            _deviceOverrides.Clear();
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                while (csv.ReadNextRecord())
                {
                    var key = $"{csv["Location"]}.{csv["Device"]}";
                    var value = csv["NewDevice"];
                    if (_deviceOverrides.ContainsKey(key))
                    {
                        _log.Error($"Dictionary DeviceOverrides already contains the key {key}");
                    }
                    else
                    {
                        _deviceOverrides.Add(key, value);
                    }
                }
            }
        }

        /// <summary>
        /// Load the RTU information
        /// </summary>
        /// <param name="file"></param>
        private void LoadRtus(string file)
        {
            _rtuNames.Clear();
            _locationToStationMap.Clear();
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true, ',', '"', '\'', '%', ValueTrimmingOptions.All))
            {
                while (csv.ReadNextRecord())
                {
                    var itt = csv["InTouchTopic"];
                    if (itt != "#NOTREQUIRED")
                    {
                        if (!_rtuNames.ContainsKey(itt))
                        {
                            var rtu = new RtuInfo { Device = csv["Device Override"], Station = csv["OSI Station Name"], Location = csv["Location"], NewRtuName = csv["OSIName"], RtuConfig = csv["RTUConfig"], DefaultEquipment = csv["DefaultEquipment"] };
                            ParseModbusConfig(rtu, csv["ModbusConfig"]);
                            _rtuNames.Add(itt, rtu);
                        }
                        else
                        {
                            _log.Warn($"InTouch Topic {itt} already exists, not adding this one to RTU Names dictionary.");
                        }
                    }

                    var loc = csv["Location"];
                    var station = csv["OSI Station Name"];
                    if (!_locationToStationMap.ContainsKey(loc) && station != "#NOTREQUIRED")
                    {
                        _locationToStationMap.Add(loc, station);
                    }
                }
            }
        }

        private void ParseModbusConfig(RtuInfo rtu, string v)
        {
            Regex r = new Regex(@"\[([0-9]+)-([0-9]+):([0-9]+):([0-9])\]");
            foreach (Match m in r.Matches(v))
            {
                rtu.ModbusConfiguration.Add((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value)));
            }
        }

        /// <summary>
        /// Load the tag map information
        /// </summary>
        /// <param name="file"></param>
        private void LoadTagMap(string file)
        {
            _tagMap.Clear();
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.Default), true))
            {
                while (csv.ReadNextRecord())
                {
                    List<string> list = csv["MatchString"].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var l in list)
                    {
                        string pr = csv["Precedence"];
                        int priority;
                        if (pr == "")
                        {
                            priority = 500;
                        }
                        else
                        {
                            priority = int.Parse(pr);
                        }
                        var label1 = SplitLabels(csv["Label1"]);
                        var label2 = SplitLabels(csv["Label2"]);
                        TagMapInfo tmi = new TagMapInfo
                        {
                            TagName = csv["Property"],
                            Priority = priority,
                            OsiType = csv["OSIType"],
                            PointGroup = csv["Group"],
                            Substitutions = new List<(string, string[])>(),
                            Units = csv["Units"],
                            ScaleOverrides = new Dictionary<string, double>(),
                            ForceNormalState = csv["ForceNormalState"],
                            DeviceOverride = csv["DeviceOverride"].Trim(),
                            AlarmGroup = csv["AlarmGroup"],
                            Archive = csv["Archive"],
                            ReasonabilityHigh = csv["ReasonabilityHigh"],
                            ReasonabilityLow = csv["ReasonabilityLow"]
                        };
                        if (csv["ToDelete"] == "TRUE")
                        {
                            tmi.ToDelete = true;
                        }
                        if (label1.Item1 != "")
                            tmi.Substitutions.Add(label1);
                        if (label2.Item1 != "")
                            tmi.Substitutions.Add(label2);
                        if (!string.IsNullOrWhiteSpace(csv["ScalingOverride"]))
                        {
                            var sfs = csv["ScalingOverride"].Split(new char[] { ',' });
                            foreach (var s in sfs)
                            {
                                try
                                {
                                    var vals = s.Split(new char[] { ':' });
                                    if (vals.Length > 2)
                                    {
                                        _log.Error($"Scale factor override entry [{s}] was malformed (too many :'s)");
                                    }
                                    else if (vals.Length == 2)
                                    {
                                        tmi.ScaleOverrides.Add(vals[0], double.Parse(vals[1]));
                                    }
                                    else if (vals.Length == 1)
                                    {
                                        tmi.ScaleOverrides.Add("", double.Parse(vals[0]));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Error($"Scale factor override entry [{s}] was malformed: {ex.Message}");
                                }
                            }
                        }
                        _tagMap.Add(l, tmi);
                    }
                }
            }
        }

        /// <summary>
        /// Load the tag override information
        /// </summary>
        /// <param name="file"></param>
        private void LoadTagOverrides(string file)
        {
            _tagOverrides.Clear();
            using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), true))
            {
                while (csv.ReadNextRecord())
                {
                    if (string.IsNullOrWhiteSpace(csv["OldTag"]))
                        continue;
                    if (!_tagOverrides.ContainsKey(csv["OldTag"]))
                    {
                        var toi = new TagOverrideInfo()
                        {
                            NewTagName = csv["NewTag"],
                            Device = csv["Device"],
                            Location = csv["Location"],
                            Property = csv["Property"],
                            PointType = csv["Type"],
                            OnLabel = csv["OnLabel"],
                            OffLabel = csv["OffLabel"],
                            NormalState = csv["NormalState"],
                            Units = csv["Units"],
                            Archive = csv["Archive"],
                            AlarmGroup = csv["AlarmGroup"]
                        };
                        _tagOverrides.Add(csv["OldTag"], toi);
                    }
                    else
                    {
                        _log.Error($"{csv["OldTag"]} has already been added to the list of tag overrides");
                    }
                }
            }
        }
        #endregion

        #region Get input information
        
        private string GetEquipment(PointDetail ipoint)
        {
            if (_rtuNames.ContainsKey(ipoint.RtuName))
            {
                RtuInfo rtuoi = _rtuNames[ipoint.RtuName];
                if (!string.IsNullOrWhiteSpace(rtuoi.RtuConfig))
                {
                    RtuTagInfo rtui = GetRtuInfo(ipoint, rtuoi.RtuConfig);
                    if (rtui == null)
                    {
                        return rtuoi.DefaultEquipment;
                    }
                    else
                    {
                        //TODO: this should be put somewhere better
                        ipoint.RTUTag = rtui;
                        return rtui.Equipment;
                    }
                }
                else
                {
                    return rtuoi.DefaultEquipment;
                }
            }
            return "";
        }

        private RtuTagInfo GetRtuInfo(PointDetail ipoint, string rtuconfig)
        {
            string key = $"{rtuconfig}.{ConvertDnpAddressToKey(ipoint.Address)}";
            if (_rtuTags.ContainsKey(key))
            {
                return _rtuTags[key];
            }
            else
                return null;
        }

        private string ConvertDnpAddressToKey(string address)
        {
            return $"{Util.DetermineDataType(address)}.{Util.ExtractDnpIndex(address)}";
        }

        private TagOverrideInfo GetTagOverrideInfo(string tagname)
        {
            if (_tagOverrides.ContainsKey(tagname))
                return _tagOverrides[tagname];
            else
                return null;
        }

        private string GetDeviceOverride(PointDetail opoint, PointDetail ipoint)
        {
            if (ipoint.RtuName != null)
            {
                if (_rtuNames.ContainsKey(ipoint.RtuName))
                {
                    var roi = _rtuNames[ipoint.RtuName];
                    if (roi.Device != "")
                        return roi.Device;
                }
            }
            if (_deviceOverrides.ContainsKey($"{opoint.Location}.{opoint.Device}"))
            {
                return _deviceOverrides[$"{opoint.Location}.{opoint.Device}"];
            }
            if (_deviceOverrides.ContainsKey($".{opoint.Device}"))
            {
                return _deviceOverrides[$".{opoint.Device}"];
            }
            return opoint.Device;
        }

        private string GetRtuLocationCode(string v)
        {
            if (_rtuNames.ContainsKey(v))
            {
                return (_rtuNames[v].Location);
            }
            return "";
        }
        
        private string GetRTUNameFromLocation(string location)
        {
            //this is a somewhat expensive operation, but required for generic tags
            foreach (var kvp in _rtuNames)
            {
                if (kvp.Value.Location == location)
                {
                    return kvp.Value.NewRtuName;
                }
            }
            return "";
        }

        private string GetNewRtuName(string v)
        {
            if (v == null)
                return "";
            if (_rtuNames.ContainsKey(v))
            {
                return (_rtuNames[v].NewRtuName);
            }
            return "";
        }

        private string GetRtuDeviceName(string v)
        {
            if (_rtuNames.ContainsKey(v))
            {
                return _rtuNames[v].Device;
            }
            return "";
        }

        private string ExtractDevice(string v)
        {
            foreach (string s in _deviceregexs)
            {
                if (s.Trim().StartsWith("//") || string.IsNullOrWhiteSpace(s))
                    continue;
                Regex r = new Regex(s);
                var m = r.Match(v);
                if (m.Success)
                {
                    return m.Value.Trim(new char[] { ' ', '_' });
                }
            }
            return "";
        }

        private TagMapInfo MatchTag(string tagname)
        {
            TagMapInfo bestmatch = null;
            foreach (var kvp in _tagMap)
            {
                if (tagname.Contains(kvp.Key))
                {
                    if (bestmatch?.Priority > kvp.Value.Priority || bestmatch == null)
                        bestmatch = kvp.Value;
                }
            }
            return bestmatch;
        }

        private (string, string[]) SplitLabels(string str)
        {
            var i = str.IndexOf('=');
            if (i == -1)
            {
                return (str, null);
            }
            var left = str.Substring(0, i).Trim(new char[] { '\'', ' ' });
            var right = str.Substring(i + 1, str.Length - i - 1).Trim();
            var subs = right.Split(new char[] { ',' }).ToList();
            for (int j = 0; j < subs.Count; j++)
            {
                subs[j] = subs[j].ToLower().Trim();
            }
            if (!subs.Contains(left.ToLower().Trim()))
            {
                subs.Add(left.ToLower().Trim());
            }

            return (left, subs.ToArray());
        }

        private string SubstituteLabel(string original, List<(string, string[])> substitutions, bool onlabel)
        {
            //when labels are forced, the first label is the onlabel, and the second label is the offlabel.  Messy, I know.
            //force a label override by ending the substition with '!'
            if (onlabel && substitutions.Count >= 1)
            {
                if (substitutions[0].Item1.EndsWith("!"))
                {
                    return substitutions[0].Item1.Trim(new char[] { ' ', '!' });
                }
            }
            else if (!onlabel && substitutions.Count >= 2)
            {
                if (substitutions[1].Item1.EndsWith("!"))
                {
                    return substitutions[1].Item1.Trim(new char[] { ' ', '!' });
                }
            }

            //if there are no overrides to apply, go through the normal substition process.
            //TODO: white space isn't actually permitted, so we need a way to deal with it :/
            //TODO: should we throw an error here??
            if (string.IsNullOrWhiteSpace(original))
                return "";
            foreach (var sub in substitutions)
            {
                foreach (var s in sub.Item2)
                {
                    if (original.ToLower() == s.ToLower())
                        return sub.Item1.Trim(new char[] { ' ', '!' });
                }
            }
            return original;
        }

        private string GetStationFromLocation(PointDetail p)
        {
            //This is a special hack for southbrook gxp
            if (p.RtuName == "Southbrook")
            {
                if (Util.ExtractDnpIndex(p.Address) >= 20000)
                {
                    return "Southbrook Grid Exit Point";
                }
            }
            else if (p.PointName.Contains("SBK GXP") || p.PointName.Contains("SBK KAI GXP") || p.PointName.Contains("SBK CB202") || p.PointName.Contains("SBK CB152"))
            {
                return "Southbrook Grid Exit Point";
            }
            //dirty hack for lochiel
            else if (p.Location == "LOC E80")
            {
                return "Lochiel Zone Substation";
            }

            if (p.Location == null)
                return null;
            if (_locationToStationMap.ContainsKey(p.Location))
                return _locationToStationMap[p.Location];
            return null;

        }

        private string GetStationFromRtu(PointDetail p)
        {
            if (string.IsNullOrWhiteSpace(p.RtuName))
                return null;

            //This is a special hack for southbrook gxp
            if (p.RtuName == "Southbrook")
            {
                if (Util.ExtractDnpIndex(p.Address) >= 20000)
                {
                    return "Southbrook Grid Exit Point";
                }
            }
            else if (p.PointName.Contains("SBK GXP") || p.PointName.Contains("SBK KAI GXP") || p.PointName.Contains("SBK CB202") || p.PointName.Contains("SBK CB152"))
            {
                return "Southbrook Grid Exit Point";
            }

            
            if (_rtuNames.ContainsKey(p.RtuName))
                return _rtuNames[p.RtuName].Station;
            return null;
        }

        private void ProcessGlobalRenameLocation(PointDetail opoint)
        {
            foreach (var kvp in _grlocations)
            {
                if (opoint.Location == kvp.Key)
                {
                    opoint.Location = kvp.Value;
                }
            }
        }

        private void ProcessGlobalRenameSwitchgear(PointDetail opoint)
        {
            foreach (var kvp in _grswitchgear)
            {
                if (opoint.PointName.Contains(kvp.Key))
                {
                    Regex r = new Regex($"(?<=(^|\\s)){kvp.Key}(?=(\\s|/))");
                    opoint.PointName = r.Replace(opoint.PointName, kvp.Value);
                    opoint.PointGroup = r.Replace(opoint.PointGroup, kvp.Value);
                }
            }
        }

        private void GetScalingOverride(PointDetail opoint, TagMapInfo tmi)
        {
            foreach (var kvp in tmi.ScaleOverrides)
            {
                if (kvp.Key == "") {
                    opoint.ScaleFactor *= kvp.Value;
                    opoint.Offset *= kvp.Value;
                    return;
                }
                if (kvp.Key == opoint.Equipment)
                {
                    opoint.ScaleFactor = kvp.Value;
                    opoint.Offset *= kvp.Value;
                    return;
                }
            }
        }

        private RtuInfo GetRtuInfo(PointDetail ipoint)
        {
            if (ipoint?.RtuName == null)
                return null;
            if (_rtuNames.ContainsKey(ipoint.RtuName))
            {
                return _rtuNames[ipoint.RtuName];
            }
            return null;
        }
        #endregion

        private string GetPointTypeFromAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return "";
            if (address.StartsWith("1."))
                return "T_IND";
            if (address.StartsWith("30."))
                return "T_ANLG";
            if (address.StartsWith("31."))
                return "T_ANLG";
            if (address.StartsWith("32."))
                return "T_ANLG";
            if (address.StartsWith("12."))
                return "T_CTL";
            if (address.StartsWith("41."))
                return "T_STPNT";
            if (address.StartsWith("20."))
                return "T_ACCUM";
            return "";
        }

        private void AssignDnpControlCodes(PointDetail opoint)
        {
            if (string.IsNullOrWhiteSpace(opoint.Address))
                return;

            //TODO: this can probably be easily done in a single regex....
            Regex r = new Regex(@"12\.[0-9]+\.[0-9]+");
            var m = r.Match(opoint.Address);
            if (m.Success)
            {
                if (m.Value == opoint.Address)
                {
                    //then we have a vanilla dnp control address - assign the default latch on/latch off codes
                    if (opoint.OnLabel == "-" && opoint.OffLabel != "-")
                    {
                        opoint.DnpControlOnCode = "N/A";
                        opoint.DnpControlOffCode = "0x03";
                    }
                    else if (opoint.OnLabel != "-" && opoint.OffLabel == "-")
                    {
                        opoint.DnpControlOnCode = "0x03";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else
                    {
                        opoint.DnpControlOnCode = "0x03";
                        opoint.DnpControlOffCode = "0x04";
                    }

                }
                else
                {

                    if (opoint.Address.EndsWith("pulse.close.dop"))
                    {
                        opoint.DnpControlOnCode = "0x41";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else if (opoint.Address.EndsWith("pulse.close.sop"))
                    {
                        opoint.DnpControlOnCode = "0x41";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else if (opoint.Address.EndsWith("pulse.trip.dop"))
                    {
                        opoint.DnpControlOnCode = "0x81";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else if (opoint.Address.EndsWith("pulse.trip.sop"))
                    {
                        opoint.DnpControlOnCode = "0x81";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else if (opoint.Address.EndsWith("pulse.sop"))
                    {
                        opoint.DnpControlOnCode = "0x01";
                        opoint.DnpControlOffCode = "N/A";
                    }
                    else
                    {
                        _log.Warn($"Unknown DNP control suffix present on point {opoint.PointName} (address: {opoint.Address}).");
                    }
                    opoint.Address = Util.SanitizeDnpAddress(opoint.Address);
                }
            }
        }

        public PointDetail GetPoint(PointDetail ipoint, bool discrete, bool io)
        {
            //TODO: surely we can work out if discrete or io from info provided?

            //in case we need to trace a point
            //if (ipoint.PointName.StartsWith(""))
            //    Debugger.Break();

            if (ipoint == null)
                throw new ArgumentNullException();

            PointDetail opoint = new PointDetail();
            RtuInfo rtui = null;

            #region Step 1: Copy default data from the input point to the output point
            opoint.Address = ipoint.Address;
            opoint.Units = ipoint.Units;
            opoint.PointType = GetPointTypeFromAddress(ipoint.Address);
            opoint.ScaleFactor = ipoint.ScaleFactor;
            opoint.Offset = ipoint.Offset;
            opoint.OnLabel = ipoint.OnLabel;
            opoint.OffLabel = ipoint.OffLabel;

            //copy limits
            opoint.ELimits = ipoint.ELimits;
            opoint.HighLimits = ipoint.HighLimits;
            opoint.LowLimits = ipoint.LowLimits;

            //set default reasonability limits
            opoint.ELimits[PointDetail.REASON] = true;
            opoint.HighLimits[PointDetail.REASON] = "999999";
            opoint.LowLimits[PointDetail.REASON] = "-999999";
            #endregion

            #region Step 2: Remove points not required in OSI, and rename points which are misspelled etc using information from the TOI
            //tagname is the preprocessed name of the tag to use for conversion.  ipoint.PointName remains the original name of the tag
            string tagname = ipoint.PointName;
            TagOverrideInfo toi = GetTagOverrideInfo(ipoint.PointName);

            if (toi != null)
            {
                if (string.IsNullOrWhiteSpace(toi.NewTagName))
                {
                    return null;
                }
                else
                {
                    tagname = toi.NewTagName;
                    if (tagname != ipoint.PointName)
                        _log.Info($"Renaming {ipoint.PointName} to {tagname}.");
                }
            }
            #endregion

            #region Step 3: Find the RTU name for the tag if the point is an IO type, assign equipment name
            if (io)
            {
                rtui = GetRtuInfo(ipoint);
                //get the new RTU name from the AccessName
                opoint.RtuName = GetNewRtuName(ipoint.RtuName);
                if (opoint.RtuName == "") //Generic tags do not have an RTU, but are still IO.  Work out the RTU name from the location (provided by newtag file)
                    opoint.RtuName = ipoint.RtuName = GetRTUNameFromLocation(ipoint.Location);
                //add the equipment information
                opoint.Equipment = GetEquipment(ipoint);
                //TODO: RTUTag is assigned above, should be done better...
                opoint.RTUTag = ipoint.RTUTag;
            }
            #endregion

            #region Step 4: Detemine the Location name.  From the tag if possible, otherwise infer the location from the RTU name
            //valid locations are two or three letter abbreviations, or KS1 (Kaiapoi S1) or NuG4 (Nulec generics)
            //blank locations are also valid - standalone switchgear will have blank locations.
            var regex = new Regex("^(([A-Z]{2,3})|(KS1)|(NuG4))(?=_)");
            var match = regex.Match(tagname);
            if (match.Success)
            {
                opoint.Location = match.Value;
            }
            else if (io)
            {
                opoint.Location = GetRtuLocationCode(ipoint.RtuName);
                if (opoint.Location == "#NOTREQUIRED")
                {
                    _log.Warn($"Skipping point {ipoint.PointName} as source location was from an RTU which is listed as NOTREQUIRED");
                    return null;
                }
            }
            else
            {
                opoint.Location = "";
            }
            #endregion

            #region Step 5: Determine the device name 
            //First try pattern matching from the _deviceexpressions file
            //Devices in this way usually match the location as well
            opoint.Device = ExtractDevice(tagname);

            //TODO: update the device expressions to use lookbehind expressions to deal with this
            //If device is the same as location, then set device is null (ie a generic property of the location)
            if (opoint.Device == opoint.Location && !string.IsNullOrWhiteSpace(opoint.Device))
            {
                opoint.Device = "";
            }
            //if the device starts with the location, then remove it
            if (opoint.Device.StartsWith($"{opoint.Location}_"))
            {
                opoint.Device = opoint.Device.Remove(0, opoint.Location.Length + 1);
            }

            //Fine tune the device further by comparing from the device map, and substituting for OSI
            opoint.Device = GetDeviceOverride(opoint, ipoint);
            #endregion

            #region Step 6: Match the tagname against the tagmap, and apply all the properties to the output point
            TagMapInfo tmi = MatchTag(tagname);
            if (tmi != null)
            {
                if (tmi.ToDelete)
                    return null;
                //Assign the OSI formatted property name
                opoint.Property = tmi.TagName;
                //Assign the default point type for the property (if not blank)
                if (!string.IsNullOrEmpty(tmi.OsiType))
                    opoint.PointType = tmi.OsiType;
                //Assign the default point group for the property
                opoint.PointGroup = tmi.PointGroup;
                //Assign the archive, alarm,  attributes
                opoint.AlarmGroup = tmi.AlarmGroup;
                opoint.Archive = tmi.Archive;
                if (!string.IsNullOrWhiteSpace(tmi.ReasonabilityHigh))
                    opoint.HighLimits[PointDetail.REASON] = tmi.ReasonabilityHigh;
                if (!string.IsNullOrWhiteSpace(tmi.ReasonabilityLow))
                    opoint.LowLimits[PointDetail.REASON] = tmi.ReasonabilityLow;

                //Assign device override
                if (!string.IsNullOrEmpty(tmi.DeviceOverride))
                {
                    if (tmi.DeviceOverride == "!") //set the device to blank
                        opoint.Device = "";
                    else if (tmi.DeviceOverride.StartsWith("<"))
                    {
                        //< means remove the string from the start or end of the device name
                        var sTrim = tmi.DeviceOverride.TrimStart("<");
                        opoint.Device = opoint.Device.TrimStart(sTrim).TrimEnd(sTrim).Trim();
                    }
                    else if (tmi.DeviceOverride.StartsWith("+"))
                    {
                        //+ means add the string to the end of the device name
                        var sTrim = tmi.DeviceOverride.TrimStart("+");
                        opoint.Device = opoint.Device + sTrim;
                    }
                    else if (tmi.DeviceOverride.StartsWith("~"))
                    {
                        //~ means add the string to the end of the device name. But not for standalone switchgear.
                        if (opoint.Equipment != "NULEC" && opoint.Equipment != "NOJA" && opoint.Equipment != "ENTEC" && opoint.Equipment != "Shinsung")
                        {
                            var sTrim = tmi.DeviceOverride.TrimStart("~");
                            opoint.Device = opoint.Device + sTrim;
                        }
                    }
                    else
                    {
                        opoint.Device = tmi.DeviceOverride;
                    }
                }

                //for discrete points, assign the default On/Off labels according to the override substitutions in the tagmap for the property
                if (discrete)
                {
                    //we won't carry over inverted points.
                    if (ipoint.Inverted)
                    {
                        _log.Warn($"Point {ipoint.PointName} is inverted");
                        opoint.OffLabel = SubstituteLabel(ipoint.OnLabel, tmi.Substitutions, true);
                        opoint.OnLabel = SubstituteLabel(ipoint.OffLabel, tmi.Substitutions, false);
                    }
                    else
                    {
                        opoint.OnLabel = SubstituteLabel(ipoint.OnLabel, tmi.Substitutions, true);
                        opoint.OffLabel = SubstituteLabel(ipoint.OffLabel, tmi.Substitutions, false);

                    }
                }
                //for analog points, apply units, scale factor override
                if (!discrete)
                {

                    if (tmi.Units != "" && opoint.Units != tmi.Units)
                    {
                        opoint.Units = tmi.Units == "!" ? "" : tmi.Units;
                        _log.Info($"Applying units overide to {ipoint.PointName} from {ipoint.Units} to {opoint.Units}.");
                    }
                    if (tmi.ScaleOverrides?.Count > 0)
                    {
                        GetScalingOverride(opoint, tmi);
                    }
                }
            }
            #endregion

            #region Step 7: Apply individual point overrides from tagoverrides file (except normal state)
            if (toi != null)
            {
                if (!string.IsNullOrWhiteSpace(toi.Location))
                    opoint.Location = toi.Location;
                if (!string.IsNullOrWhiteSpace(toi.Device))
                    opoint.Device = toi.Device == "!" ? "" : toi.Device;
                if (!string.IsNullOrWhiteSpace(toi.Property))
                {
                    opoint.Property = toi.Property;
                    //wipe out pointgroup if the property is overridden
                    opoint.PointGroup = "";
                }
                if (!string.IsNullOrWhiteSpace(toi.PointType))
                    opoint.PointType = toi.PointType;
                if (!string.IsNullOrWhiteSpace(toi.OnLabel))
                    opoint.OnLabel = toi.OnLabel;
                if (!string.IsNullOrWhiteSpace(toi.OffLabel))
                    opoint.OffLabel = toi.OffLabel;
                if (!string.IsNullOrWhiteSpace(toi.Units))
                    opoint.Units = toi.Units == "!" ? "" : toi.Units;
                if (!string.IsNullOrWhiteSpace(toi.AlarmGroup))
                    opoint.AlarmGroup = toi.AlarmGroup;
                if (!string.IsNullOrWhiteSpace(toi.Archive))
                    opoint.Archive = toi.Archive;
                if (!string.IsNullOrWhiteSpace(toi.ReasonabilityHigh))
                    opoint.HighLimits[PointDetail.REASON] = toi.ReasonabilityHigh;
                if (!string.IsNullOrWhiteSpace(toi.ReasonabilityLow))
                    opoint.LowLimits[PointDetail.REASON] = toi.ReasonabilityLow;
            }
            #endregion

            #region Step 8: Assign normal state for discrete points
            if (discrete)
            {
                if (opoint.OnLabel == "")
                    _log.Error($"Point {ipoint.PointName} has a blank OnLabel");
                if (opoint.OffLabel == "")
                    _log.Error($"Point {ipoint.PointName} has a blank OffLabel");
                //Control points do not have a normal state
                if (opoint.PointType == "T_CTL" || opoint.PointType == "T_R/L")
                {
                    opoint.NormalState = "";
                }
                //else apply from the overrides
                else if (!string.IsNullOrWhiteSpace(toi?.NormalState))
                    opoint.NormalState = toi.NormalState == "!" ? "" : toi.NormalState;
                //else take if from the tagmap 
                else if (tmi?.ForceNormalState == "0")
                {
                    opoint.NormalState = "";
                }
                else if (tmi?.ForceNormalState == "1")
                {
                    //TODO: will fail if force normal state is set and labels are not set...
                    //TODO: this is a bit gross, can we tidy it up please??
                    if (opoint.OnLabel == tmi?.Substitutions[0].Item1.Trim(new char[] { ' ', '!' }) || opoint.OffLabel == tmi?.Substitutions[0].Item1.Trim(new char[] { ' ', '!' }))
                        opoint.NormalState = tmi?.Substitutions[0].Item1.Trim(new char[] { ' ', '!' });
                    else
                        _log.Warn($"Normal state for point {ipoint.PointName} was not set (Forced state didn't match either on or off label)");
                }
                else if (tmi?.ForceNormalState == "2")
                {
                    //TODO: will fail if force normal state is set and labels are not set...
                    //TODO: this is a bit gross, can we tidy it up please??
                    if (opoint.OnLabel == tmi?.Substitutions[1].Item1.Trim(new char[] { ' ', '!' }) || opoint.OffLabel == tmi?.Substitutions[1].Item1.Trim(new char[] { ' ', '!' }))
                        opoint.NormalState = tmi?.Substitutions[1].Item1.Trim(new char[] { ' ', '!' });
                    else
                        _log.Warn($"Normal state for point {ipoint.PointName} was not set (Forced state didn't match either on or off label)");
                }
                //otherwise we will use the normal state as defined from the InitialState in InTouch
                else
                {
                    if (ipoint.NormalState == "On")
                        opoint.NormalState = opoint.OnLabel;
                    else
                        opoint.NormalState = opoint.OffLabel;
                }
            }
            #endregion

            #region Step 9: Apply extra final override for device names based on the RTU name
            if (io)
            {
                //get the new device name from the AccessName
                var newdev = GetRtuDeviceName(opoint.RtuName);
                //if there is a valid override, then poke it in
                if (!string.IsNullOrWhiteSpace(newdev))
                    opoint.Device = newdev;
            }
            #endregion

            #region Step 10: Generate the point and group names
            //Apply rename rules to the location
            ProcessGlobalRenameLocation(opoint);

            //Identify points which we couldn't convert
            if (string.IsNullOrWhiteSpace(opoint.Property))
            {
                opoint.PointName = "#CouldntConvert";
                _log.Error($"Could not convert point {ipoint.PointName} to the OSI format");
                return null;
            }
            //Combine the location device and proerty into the point name
            else
            {
                opoint.PointName = $"{opoint.Location} {opoint.Device} {opoint.Property}".Replace("  ", " ").Trim();
            }

            //Combine the location device and pointgroup into the group name
            if (!string.IsNullOrWhiteSpace(opoint.PointGroup))
            {
                opoint.PointGroup = $"{opoint.Location} {opoint.Device} {opoint.PointGroup}".Replace("  ", " ").Trim();
            }
            //Apply renaming rules for switchgear (will rename the point and group names)
            ProcessGlobalRenameSwitchgear(opoint);
            #endregion

            #region Step 11: Fill out the station name
            var station = GetStationFromLocation(opoint);
            if (station != null)
            {
                opoint.StationName = station;
            }
            else
            {
                station = GetStationFromRtu(ipoint);
                if (station != null)
                {
                    opoint.StationName = station;
                }
                else
                {
                    _log.Error($"Could not determine station name for point {ipoint.PointName}");
                }
            }
            #endregion

            #region Step 12: Make sure that non io points do not have an io point type, convert those that do
            if (!io || (io && opoint?.RtuName == "Calculation"))
            {
                //for the benefit of the next section
                io = false;
                switch (opoint?.PointType)
                {
                    case "T_CTL":
                        opoint.PointType = "M_CTL";
                        break;
                    case "T_IND":
                        opoint.PointType = "C_IND";
                        break;
                    case "T_ANLG":
                        opoint.PointType = "C_ANLG";
                        break;
                    case "T_ACCUM":
                        opoint.PointType = "C_ACCUM";
                        break;
                    case "T_STPNT":
                        opoint.PointType = "M_STPNT";
                        break;
                }
            }
            #endregion

            #region Step 13: Process protocol specific data
            if (io)
            {
                AssignDnpControlCodes(opoint);
                ProcessModbusRegister(opoint, rtui);

                //should deal with this properly, but just going to chuck a dirty hack in here
                if (opoint.Address.StartsWith("20."))
                {
                    opoint.PointType = "T_ACCUM";
                }
            }
            #endregion

            #region Step 14: Validate alarm parameters
            if (IsAnalog(opoint.PointType))
            {
                //we must have reasonability limits set for all analogs
                if (string.IsNullOrWhiteSpace(opoint.LowLimits[PointDetail.REASON]))
                {
                    _log.Warn($"Reasonability Low not set for [{ipoint.PointName}]");
                }
                if (string.IsNullOrWhiteSpace(opoint.HighLimits[PointDetail.REASON]))
                {
                    _log.Warn($"Reasonability High not set for [{ipoint.PointName}]");
                }
                //for each limit, fill out the values if they are blank
                //if the limit is disabled, set the blank value to 0
                //if the limit is enabled, set the blank value to the reasonability limit
                for (int i = 0; i < 4; i++)
                {
                    if (opoint.ELimits[i])
                    {
                        if (string.IsNullOrWhiteSpace(opoint.LowLimits[i]))
                        {
                            opoint.LowLimits[i] = opoint.LowLimits[PointDetail.REASON];
                        }
                        if (string.IsNullOrWhiteSpace(opoint.HighLimits[i]))
                        {
                            opoint.HighLimits[i] = opoint.HighLimits[PointDetail.REASON];
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(opoint.LowLimits[i]))
                        {
                            opoint.LowLimits[i] = "0";
                        }
                        if (string.IsNullOrWhiteSpace(opoint.HighLimits[i]))
                        {
                            opoint.HighLimits[i] = "0";
                        }
                    }
                }
            }
            #endregion

            #region Step 15: Validate Alarm Groups and Archive flags
            if (string.IsNullOrWhiteSpace(opoint.Archive))
                _log.Warn($"Archive flag is not set for [{ipoint.PointName}]");
            if (string.IsNullOrWhiteSpace(opoint.AlarmGroup))
                _log.Warn($"Alarm group is not set for [{ipoint.PointName}]");
            #endregion

            return opoint;
        }

        private void ProcessModbusRegister(PointDetail opoint, RtuInfo rtu)
        {
            if (!string.IsNullOrWhiteSpace(opoint.Address))
            {
                if (int.TryParse(opoint.Address, out int register))
                {
                    if (rtu?.ModbusConfiguration?.Count > 0)
                    {
                        foreach (var config in rtu.ModbusConfiguration)
                        {
                            if (register >= config.Item1 && register <= config.Item2)
                            {
                                UInt16 address = (UInt16)(register - config.Item3 + 1);
                                Int16 a2 = unchecked((Int16)address);
                                opoint.Address = a2.ToString();
                                opoint.ModbusFunctionCode = config.Item4.ToString();
                                return;
                            }
                        }
                        _log.Warn($"Address {opoint.Address} for point {opoint.PointName} looked like a modbus address, but there was no associated modbus configuration.");
                    }
                    else
                    {
                        _log.Warn($"Address {opoint.Address} for point {opoint.PointName} looked like a modbus address, but there was no associated modbus configuration.");
                    }
                }
            }
        }

        private bool IsAnalog(string ptype)
        {
            if (ptype == "T_ANLG" || ptype == "C_ANLG" || ptype == "M_ANLG" || ptype == "T_STPNT" || ptype == "M_STPNT" || ptype == "C_STPNT" || ptype == "T_ACCUM" || ptype == "C_ACCUM" || ptype == "M_ACCUM")
                return true;
            return false;
        }
    }
}
