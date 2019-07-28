using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    internal class PFDetail
    {
        public bool Node1Mark { get; set; }
        public bool Node2Mark { get; set; }
        public double Node1Distance { get; set; } = double.NaN;
        public double Node2Distance { get; set; } = double.NaN;
    }
}
