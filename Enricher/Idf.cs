using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    internal class Idf
    {
        private string _fullFileName = "";

        public XDocument Content { get; set; }
        public IdfFileType Type { get; set; }
        public string FileName { get; private set; }
        public Dictionary<string, XElement> GroupElements { get; set; } = new Dictionary<string, XElement>();
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
    }
}
