﻿using MessagePack;
using System;

namespace MainPower.Osi.Enricher
{
    [Serializable]
    [MessagePackObject]
    public class ModelSource
    {
        [Key(0)]
        public string Id { get; set; }

        [Key(1)]
        public string DeviceId { get; set; }

        [Key(2)]
        public string Name { get; set; }

        [Key(3)]
        public string GroupId { get; set; }

        [Key(4)]
        public short[] PhaseAngles { get; set; } = new short[3];
    }
}
