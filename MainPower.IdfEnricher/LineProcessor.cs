using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MainPower.IdfEnricher
{
    internal class LineProcessor : DeviceProcessor
    {
        private const string IDF_LINE_ID = "id";
        private const string IDF_LINE_NAME = "name";

        private string _id;
        private string _name;

        public LineProcessor(XmlElement node, GroupProcessor processor) : base(node, processor) { }

        internal override void Process()
        {
            try
            {
                _id = Node.Attributes[IDF_LINE_ID].InnerText;
                _name = Node.Attributes[IDF_LINE_NAME].InnerText;

                Processor.AddColorLink(_id);
            }
            catch (Exception ex)
            {
                Error("", $"Uncaught exception: {ex.Message}");
            }
        }
        
        #region Overrides
        protected override void Debug(string code, string message)
        {
            _log.Debug($"LINE,{_id},{_name},\"{message}\"");
        }

        protected override void Error(string code, string message)
        {
            _log.Error($"LINE,{_id},{_name},\"{message}\"");
        }

        protected override void Info(string code, string message)
        {
            _log.Info($"LINE,{_id},{_name},\"{message}\"");
        }

        protected override void Warn(string code, string message)
        {
            _log.Warn($"LINE,{_id},{_name},\"{message}\"");
        }

        #endregion
    }
}
