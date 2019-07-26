using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class RegulatorProcessor : DeviceProcessor
    {
        private const string IDF_LINE_ID = "id";
        private const string IDF_LINE_NAME = "name";

        private string _id;
        private string _name;

        public const string GIS_T1_ASSET = "mpwr_t1_asset_nbr";

        public RegulatorProcessor(XElement node, GroupProcessor processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _id = Node.Attribute(IDF_LINE_ID).Value;
                _name = Node.Attribute(IDF_LINE_NAME).Value;
                ParentGroup.SetSymbolName(_id, "Symbol 7", double.NaN, double.NaN, 2);
                Node.SetAttributeValue("aorGroup", "1");
                Node.SetAttributeValue("nominalState1", "True");
                Node.SetAttributeValue("nominalState2", "True");
                Node.SetAttributeValue("nominalState3", "True");
                Node.SetAttributeValue("ratedKV", "12");
                Node.SetAttributeValue(GIS_T1_ASSET, null);
            }
            catch (Exception ex)
            {
                Error("", $"Uncaught exception: {ex.Message}");
            }
        }

        #region Overrides
        protected override void Debug(string code, string message)
        {
            _log.Debug($"REGULATOR,{_id},{_name},\"{message}\"");
        }

        protected override void Error(string code, string message)
        {
            _log.Error($"REGULATOR,{_id},{_name},\"{message}\"");
        }

        protected override void Info(string code, string message)
        {
            _log.Info($"REGULATOR,{_id},{_name},\"{message}\"");
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn($"REGULATOR,{_id},{_name},\"{message}\"");
        }

        #endregion
    }
}
