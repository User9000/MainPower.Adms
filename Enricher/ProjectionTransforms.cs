using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainPower.Osi.Enricher
{
    /// <summary>
    /// Conversion routines for Google, TMS, and Microsoft Quadtree tile representations, derived from
    /// http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/ 
    /// </summary>
    static class ProjectionTransforms
    {

        private const int TileSize = 256;
        private const int EarthRadius = 6378137;
        private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
        private const double OriginShift = 2 * Math.PI * EarthRadius / 2;

        //Converts given lat/lon in WGS84 Datum to XY in Spherical Mercator EPSG:900913
        public static PointD LatLonToMeters(double lat, double lon)
        {
            var p = new PointD();
            p.X = lon * OriginShift / 180;
            p.Y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            p.Y = p.Y * OriginShift / 180;
            return p;
        }

        //Converts XY PointD from (Spherical) Web Mercator EPSG:3785 (unofficially EPSG:900913) to lat/lon in WGS84 Datum
        public static PointD MetersToLatLon(PointD m)
        {
            var ll = new PointD();
            ll.X = (m.X / OriginShift) * 180;
            ll.Y = (m.Y / OriginShift) * 180;
            ll.Y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(ll.Y * Math.PI / 180)) - Math.PI / 2);
            return ll;
        }
    }
}
          
