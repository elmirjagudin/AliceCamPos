using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hagring;

public class SourceVideo
{
    static TimeBase TIME_BASE = new TimeBase { Numerator = 1001, Denominator = 30000 };

    public delegate void VideoSwitched(string fileName);
    public static event VideoSwitched VideoSwitchedEvent;

    public delegate void ImportStarted();
    public static event ImportStarted ImportStartedEvent;

    public delegate void ImportProgress(string stepName, float done);
    public static event ImportProgress ImportProgressEvent;

    public delegate void ImportFinished();
    public static event ImportFinished ImportFinishedEvent;

    /* hard-coded for now, TODO: package on in StreamingAssets and use that? */
    const string FFMPEG_BIN = "/usr/bin/ffmpeg";
    const string MESHROOM_COMPUTE_BIN = "/home/boris/Meshroom-2019.1.0/meshroom_compute";
    const string SENSOR_DATABASE = "/home/boris/Meshroom-2019.1.0/aliceVision/share/aliceVision/cameraSensors.db";
    const string VOC_TREE = "/home/boris/Meshroom-2019.1.0/aliceVision/share/aliceVision/vlfeat_K80L3.SIFT.tree";

    static void SplitProgress(float done)
    {
        ImportProgressEvent?.Invoke("importing: extracting frames", done);
    }

    static void MeshroomProgress(int chunkNum, float done)
    {
        ImportProgressEvent?.Invoke($"importing: analyzing segment {chunkNum+1}", done);
    }

    static void ImportVideo(string fileName)
    {
        ImportStartedEvent?.Invoke();

        uint NumFrames;
        string ImagesDir;

        PrepVideo.SplitFrames(
            FFMPEG_BIN, fileName, SplitProgress, out NumFrames, out ImagesDir);
        PrepVideo.ExtractSubtitles(FFMPEG_BIN, fileName);

        MeshroomCompute.PhotogrammImages(
            MESHROOM_COMPUTE_BIN, SENSOR_DATABASE, VOC_TREE,
            ImagesDir, TIME_BASE, NumFrames, MeshroomProgress);

        ImportFinishedEvent?.Invoke();
    }

    public static void Open(string fileName)
    {
        VideoSwitchedEvent?.Invoke(fileName);

        Utils.StartThread(() => ImportVideo(fileName), "ImportVideo");
    }
}

