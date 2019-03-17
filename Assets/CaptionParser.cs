using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hagring.DJI
{

class Utils
{
    public static string Get(GroupCollection groups, string name)
    {
        return groups[name].Value;
    }

    ///
    /// parse integer number culturally independent
    ///
    public static uint ParseUint(string txt)
    {
        return uint.Parse(txt, CultureInfo.InvariantCulture);
    }

    ///
    /// parse decimal number to doube precision culturally independent
    ///
    public static double ParseDouble(string txt)
    {
        return double.Parse(txt, CultureInfo.InvariantCulture);
    }

    ///
    /// parse decimal number to doube precision culturally independent
    ///
    public static float ParseFloat(string txt)
    {
        return float.Parse(txt, CultureInfo.InvariantCulture);
    }
}

class DJICaptionParser
{
    static Regex rx =
        new Regex(
            @"^.*RTK \((?<long>\d*\.\d*), " +
            @"(?<lat>\d*\.\d*), " +
//            @"(?<alt>\d*\.?\d*)\)," +
            @".* HOME \((\d*\.\d*, ){2}(?<halt>\d*\.\d*)m\)" +
            @".*H (?<height>\d*\.\d*)m" +
            @".* G.PRY \(" +
            @"(?<pitch>-?\d*\.\d*)°, " +
            @"(?<roll>-?\d*\.\d*)°, " +
            @"(?<yaw>-?\d*\.\d*)°\)",
                  RegexOptions.Compiled);

    public static void Parse(string str,
                             out double Longitude,
                             out double Latitude,
                             out double Altitude,
                             out float Pitch,
                             out float Roll,
                             out float Yaw)
    {
        var groups = rx.Match(str).Groups;

        Longitude = Utils.ParseDouble(Utils.Get(groups, "long"));
        Latitude = Utils.ParseDouble(Utils.Get(groups, "lat"));

        var home_height = Utils.ParseDouble(Utils.Get(groups, "halt"));
        var rel_height = Utils.ParseDouble(Utils.Get(groups, "height"));
        Altitude = home_height + rel_height;
        //Altitude = Utils.ParseDouble(Utils.Get(groups, "alt"));

        Pitch = Utils.ParseFloat(Utils.Get(groups, "pitch"));
        Roll = Utils.ParseFloat(Utils.Get(groups, "roll"));
        Yaw = Utils.ParseFloat(Utils.Get(groups, "yaw"));
    }
}

class SrtReader
{
    const int GROUPS_NUM = 5;
    static Regex rx =
        new Regex(
            @"^(?<hours>\d\d):(?<min>\d\d):(?<sec>\d\d),(?<msec>\d\d\d)",
                  RegexOptions.Compiled);

    IEnumerator<string> fileLines;

    public SrtReader(string fileName)
    {
        fileLines = File.ReadLines(fileName).GetEnumerator();
    }

    uint ParseTimestamp(string str)
    {
        var groups = rx.Match(str).Groups;

        if (groups.Count != GROUPS_NUM)
        {
            throw new Exception("error parsing time stamp : '" + str + "'");
        }


        var hours = Utils.ParseUint(Utils.Get(groups, "hours"));
        var mins = Utils.ParseUint(Utils.Get(groups, "min"));
        var secs = Utils.ParseUint(Utils.Get(groups, "sec"));
        var msecs = Utils.ParseUint(Utils.Get(groups, "msec"));

        return (((hours * 60) + mins) * 60 + secs) * 1000 + msecs;
    }

    string NextLine()
    {
        if (!fileLines.MoveNext())
        {
            throw new EndOfStreamException();
        }

        return fileLines.Current;
    }

    public void NextCaption(out uint TimeStamp, out string Caption)
    {
        /* ignore subtitle number */
        NextLine();

        /* parse time stamp */
        TimeStamp = ParseTimestamp(NextLine());

        /*
         * NOTE: we assume that each subtitle entry
         * is only one line, which is not a valid
         * assumtion for all .SRT files
         */

        /* get one line of subtitle */
        Caption = NextLine();

        /* ignore the separator line */
        NextLine();
    }
}

public class CaptionParser
{
    SrtReader SrtReader;

    public CaptionParser(string  SrtFile)
    {
        SrtReader = new SrtReader(SrtFile);
    }

    public void ReadPose(out uint TimeStamp,
                         out double Latitude,
                         out double Longitude,
                         out double Altitude,
                         out float Pitch,
                         out float Roll,
                         out float Yaw)
    {
        string Caption;

        SrtReader.NextCaption(out TimeStamp, out Caption);

        DJICaptionParser.Parse(Caption, out Longitude, out Latitude, out Altitude,
                               out Pitch, out Roll, out Yaw);
    }
}

}