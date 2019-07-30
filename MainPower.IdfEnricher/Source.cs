using MessagePack;
using System;

namespace MainPower.IdfEnricher
{
    [Serializable]
    [MessagePackObject]
    public class Source
    {
        [Key(0)]
        public string Id { get; set; }

        [Key(1)]
        public string DeviceId { get; set; }

        [Key(2)]
        public string Name { get; set; }

        [Key(3)]
        public string GroupId { get; set; }
    }
}
