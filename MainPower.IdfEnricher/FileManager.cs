using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class FileManager
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static FileManager _singleton = null;

        public static FileManager I
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new FileManager();
                }
                return _singleton;
            }
        }

        internal List<Idf> DataFiles = new List<Idf>();
        internal List<Idf> GraphicsFiles = new List<Idf>();
        internal Idf ImportConfig = null;
        internal Dictionary<string, GroupSet> GroupFiles = new Dictionary<string, GroupSet>();

        public void Initialize(string path)
        {
            //Read all the xml files in the directory
            var files = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var idf = new Idf();
                    idf.Content = XDocument.Load(file);
                    idf.FullFileName = file;
                    var type = idf.Content.Root.Attribute("type").Value;
                    switch (type)
                    {
                        case "OSI Oneline":
                            idf.Type = IdfFileType.Graphics;
                            GraphicsFiles.Add(idf);
                            break;
                        case "Import Configuration":
                            idf.Type = IdfFileType.ImportConfig;
                            if (ImportConfig == null)
                            {
                                ImportConfig = idf;
                            }
                            else
                            {
                                _log.Error($"Import Configuration is already set ({ImportConfig.FullFileName}), ignoring second import configuration ({idf.FullFileName})");
                            }
                            break;
                        case "Electric Distribution":
                            idf.Type = IdfFileType.Data;
                            DataFiles.Add(idf);
                            break;
                        default:
                            _log.Error($"File {file} had an unrecognised type ({type})");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error processing xml file {file}: {ex.Message}");
                }

            }

            //work out which groups are in which files
            List<Idf> Files = new List<Idf>();
            Files.AddRange(DataFiles);
            Files.AddRange(GraphicsFiles);

            foreach(var idf in Files)
            {
                var ids = idf.Content.Descendants("groups").Descendants("group").Attributes("id");
                foreach (string id in ids)
                {
                    if (!GroupFiles.ContainsKey(id))
                    {
                        GroupFiles.Add(id, new GroupSet());
                    }
                    if (idf.Type == IdfFileType.Data)
                        GroupFiles[id].DataFiles.Add(idf);
                    else if (idf.Type == IdfFileType.Graphics)
                        GroupFiles[id].GraphicFiles.Add(idf);
                }
            }


        }

        public void SaveFiles(string path)
        {
            List<Idf> files = new List<Idf>();
            files.AddRange(DataFiles);
            files.AddRange(GraphicsFiles);
            files.Add(ImportConfig);

            foreach (var idf in files)
            {
                idf.Content.Save($"{path}\\{idf.FileName}");
            }
        }

    }
}
