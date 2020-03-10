using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MainPower.Adms.ExtractManager
{
    public static class Util
    {
        public static void SerializeNewtonsoft(string file, object obj, JsonSerializer s = null)
        {

            using (var f = File.CreateText(file))
            {
                if (s == null)
                {
                    s = new JsonSerializer
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        Formatting = Formatting.None
                    };
                }
                s.Serialize(f, obj);
            }
        }


        public static T DeserializeNewtonsoft<T>(string file) where T: class
        {
            try
            {
                using (var f = File.OpenText(file))
                {
                    JsonTextReader r = new JsonTextReader(f);
                    JsonSerializer s = new JsonSerializer()
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };
                    return s.Deserialize<T>(r);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
