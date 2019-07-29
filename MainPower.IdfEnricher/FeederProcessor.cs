using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class FeederProcessor : DeviceProcessor
    {
        private const string IDF_LINE_ID = "id";
        private const string IDF_LINE_NAME = "name";

        private string _id;
        private string _name;

        public FeederProcessor(XElement node, GroupProcessor processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _id = Node.Attribute(IDF_LINE_ID).Value;
                _name = Node.Attribute(IDF_LINE_NAME).Value;

                Node.SetAttributeValue("aorGroup", "1");
                Node.SetAttributeValue("source", "");
                ParentGroup.SetSwitchInSubstation(Node.Attribute("device").Value);
            }
            catch (Exception ex)
            {
                Error("", $"Uncaught exception: {ex.Message}");
            }
        }

        #region Overrides
        protected override void Debug(string code, string message)
        {
            _log.Debug(Util.FormatLogString(LogLevel.Debug, "FEEDER", _id, _name, message));
        }

        protected override void Error(string code, string message)
        {
            _log.Error(Util.FormatLogString(LogLevel.Error, "FEEDER", _id, _name, message));
        }

        protected override void Info(string code, string message)
        {
            _log.Info(Util.FormatLogString(LogLevel.Info, "FEEDER", _id, _name, message));
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn(Util.FormatLogString(LogLevel.Warn, "FEEDER", _id, _name, message));
        }

        #endregion
    }
}

