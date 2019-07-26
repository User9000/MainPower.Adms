using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    abstract internal class DeviceProcessor
    {
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal XElement Node { get; private set; }
        internal GroupProcessor ParentGroup { get; private set; }

        internal DeviceProcessor(XElement node, GroupProcessor processor)
        {
            Node = node;
            ParentGroup = processor;
        }

        abstract internal void Process();

        abstract protected void Debug(string code, string message);
        abstract protected void Info(string code, string message);
        abstract protected void Warn(string code, string message);
        abstract protected void Error(string code, string message);
    }

}
