using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable 0649
class Status
{
    public string status;

    public static bool isDone(string statusFile)
    {
        var status = JsonConvert.DeserializeObject<Status>(File.ReadAllText(statusFile));
        return string.Compare(status.status, "SUCCESS") == 0;
    }
}
#pragma warning restore

public class ComputeProgress
{
    List<string> statusSteps;

    public int TotalSteps
    {
        get { return statusSteps.Count; }
    }

    public ComputeProgress(IEnumerable<string> statusFiles)
    {
        this.statusSteps = statusFiles.ToList();
    }

    public int PollStepsDone()
    {
        int done = 0;
        foreach (var statusFile in statusSteps)
        {
            if (Status.isDone(statusFile))
            {
                done += 1;
            }
        }

        return done;
    }
}

class MeshroomProgress
{
    static void WaitForCamInitDir(string path)
    {
        var camInitRoot = Path.Combine(path, "CameraInit");

        while (!Directory.Exists(camInitRoot))
        {
            Thread.Sleep(500);
        }

        string nodeDir;
        while ((nodeDir = Meshroom.GetNodeCacheDir(camInitRoot)) == null)
        {
            Thread.Sleep(1500);
        }

        string statusFile = Path.Combine(nodeDir, "status");
        while (!File.Exists(statusFile))
        {
            Thread.Sleep(500);
        }

        while (!Status.isDone(statusFile))
        {
            Thread.Sleep(500);
        }
    }

    static IEnumerable<string> StatusFiles(string CacheDir)
    {
        foreach (var nodeCacheDir in NodeCacheDirs(CacheDir))
        {
            foreach (var status in Directory.GetFiles(nodeCacheDir, "*status"))
            {
                yield return status;
            }
        }
    }

    static IEnumerable<string> NodeCacheDirs(string CacheDir)
    {
        foreach (var nodeRoot in Directory.GetDirectories(CacheDir))
        {
            yield return Meshroom.GetNodeCacheDir(nodeRoot);
        }
    }

    public static ComputeProgress GetPoller(string path)
    {
        var CacheDir = Meshroom.GetCacheDir(path);
        WaitForCamInitDir(CacheDir);
        return new ComputeProgress(StatusFiles(CacheDir));
    }
}
