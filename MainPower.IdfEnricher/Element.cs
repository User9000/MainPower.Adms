using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    abstract internal class Element : ErrorReporter
    {

        internal XElement Node { get; private set; }
        internal Group ParentGroup { get; private set; }
        internal string Id { get; private set; }
        internal string Name { get; private set; }

        protected const string IDF_ELEMENT_NAME = "name";
        protected const string IDF_ELEMENT_ID = "id";
        protected const string IDF_ELEMENT_AOR_GROUP = "aorGroup";
        protected const string IDF_DEVICE_NOMSTATE1 = "nominalState1";
        protected const string IDF_DEVICE_NOMSTATE2 = "nominalState2";
        protected const string IDF_DEVICE_NOMSTATE3 = "nominalState3";
        protected const string IDF_DEVICE_INSUBSTATION = "inSubstation";

        protected const string GIS_T1_ASSET = "mpwr_t1_asset_nbr";
        protected const string GIS_SWITCH_TYPE = "mpwr_gis_switch_type";
        protected const string GIS_FUSE_RATING = "mpwr_fuse_rating";

        protected const string IDF_TRUE = "True";
        protected const string IDF_FALSE = "False";

        protected const string AOR_DEFAULT = "1";

        protected const string SCADA_NAME = "Name";

        internal Element(XElement node, Group processor)
        {
            Node = node;
            ParentGroup = processor;
            Id = node.Attribute(IDF_ELEMENT_ID).Value;
            Name = node.Attribute(IDF_ELEMENT_NAME).Value;
        }

        abstract internal void Process();

        protected override void Debug(string message, [CallerMemberName]string caller = "")
        {
            _log.Debug(FormatLogString(LogLevel.Debug, $"{GetType().Name}\\{caller}", Id, Name, message));
        }
        protected override void Info(string message, [CallerMemberName]string caller = "")
        {
            _log.Info(FormatLogString(LogLevel.Info, $"{GetType().Name}\\{caller}", Id, Name, message));
        }
        protected override void Warn(string message, [CallerMemberName]string caller = "")
        {
            _log.Warn(FormatLogString(LogLevel.Warn, $"{GetType().Name}\\{caller}", Id, Name, message));
        }
        protected override void Error(string message, [CallerMemberName]string caller = "")
        {
            _log.Error(FormatLogString(LogLevel.Error, $"{GetType().Name}\\{caller}", Id, Name, message));
        }
        protected override void Fatal(string message, [CallerMemberName]string caller = "")
        {
            _log.Fatal(FormatLogString(LogLevel.Fatal, $"{GetType().Name}\\{caller}", Id, Name, message));
        }
    }

}
