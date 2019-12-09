using MessagePack;
using System;
using System.Drawing;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
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
       /* TODO: sort this
        [Key(4)]
        public Color Color { get; set; }
        */
        public ModelFeeder() { }

    }
}

