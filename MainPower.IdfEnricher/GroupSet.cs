using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    internal class GroupSet
    {
        public List<Idf> GraphicFiles { get; set; } = new List<Idf>();
        public Idf DataFile { get; set; } = null;
    }
}
