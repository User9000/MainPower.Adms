using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    class Program
    {
        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o => 
               {
                   try
                   {
                       var enricher = Enricher.Singleton;
                       enricher.Options = o;
                       enricher.LoadSourceData();
                       enricher.ProcessImportConfiguration();
                       Console.WriteLine("All done....");
                       Console.ReadKey();
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                       Console.ReadKey();
                   }
                   
               });
            
        }
    }
}
