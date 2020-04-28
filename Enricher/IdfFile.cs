using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class IdfFile
    {
        private string _fullFileName = "";

        public XDocument Content { get; set; }
        
        //TODO: remove cruft
        //public IdfFileType Type { get; set; }
        public string FileName { get; private set; }
        
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

    public class ImportConfig : IdfFile
    {
        public XElement MainPowerGroups { get; set; }

        public XElement GlobalGroups { get; set; }

        public ImportConfig(IdfFile i)
        {
            Content = i.Content;
            FullFileName = i.FullFileName;
        }
    }
}
