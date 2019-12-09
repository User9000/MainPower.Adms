using System;
using System.Xml;

namespace MainPower.Adms.EpaFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                XmlDocument x = new XmlDocument();
                x.Load("fep_config.xml");
                var points = x.SelectNodes("//RTU[Subtype ='2']/POINT");
                foreach (XmlElement point in points)
                {
                    try //in case bad file
                    {
                        point["FepPointAddress"].InnerText = (int.Parse(point["IntParms"]["Index0"].InnerText) + 1).ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                
                points = x.SelectNodes("//RTU[Subtype ='1']/POINT");
                foreach (XmlElement point in points)
                {
                    try //in case bad file
                    {
                        point["FepPointAddress"].InnerText = (int.Parse(point["IntParms"]["Index0"].InnerText) + 1).ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                x.Save("fep_config.xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
