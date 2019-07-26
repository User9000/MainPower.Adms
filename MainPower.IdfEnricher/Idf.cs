using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class Idf
    {
        private string _fullFileName = "";
        public string FullFileName
        {
            get
            {
                return _fullFileName;
            }
            set {
                _fullFileName = value;
                FileName = Path.GetFileName(_fullFileName);
            }
        }
        public XDocument Content { get; set; }
        public IdfFileType Type { get; set; }
        public string FileName { get; private set; }
    }
}
