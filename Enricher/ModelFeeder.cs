using MessagePack;
using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    [MessagePackObject]
    public class ModelFeeder
    {
        [Key(0)]
        public string DeviceId { get; set; }
        [Key(1)]
        public string FeederId { get; set; }
        [Key(2)]
        public string FeederName { get; set; }
        [Key(3)]
        public string GroupId { get; set; }
        public ModelFeeder() { }

    }
}

