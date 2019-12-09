using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace MainPower.Adms.Enricher
{
    [Serializable]
    [MessagePackObject]
    public class ModelNode
    {
        [Key(0)]
        public string Id { get; set; }

        [IgnoreMember]
        [JsonIgnore]
        public List<ModelDevice> Devices { get; set; } = new List<ModelDevice>();

        [IgnoreMember]
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }
}
