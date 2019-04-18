using System;
using System.IO;

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
