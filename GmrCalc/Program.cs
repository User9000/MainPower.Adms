using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmrCalc
{
    class Program
    {
        static void Main(string[] args)
        {
            double d = 2.110;
            double layers = 2;
            int num_conductors = 7;
            PointF[]  l = new PointF[num_conductors];

            //first strand is in the middle
            l[0] = new PointF(0, 0);

            for (int i = 0; i < 6; i++)
            {
                l[i + 1] = new PointF((float)(Math.Cos(i * 2 * Math.PI / 6) * d), (float)(Math.Sin(i * 2* Math.PI / 6) * d));
            }

            double adis = 1;
            int count = 0; 
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (j == i)
                        continue;
                    double dis = Math.Sqrt(Math.Pow(l[j].X - l[i].X, 2) + Math.Pow(l[j].Y - l[i].Y, 2));
                    adis *= dis;
                    count++;
                }              
            }

            var result = Math.Pow(adis, 1d / (Math.Pow(num_conductors,2)));

        }
    }
}
