using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Hagring;

public class MeshroomCompute
{
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

    static void RunMeshroomCompute(
        string MeshroomComputeBin,
        string ImagesDir,
        string graph,
        ChunkProgress ChunkProgressCB)
    {
        /* remove previous cache dir, if it exists */
        Utils.RemoveDir(Meshroom.GetCacheDir(ImagesDir));

        /* start meshroom compute process */
        var proc = Utils.Run(MeshroomComputeBin, graph);
        var graphName = Path.GetFileNameWithoutExtension(graph);

        var stepsPoller = MeshroomProgress.GetPoller(ImagesDir);
        while (!proc.HasExited)
        {
            var done = stepsPoller.PollStepsDone();
            var total = stepsPoller.TotalSteps;
            ChunkProgressCB((float)done/(float)total);
            Thread.Sleep(1300);
        }

        var exitCode = proc.ExitCode;
        if (exitCode != 0)
        {
            var err = string.Format("{0} failed, exit code {1}", MeshroomComputeBin, exitCode);
            throw new Exception(err);
        }

        CopyCamerasSFMFile(ImagesDir, graphName);
    }

    public static void PhotogrammImages(
            string MeshroomComputeBin,
            string SensorDatabase, string VocTree, string ImagesDir,
            TimeBase TimeBase, uint LastFrame,
            ComputeProgress ComputeProgressCB)
    {
        var chunks = Chunks.GetChunks(TimeBase, LastFrame);
        var graphs = MeshroomGraphs(SensorDatabase, VocTree, ImagesDir, chunks);

        int chunkNum = 0;
        foreach (var graph in graphs)
        {
            RunMeshroomCompute(MeshroomComputeBin, ImagesDir, graph,
                               (done) => ComputeProgressCB(chunkNum, done));
            chunkNum += 1;
        }

        /* clean-up last meshroom cache directory */
        Utils.RemoveDir(Meshroom.GetCacheDir(ImagesDir));
    }
}