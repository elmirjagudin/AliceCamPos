using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Log
{
    public static void Msg(object msg)
    {
        Msg("{0}", msg);
    }

    public static void Msg(string format, params object[] args)
    {
        Debug.LogFormat(format, args);
        Console.Error.WriteLine(format, args);
    }

    public static void Wrn(object msg)
    {
        Wrn("{0}", msg);
    }

    public static void Wrn(string format, params object[] args)
    {
        Debug.LogWarningFormat(format, args);
        Console.Error.WriteLine("WARNING:" + format, args);
    }

    public static void Err(object msg)
    {
        Err("{0}", msg);
    }

    public static void Err(string format, params object[] args)
    {
        Debug.LogErrorFormat(format, args);
        Console.Error.WriteLine("ERROR:" + format, args);
    }
}

///
/// shorthand wrapper, log with:
///
///     L.M("my message");
///
public class L
{
    public static void M(object msg)
    {
        Log.Msg("{0}", msg);
    }

    public static void M(string format, params object[] args)
    {
        Log.Msg(format, args);
    }

    public static void W(object msg)
    {
        Log.Wrn("{0}", msg);
    }

    public static void W(string format, params object[] args)
    {
        Log.Wrn(format, args);
    }

    public static void E(object msg)
    {
        Log.Err("{0}", msg);
    }

    public static void E(string format, params object[] args)
    {
        Log.Err(format, args);
    }

}
