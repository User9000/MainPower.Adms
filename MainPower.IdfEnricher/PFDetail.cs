using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    [Serializable]
    [MessagePackObject]
    public class PFDetail
    {
        [Key(0)]
        public bool Node1Mark { get; set; }
        [Key(1)]
        public bool Node2Mark { get; set; }
        [Key(2)]
        public double Node1Distance { get; set; } = double.NaN;
        [Key(3)]
        public double Node2Distance { get; set; } = double.NaN;
    }
}
