using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.IdfEnricher
{
    class ScadaStatusPointInfo
    {
        public string PointName { get; set; } = "";
        public string Key { get; set; } = "";
        public string PointType { get; set; } = "";
        public bool QuadState { get; set; } = false;
    }

    class ScadaAnalogPointInfo
    {
        public string PointName { get; set; } = "";
        public string Key { get; set; } = "";
        public string PointType { get; set; } = "";
        public string Units { get; set; } = "";
    }


}
