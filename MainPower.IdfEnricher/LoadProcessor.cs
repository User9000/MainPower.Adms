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
            _log.Debug($"LOAD,{_id},{_name},\"{message}\"");
        }

        protected override void Error(string code, string message)
        {
            _log.Error($"LOAD,{_id},{_name},\"{message}\"");
        }

        protected override void Info(string code, string message)
        {
            _log.Info($"LOAD,{_id},{_name},\"{message}\"");
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn($"LOAD,{_id},{_name},\"{message}\"");
        }

        #endregion
    }
}
