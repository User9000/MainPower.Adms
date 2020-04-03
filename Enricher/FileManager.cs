using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Text;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// Reads IDF files and sorts them into groups
    /// </summary>
    public class FileManager : ErrorReporter
    {
        public List<IdfFile> DataFiles = new List<IdfFile>();
        public List<IdfFile> GraphicsFiles = new List<IdfFile>();
        public IdfFile ImportConfig = null;
        public Dictionary<string, IdfGroup> Groups = new Dictionary<string, IdfGroup>();

        public bool Initialize(string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Info("Sorting idfs...");
            //Read all the xml files in the directory
            var files = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var idf = new IdfFile();
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
                    idf.Content.Root.SetAttributeValue("timestamp", DateTime.UtcNow.ToString("s"));
                }
                catch (Exception ex)
                {
                    Fatal($"Error processing xml file {file}: {ex.Message}");
                    return false;
                }
            }

            Info("Reading Import Config...");
            ImportConfig.Groups = ImportConfig.Content.Descendants("container").Where(x=> x.Attribute("name")?.Value == "Mainpower").FirstOrDefault();

            //Read all the groups we are going to import add them to the collection
            foreach (var group in ImportConfig.Content.Descendants("group"))
            {
                var g = new IdfGroup(group, null);
                Groups.Add(g.Id, g);
            }

            Info("Loading groups from data files...");
            //loop through the data files and assign the elements to the groups
            foreach (var idf in DataFiles)
            {
                var groups = idf.Content.Descendants("group");
                foreach (var group in groups)
                {
                    var id = group.Attribute("id").Value;
                    if (Groups.ContainsKey(id))
                    {
                        Groups[id].SetDataGroup(group);
                    }
                    else
                    {
                        Err("Data group is not in the import configuration.", id, idf.FileName);
                    }
                }
            }

            Info("Loading groups from display files...");
            //loop through the graphics files and assign the elements to the groups
            foreach (var idf in GraphicsFiles)
            {
                string display = idf.Content.Root.Attribute("displayName").Value;
                var groups = idf.Content.Descendants("group");
                //TODO: tolist  required so we can add new ones...
                foreach (var group in groups.ToList())
                {

                    var id = group.Attribute("id")?.Value;
                    if (id == null)
                    {
                        Err($"A display group with no id was in file {idf.FileName} and will be deleted");
                        group.Remove();
                        continue;
                    }
                    if (!Groups.ContainsKey(id))
                    {
                        Err("Display group is not in the import configuration", id, idf.FileName);
                        //TODO: we probably shouldn't do this in prod
                        //XElement g = new XElement("group", new XAttribute("id", id), new XAttribute("name", id));
                        //ImportConfig.Groups.Add(g);
                        //var gr = new IdfGroup(g, null);
                        //Groups.Add(gr.Id, gr);
                    }
                    Groups[id].AddDisplayGroup(display, group);
                }
            }

            return true;
        }

        public void SaveFiles(string path)
        {
            List<IdfFile> files = new List<IdfFile>();
            files.AddRange(DataFiles);
            files.AddRange(GraphicsFiles);
            files.Add(ImportConfig);

            foreach (var idf in files)
            {
                idf.Content.Save($"{path}\\{idf.FileName}");
            }

            var gFile = new StreamWriter($"{path}\\groups.dat");
            foreach (var kvp in Groups)
            {
                gFile.WriteLine(kvp.Key);
            }
            gFile.Close();

        }

    }
}
