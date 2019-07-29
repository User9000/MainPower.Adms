using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    [Serializable]
    [MessagePackObject]
    public class Node
    {
        [Key(0)]
        public string Id { get; set; }
        [IgnoreMember]
        [JsonIgnore]
        public List<Device> Devices { get; set; } = new List<Device>();
    }
}
