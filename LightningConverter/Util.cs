using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace MainPower.Adms.Lightning
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
                        PreserveReferencesHandling = PreserveReferencesHandling.None,
                        Formatting = Formatting.None
                    };
                }
                s.Serialize(f, obj);
            }
        }

        public static JArray DeserializeNewtonsoft(string file)
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
                    return (JArray)s.Deserialize(r);
                }
            }
            catch
            {
                return null;
            }
        }

        public static JArray GetLightningDataFromMetservice()
        {
            return null;
        }

        public static string ToJson(object o)
        {
            return "";
        }
    }
}
