using System.Globalization;

namespace Brab
{

public class Parse
{
    ///
    /// parse integer number culturally independent
    ///
    public static uint Uint(string txt)
    {
        return uint.Parse(txt, CultureInfo.InvariantCulture);
    }

    ///
    /// parse decimal number to doube precision culturally independent
    ///
    public static double Double(string txt)
    {
        return double.Parse(txt, CultureInfo.InvariantCulture);
    }
}

}
