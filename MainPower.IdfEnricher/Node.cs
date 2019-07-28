using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    internal class Node
    {
        public string Id { get; set; }
        public List<Device> Devices { get; set; } = new List<Device>();
    }
}
