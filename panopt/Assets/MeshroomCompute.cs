using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Hagring;

public class MeshroomCompute
{
    const int POLL_FREQ = 1300; /* miliseconds */
    public delegate void ComputeProgress(int chunkNum, float done);

    delegate void ChunkProgress(float done);

    static IEnumerable<string> Images(string ImgsDir, IEnumerable<uint> frameNums)
    {
        foreach (var frame in frameNums)
        {
            var fname = string.Format("{0:D4}.jpg", frame);
            yield return Path.Combine(ImgsDir, fname);
        }
    }

    static IEnumerable<string> MeshroomGraphs(string SensorDatabase, string VocTree, string ImagesDir, IEnumerable<IEnumerable<uint>> Chunks)
    {
        uint chunkNum = 0;
        foreach (var chunk in Chunks)
        {
            var graphFile = Path.Combine(ImagesDir, string.Format("chunk{0}.mg", chunkNum));
            var graph = new PipelineJson(2720.44015, 3840, 2160, Images(ImagesDir, chunk),
                        SensorDatabase, VocTree);
            graph.WriteToFile(graphFile);

            yield return graphFile;

            chunkNum += 1;
        }
    }

    static void CopyCamerasSFMFile(string ImagesDir, string graphName)
    {
        var MeshroomCacheDir = Meshroom.GetCacheDir(ImagesDir);
        var SfmCacheDir =
            Meshroom.GetNodeCacheDir(
                Path.Combine(MeshroomCacheDir, "StructureFromMotion"));

        var camsSfmFile = Path.Combine(SfmCacheDir, "cameras.sfm");
        var destFile = Path.Combine(ImagesDir, $"{graphName}.sfm");

        File.Copy(camsSfmFile, destFile, true);
    }

    static int hackCntr = 0;

    static void RemoveCacheDir(string ImagesDir)
    {
        var cacheDir = Meshroom.GetCacheDir(ImagesDir);
        if (Directory.Exists(cacheDir))
        {
            var destDir = Path.Combine(ImagesDir, $"MeshroomCache{hackCntr++}");
L.M($"'{cacheDir}' -> '{destDir}'");
            Directory.Move(cacheDir, destDir);
        }
        Utils.RemoveDir(cacheDir);
    }

    static void RunMeshroomCompute(
        string MeshroomComputeBin,
        string ImagesDir,
        string graph,
        ChunkProgress ChunkProgressCB,
        AutoResetEvent AbortEvent)
    {
        /* remove previous cache dir, if it exists */
        RemoveCacheDir(ImagesDir);

        /* start meshroom compute process */
        var runner = new ProcRunner(MeshroomComputeBin, graph);
        runner.StartAsync();

        /* poll progress until finished/aborted */
        var stepsPoller = MeshroomProgress.GetPoller(ImagesDir);
        var graphName = Path.GetFileNameWithoutExtension(graph);
        while (runner.IsRunning(AbortEvent, POLL_FREQ))
        {
            var done = stepsPoller.PollStepsDone();
            var total = stepsPoller.TotalSteps;
            ChunkProgressCB((float)done/(float)total);
        }

        CopyCamerasSFMFile(ImagesDir, graphName);
    }

    public static void PhotogrammImages(
            string MeshroomComputeBin,
            string SensorDatabase, string VocTree, string ImagesDir,
            TimeBase TimeBase, uint LastFrame,
            ComputeProgress ComputeProgressCB,
            AutoResetEvent AbortEvent)
    {
        var chunks = FrameChunks.GetChunks(TimeBase, LastFrame);
        var graphs = MeshroomGraphs(SensorDatabase, VocTree, ImagesDir, chunks);

        int chunkNum = 0;
        foreach (var graph in graphs)
        {
            RunMeshroomCompute(MeshroomComputeBin, ImagesDir, graph,
                               (done) => ComputeProgressCB(chunkNum, done),
                               AbortEvent);
            chunkNum += 1;
        }

        /* clean-up last meshroom cache directory */
        RemoveCacheDir(ImagesDir);
    }
}