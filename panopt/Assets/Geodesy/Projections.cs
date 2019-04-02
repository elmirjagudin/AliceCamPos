using System.Collections.Generic;
using Hagring;
using Hagring.GNSS;
using System;

///
/// Thrown when trying to convert coordinated for which the
/// used projection is not defined.
///
public class CoordinateTransformationException : Exception
{
    ///
    /// source position, which we failed to transform
    ///
    GPSPosition _Position;
    public GPSPosition Position { get { return _Position; } } /* read only property */

    ///
    /// target projection for transformation
    ///
    string _Projection;
    public string Projection { get { return _Projection; } } /* read only property */


    public CoordinateTransformationException(string Projection, GPSPosition Position)
    {
        _Projection = Projection;
        _Position = Position;
    }
}

public class AxisNames
{
    public string North;
    public string East;
    public string Up;
}

public interface CoordinatesConverter
{
    GPSPosition fromWGS84(double longitude, double latitude, double elevation);
    void toWGS84(GPSPosition pos, out double longitude, out double latitude, out double elevation);
}

public class GeodesyProjections
{
    ///
    /// May throw InvalidProjectionSelectedException exception if specified
    /// longitude and latitude are outside of the projection's bounding box.
    /// In other words if this projection is not specified for provided coordinates.
    ///
    public delegate GPSPosition fromWGS84(double longitude, double latitude, double elevation);

    delegate CoordinatesConverter ConverterFactory(string Projection);

    /* formats a position in the local projection to a string used in 'Status' UI box */
    public delegate string PositionFormatter(GPSPosition pos, FixQuality Quality);

    private static AxisNames NorthingEastingAxis = new AxisNames { North = "Northing (m)", East = "Easting (m)", Up = "Height (m)" };

    class ProjDesc
    {
        /* converter factory delegate */
        public ConverterFactory convFact;
        /* position formatter delegate */
        public PositionFormatter posForm;
        /* axis names description */
        public AxisNames axisNames;
    }

    /* maps projection string to a delegates for converting and formatting coordinates */
    static Dictionary<string, ProjDesc> projDescriptions = new Dictionary<string, ProjDesc>
    {
        { "sweref_99_tm", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_12_00", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_13_30", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_15_00", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_16_30", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_18_00", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_14_15", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_15_45", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_17_15", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_18_45", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_20_15", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_21_45", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
        { "sweref_99_23_15", new ProjDesc { convFact = SWEREF.makeConverter, posForm = SWEREF.Format, axisNames = SWEREF.Axis }},
    };

    static Dictionary<string, CoordinatesConverter> ConvertersCache = new Dictionary<string, CoordinatesConverter>();

    ///
    /// Implements a special case projection 'conversation' where do identity projection,
    ///
    /// that is we set:
    ///   latitute to north
    ///   longitude to east
    ///   elevation to altitude
    ///
    /// this dummy conversion is used by the H�gring desktop application,
    /// where we have all input coordinates in same local projection,
    /// and don't want to do unneeded projection -> WGS84 -> projection
    /// conversations
    class IdentityProjection
    {
        string Projection;

        public IdentityProjection(string Projection)
        {
            this.Projection = Projection;
        }

        public GPSPosition identityConv(double longitude, double latitude, double elevation)
        {
            return new GPSPosition(Projection, latitude, longitude, elevation);
        }
    }

    public static CoordinatesConverter GetConverter(string Projection)
    {
        if (!ConvertersCache.ContainsKey(Projection))
        {
            ConvertersCache.Add(Projection, projDescriptions[Projection].convFact(Projection));
        }

        return ConvertersCache[Projection];
    }

    ///
    /// Get a function that can convert WGS84 lon, lat, altitude coordinates to
    /// the specified projection.
    ///
    public static fromWGS84 fromWGS84Converter(string Projection, bool DoDummyConversions=false)
    {
        if (DoDummyConversions)
        {
            var x = new IdentityProjection(Projection);
            return x.identityConv;
        }

        return GetConverter(Projection).fromWGS84;
    }

    public static AxisNames getAxisNames(string Projection)
    {
        return projDescriptions[Projection].axisNames;
    }

    public static PositionFormatter getPositionFormatter(string Projection)
    {
        return projDescriptions[Projection].posForm;
    }
}

public class GeodesyTransforms
{
    public static void toWGS84(GPSPosition pos, out double longitude, out double latitude, out double elevation)
    {
        var converter = GeodesyProjections.GetConverter(pos.Projection);
        converter.toWGS84(pos, out longitude, out latitude, out elevation);
    }

    public static GPSPosition fromWGS84(string Projection, double longitude, double latitude, double elevation)
    {
        var converter = GeodesyProjections.GetConverter(Projection);
        return converter.fromWGS84(longitude, latitude, elevation);
    }
}
