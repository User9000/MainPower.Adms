using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace IdfCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.Write("Enter path to idfs to clear:");
            string path = Console.ReadLine();
            var files = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    //if (doc.Root.Attribute("type")?.Value == "OSI Oneline")
                    {
                        Console.WriteLine($"Cleaning {file}");
                        var groups = doc.Root.Descendants("group");
                        foreach (var group in groups)
                        {
                            group.RemoveNodes();
                        }
                    }
                    doc.Save(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
