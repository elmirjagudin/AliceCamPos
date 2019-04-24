using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hagring;

public class SourceVideo
{
    public static TimeBase TIME_BASE = new TimeBase { Numerator = 1001, Denominator = 30000 };

    public delegate void CancelImport();

    public delegate void VideoOpened(string videoFile);
    public static event VideoOpened VideoOpenedEvent;

    public delegate void ImportStarted(string videoFile, CancelImport CancelImport);
    public static event ImportStarted ImportStartedEvent;

    public delegate void ImportProgress(string stepName, float done);
    public static event ImportProgress ImportProgressEvent;

    public delegate void ImportCanceled();
    public static event ImportCanceled ImportCanceledEvent;

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

    static void ImportVideo(string videoFile)
    {
        AutoResetEvent AbortEvent = new AutoResetEvent(false);
        ImportStartedEvent?.Invoke(
            videoFile,
            () => AbortEvent.Set());

        try
        {
            uint NumFrames;

            PrepVideo.SplitFrames(
                FFMPEG_BIN, videoFile, SplitProgress, AbortEvent,
                out NumFrames);

            MeshroomCompute.PhotogrammImages(
                MESHROOM_COMPUTE_BIN, SENSOR_DATABASE, VOC_TREE,
                PrepVideo.GetImagesDir(videoFile),
                TIME_BASE, NumFrames, MeshroomProgress,
                AbortEvent);

            /*
             * create positions file last, we use it to
             * figure out if a video have been imported
             */
            PrepVideo.ExtractSubtitles(FFMPEG_BIN, videoFile, AbortEvent);

            ImportFinishedEvent?.Invoke();
            VideoOpenedEvent?.Invoke(videoFile);
        }
        catch (ProcessAborted)
        {
            ImportCanceledEvent?.Invoke();
        }
    }

    static bool IsImported(string videoFile)
    {
        /*
         * we check the presens of positions file to
         * figure out if the video have been imported
         */
        var positionsFile = PrepVideo.GetPositionsFilePath(videoFile);
        return File.Exists(positionsFile);
    }

    public static void Open(string videoFile)
    {
        if (IsImported(videoFile))
        {
            VideoOpenedEvent?.Invoke(videoFile);
            return;
        }

        /* start the import process */
        Utils.StartThread(() => ImportVideo(videoFile), "ImportVideo");
    }
}

