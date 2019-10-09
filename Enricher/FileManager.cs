﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// Reads IDF files and sorts them into groups
    /// </summary>
    internal class FileManager : ErrorReporter
    {
        public static FileManager I { get; } = new FileManager();

        internal List<Idf> DataFiles = new List<Idf>();
        internal List<Idf> GraphicsFiles = new List<Idf>();
        internal Idf ImportConfig = null;
        internal Dictionary<string, Group> Groups = new Dictionary<string, Group>();

        public bool Initialize(string path)
        {
            Info("Sorting idfs...");
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
                    idf.Content.Root.SetAttributeValue("timestamp", DateTime.UtcNow.ToString("s"));
                }
                catch (Exception ex)
                {
                    Fatal($"Error processing xml file {file}: {ex.Message}");
                    return false;
                }
            }

            Info("Reading Import Config...");
            //Read all the groups we are going to import add them to the collection
            foreach (var group in ImportConfig.Content.Descendants("group"))
            {
                var g = new Group(group, null);
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
                        Warn($"group {id} was is not in the import configuration");
                    }
                }
            }

            Info("Loading groups from display files...");
            //loop through the graphics files and assign the elements to the groups
            foreach (var idf in GraphicsFiles)
            {
                string display = idf.Content.Root.Attribute("displayName").Value;
                var groups = idf.Content.Descendants("group");
                foreach (var group in groups)
                {
                    var id = group.Attribute("id").Value;
                    if (Groups.ContainsKey(id))
                    {
                        Groups[id].AddDisplayGroup(display, group);
                    }
                    else
                    {
                        Warn($"group {id} was is not in the import configuration");
                    }
                }
            }

            return true;
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
