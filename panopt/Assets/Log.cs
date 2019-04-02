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
}
