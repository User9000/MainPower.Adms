using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MainPower.Adms.CoordinateConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static Point TranslatePoint2(Point p)
        {
            p = ProjectionTransforms.LatLonToMeters(p.X, p.Y);
            p.Y += 5288390.101;
            p.X -= 19252195.535;
            p.Y /= 0.4552;
            p.X /= 0.4552;
            p.Y += 250000;
            p.X += 250000;
            return p;
        }

        public static Point TranslatePoint(Point p)
        {
            p.X -= 250000;
            p.Y -= 250000;
            p.X *= 0.4552;
            p.Y *= 0.4552;
            p.X += 19252195.535;
            p.Y -= 5288390.101;

            p = ProjectionTransforms.MetersToLatLon(p);

            return p;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point p = new Point
                {
                    X = double.Parse(txtlat1.Text),
                    Y = double.Parse(txtlon1.Text),
                };
                var p2 = TranslatePoint2(p);
                txtlat2.Text = p2.X.ToString();
                txtlon2.Text = p2.Y.ToString();
            } catch { }
        }
    }

    static class ProjectionTransforms
    {

        private const int TileSize = 256;
        private const int EarthRadius = 6378137;
        private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
        private const double OriginShift = 2 * Math.PI * EarthRadius / 2;

        //Converts given lat/lon in WGS84 Datum to XY in Spherical Mercator EPSG:900913
        public static Point LatLonToMeters(double lat, double lon)
        {
            var p = new Point();
            p.X = lon * OriginShift / 180;
            p.Y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            p.Y = p.Y * OriginShift / 180;
            return p;
        }

        //Converts XY PointD from (Spherical) Web Mercator EPSG:3785 (unofficially EPSG:900913) to lat/lon in WGS84 Datum
        public static Point MetersToLatLon(Point m)
        {
            var ll = new Point();
            ll.X = (m.X / OriginShift) * 180;
            ll.Y = (m.Y / OriginShift) * 180;
            ll.Y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(ll.Y * Math.PI / 180)) - Math.PI / 2);
            return ll;
        }
    }

    public struct Point : IEquatable<Point>
    {
        public double X { get; set; }
        public double Y { get; set; }
       
        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        public bool Equals(Point other)
        {
            throw new NotImplementedException();
        }
    }
}
