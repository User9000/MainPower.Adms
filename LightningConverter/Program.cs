using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MainPower.Adms.Lightning
{
    class Program
    {
        static void Main(string[] args)
        {
            //

            JArray inputarray = Util.DeserializeNewtonsoft(@"C:\Users\hsc\Downloads\lightninginput.txt");

            //JArray inputarray = Util.GetLightningDataFromMetservice();
            List<object> templist = new List<object>();
            string output;

            foreach (var item in inputarray)
            {
                DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item["timeMillis"].ToString()));
                var dt = dto.LocalDateTime;
                var i = new
                {
                    attributes = new
                    {
                        timeMillis = item["timeMillis"],
                        latitude = item["latitude"],
                        longitude = item["longitude"],
                        direction = item["direction"],
                        current = item["current"],
                        datetime = $"{dt.ToShortDateString()} {dt.ToShortTimeString()}"
                    }
                };
                templist.Add(i);
            }
            //output = Util.ToJson(templist);


            Util.SerializeNewtonsoft(@"C:\users\hsc\downloads\lightningoutput.txt", templist);

            
        }
    }
}
