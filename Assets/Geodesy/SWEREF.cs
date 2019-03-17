using System;
using System.Collections.Generic;
using Hagring;
using Hagring.GNSS;


///
/// handles conversation of WGS84 to SWEREF and RH2000
///
public class SWEREF
{
    class Converter : Proj4Converter, CoordinatesConverter
    {
        static Dictionary<string, string> ProjParams = new Dictionary<string, string>
        {
            { "sweref_99_tm", "+proj=utm +zone=33" },
            { "sweref_99_12_00", "+proj=tmerc +lat_0=0 +lon_0=12 +k=1 +x_0=150000 +y_0=0"    },
            { "sweref_99_13_30", "+proj=tmerc +lat_0=0 +lon_0=13.5 +k=1 +x_0=150000 +y_0=0"  },
            { "sweref_99_15_00", "+proj=tmerc +lat_0=0 +lon_0=15 +k=1 +x_0=150000 +y_0=0"    },
            { "sweref_99_16_30", "+proj=tmerc +lat_0=0 +lon_0=16.5 +k=1 +x_0=150000 +y_0=0"  },
            { "sweref_99_18_00", "+proj=tmerc +lat_0=0 +lon_0=18 +k=1 +x_0=150000 +y_0=0"    },
            { "sweref_99_14_15", "+proj=tmerc +lat_0=0 +lon_0=14.25 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_15_45", "+proj=tmerc +lat_0=0 +lon_0=15.75 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_17_15", "+proj=tmerc +lat_0=0 +lon_0=17.25 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_18_45", "+proj=tmerc +lat_0=0 +lon_0=18.75 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_20_15", "+proj=tmerc +lat_0=0 +lon_0=20.25 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_21_45", "+proj=tmerc +lat_0=0 +lon_0=21.75 +k=1 +x_0=150000 +y_0=0" },
            { "sweref_99_23_15", "+proj=tmerc +lat_0=0 +lon_0=23.25 +k=1 +x_0=150000 +y_0=0" },
        };

        /* our internal name of the projection, e.g. sweref_99_tm or sweref_99_12_00 */
        string ProjName;

        public Converter(string projection) : base()
        {
            ProjName = projection;
            var ProjString = string.Format(
#if UNITY_ANDROID
                /*
                 * skip doing RH2000 height calculation on android for now,
                 * as proj4 can't load geogird data on android
                 */
                "{0} +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs",
#else
"{0} +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs",
//                "{0} +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +geoidgrids=SWEN17_RH2000.gtx +units=m +no_defs",
#endif
                ProjParams[projection]);

            Proj = Proj4.CreateProj(ProjString);
        }

        public GPSPosition fromWGS84(double longitude, double latitude, double elevation)
        {
            Console.Error.WriteLine("fromWGS84 {0} {1} {2}",
                                    longitude, latitude, elevation);

            Console.Error.WriteLine(
                "{0} {1} {2}",
                Utils.toRad(longitude), Utils.toRad(latitude), elevation
            );
            var pos = Proj4.proj_trans(Proj,
                                       Proj4.PJ_DIRECTION.PJ_FWD,
                                       Proj4.proj_coord(Utils.toRad(longitude), Utils.toRad(latitude), elevation, 0));

            if (double.IsInfinity(pos.up))
            {
                /*
                 * proj4 returns infinity value for up when we are outside of the geoid grid bounding box
                 *
                 * use fake geoid heigt, which we set to elevation, so that
                 * we can use SWEREF99 projections outside of the RH2000 bounding box
                 * this is usefull for running the device at real world location where
                 * we don't support the local projection system
                 */
                pos.up = elevation;
            }

            return new GPSPosition(ProjName, pos.north, pos.east, pos.up);
        }
    }

    public static AxisNames Axis = new AxisNames { North = "X", East = "Y", Up = "Z" };

    public static CoordinatesConverter makeConverter(string projection)
    {
        return new Converter(projection);
    }

    public static string Format(GPSPosition pos, FixQuality Quality)
    {
        return string.Format(
            "{0}\n" +
            "X {1:0.00}\n" +
            "Y {2:0.00}\n" +
            "Z {3:0.00}\n" +
            "Q {4}",
            pos.Projection.Replace("_", " "),
            pos.North, pos.East, pos.Altitude,
            Quality.ToString());
    }
}
