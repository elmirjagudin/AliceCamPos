using System;
using System.IO;
using System.Collections.Generic;

///
/// Handles generation of unique ID number,
/// suitable as ID for views and poses
///
class IDs
{
    /*
     * seems that view and pose ID must be 'large numbers',
     * otherwise the meshroom_compute will fail on
     * 'DepthMap' node
     */
    const uint MAGIC_LARGE_NUMBER = 60903726;

    static uint NextId = MAGIC_LARGE_NUMBER;
    static Dictionary<string, uint> ids = new Dictionary<string, uint>();

    public static uint Get(string image)
    {
        if (!ids.ContainsKey(image))
        {
            ids[image] = NextId++;
        }

        return ids[image];
    }
}


public class Meshroom
{
    const string MESHROOM_CACHE_DIR = "MeshroomCache";

    public static string GetCacheDir(string ImagesDir)
    {
        return Path.Combine(ImagesDir, "MeshroomCache");
    }

    public static string GetNodeCacheDir(string nodeRoot)
    {
        var subdirs = Directory.GetDirectories(nodeRoot);

        if (subdirs.Length == 0)
        {
            return null;
        }

        if (subdirs.Length != 1)
        {
            throw new Exception($"unexpected subdirectries in {nodeRoot}");
        }
        return subdirs[0];
    }
}
