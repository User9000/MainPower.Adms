using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MainPower.Adms.Lightning
{
    class Program
    {
        static void Main(string[] args)
        {
            JArray inputarray = Util.DeserializeNewtonsoft(@"lightninginput.txt");
            List<object> templist = new List<object>();
            string output;

            foreach (var item in inputarray)
            {
                DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item["timeMillis"].ToString()));
                var dt = dto.LocalDateTime;
                var i = new
                {
                    timeMillis = item["timeMillis"],
                    latitude = item["latitude"],
                    longitude = item["longitude"],
                    direction = item["direction"],
                    current = item["current"],
                    datetime = $"{dt.ToShortDateString()} {dt.ToShortTimeString()}"
                };
                templist.Add(i);
            }
            Util.SerializeNewtonsoft(@"lightningoutput.txt", templist);           
        }
    }
}
