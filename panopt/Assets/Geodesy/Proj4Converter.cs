using Hagring;
using System;
using UnityEngine;

public abstract class Proj4Converter
{
    protected IntPtr Proj;

    protected Proj4Converter()
    {
        /* look for geoid grid tables in streaming assets directory */
        Proj4.SetSearchPath(AppPaths.StreamingAssetsDir);
    }

    public void toWGS84(GPSPosition pos, out double longitude, out double latitude, out double elevation)
    {
        var wgsPos = Proj4.proj_trans(Proj,
                                      Proj4.PJ_DIRECTION.PJ_INV,
                                      Proj4.proj_coord(pos.East, pos.North, pos.Altitude));

        if (double.IsInfinity(wgsPos.north) || double.IsInfinity(wgsPos.east) || double.IsInfinity(wgsPos.up))
        {
            /* proj4 returns infinity value if it can't transform position */
            throw new CoordinateTransformationException("WGS84", pos);
        }

        longitude = Utils.toDegrees(wgsPos.east);
        latitude = Utils.toDegrees(wgsPos.north);
        elevation = wgsPos.up;
    }
}
