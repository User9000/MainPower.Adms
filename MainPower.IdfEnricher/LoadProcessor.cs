using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class LoadProcessor : DeviceProcessor
    {
        private const string IDF_LOAD_ID = "id";
        private const string IDF_LOAD_NAME = "name";

        private string _id;
        private string _name;

        public LoadProcessor(XElement node, GroupProcessor processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _id = Node.Attribute(IDF_LOAD_ID).Value;
                _name = Node.Attribute(IDF_LOAD_NAME).Value;
                Node.SetAttributeValue("aorGroup", "1");
                Node.SetAttributeValue("nominalState1", "True");
                Node.SetAttributeValue("nominalState2", "True");
                Node.SetAttributeValue("nominalState3", "True");
                Node.SetAttributeValue("ratedKV", "0.4000");
                Node.SetAttributeValue("secondaryBaseKV", "0.4000");
                var basekv = Node.Attribute("baseKV")?.Value;
                if (string.IsNullOrWhiteSpace(basekv))
                {
                    Node.SetAttributeValue("baseKV", "0.2300");
                }
                //if (basekv = "0.4000)
                double load = Enricher.I.GetIcpLoad(Node.Attribute("name").Value);
                if (load.Equals(double.NaN))
                {
                    Warn("ICP", $"ICP was not found in the ICP database, assigning default load of 7.5kW");
                    load = 7.5;
                }
                else
                {
                    load = load / 72;
                }
                //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                //Node.SetAttributeValue("nominalkW1", load.ToString("N1");
                Node.SetAttributeValue("nominalKWAggregate", load.ToString("N1"));
                ParentGroup.SetSymbolName(_id, "Symbol 13", 2.0);
            }
            catch (Exception ex)
            {
                Error("", $"Uncaught exception: {ex.Message}");
            }
        }

        #region Overrides
        protected override void Debug(string code, string message)
        { 
            _log.Debug(Util.FormatLogString(LogLevel.Debug, $"LOAD", _id, _name, message));
        }

        protected override void Error(string code, string message)
        {
            _log.Error(Util.FormatLogString(LogLevel.Error, $"LOAD", _id, _name, message));
        }

        protected override void Info(string code, string message)
        {
            _log.Info(Util.FormatLogString(LogLevel.Info, $"LOAD", _id, _name, message));
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn(Util.FormatLogString(LogLevel.Warn, $"LOAD", _id, _name, message));
        }

        #endregion
    }
}
