using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MainPower.IdfEnricher
{
    internal class FileManager : ErrorReporter
    {
        public static FileManager I { get; } = new FileManager();

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
                                Fatal($"Import Configuration is already set ({ImportConfig.FullFileName}), ignoring second import configuration ({idf.FullFileName})");
                            }
                            break;
                        case "Electric Distribution":
                            idf.Type = IdfFileType.Data;
                            DataFiles.Add(idf);
                            break;
                        default:
                            Fatal($"File {file} had an unrecognised type ({type})");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Fatal($"Error processing xml file {file}: {ex.Message}");
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
                    {
                        if (GroupFiles[id].DataFile == null)
                            GroupFiles[id].DataFile = idf;
                        else
                            Fatal("Group data file was already specified");
                    }
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
