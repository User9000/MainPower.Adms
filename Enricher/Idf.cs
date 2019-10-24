using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class Idf
    {
        private string _fullFileName = "";

        public XDocument Content { get; set; }
        public IdfFileType Type { get; set; }
        public string FileName { get; private set; }
        
        //TODO: just for data\graphics idfs, should we sub class this into a separate type?
        public Dictionary<string, XElement> GroupElements { get; set; } = new Dictionary<string, XElement>();

        //TODO: just for the import config.  should we sub class this into a import config type?
        public XElement Groups { get; set; }
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
