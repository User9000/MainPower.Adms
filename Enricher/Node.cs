using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace MainPower.Osi.Enricher
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

        [IgnoreMember]
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }
}
