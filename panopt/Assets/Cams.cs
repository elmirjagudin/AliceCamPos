using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hagring;
using Brab;
using Brab.Meshroom;

class Chunk
{
    public Dictionary<uint, GameObject> CamsPositions;
    public uint FirstFrame;
    public uint LastFrame;

    /* transfrom from GNSS positions */
    public float scale;
    public Vector3 position;
    public Quaternion rotation;
    public GPSPosition GNSSOrigin;

    public bool IncludesFrame(uint FrameNum)
    {
        return FirstFrame <= FrameNum && FrameNum <= LastFrame;
    }
}

class ChunksSequence
{
    List<Chunk> chunks = new List<Chunk>();

    public void AddChunk(Chunk chunk)
    {
        chunks.Add(chunk);
    }

    public Chunk GetChunk(uint FrameNum)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.IncludesFrame(FrameNum))
            {
                return chunk;
            }
        }

        return null;
    }
}

public class Cams : MonoBehaviour
{
    const string FILE_EXT = "jpg";

    public Text PositionName;
    public Camera RenderCamera;
    public GameObject CamPrefab;
    public GNSSTransform GNSSTransform;
    public BackgroundImage Background;

    ChunksSequence ChunksSequence = new ChunksSequence();
    Chunk CurrentChunk = null;

    Quaternion rot180z;

    public void Init(string VideoFile, out uint FirstFrame, out uint LastFrame)
    {
        var ImagesDir = PrepVideo.GetImagesDir(VideoFile);

        Background.ImagesDir = ImagesDir;
        Background.FileExt = FILE_EXT;

        rot180z = Quaternion.Euler(0, 0, 180);

        var posFile = PrepVideo.GetPositionsFilePath(VideoFile);

        GNSSTransform.InitModels();
        GNSSTransform.CreateGNSSMarkers(posFile);

        FirstFrame = UInt32.MaxValue;
        LastFrame = 0;
        foreach (var viewsChunk in AliceSfm.Load(ImagesDir))
        {
            var chunk = InitChunk(viewsChunk, posFile);
            FirstFrame = Math.Min(FirstFrame, chunk.FirstFrame);
            LastFrame = Math.Max(LastFrame, chunk.LastFrame);

            ChunksSequence.AddChunk(chunk);
        }
    }

    Chunk InitChunk(IEnumerable<(string viewName, float[] center, float[] rotation)> views,
                    string posFile)
    {
        var CamsPositions = new Dictionary<uint, GameObject>();

        foreach (var pose in views)
        {
            var frameNum = Parse.Uint(pose.viewName);

            var cam = Instantiate(CamPrefab, gameObject.transform);
            cam.name = pose.viewName;

            var unityPos = new Vector3(
                    -pose.center[0],
                    pose.center[1],
                    pose.center[2]);

            cam.transform.position = unityPos;
            cam.transform.rotation = R2Quaternion(pose.rotation) * rot180z;

            CamsPositions.Add(frameNum, cam);
        }

        var frameNums = CamsPositions.Keys.OrderBy(x=>x);
        var gnssTrans = GNSSTransform.CalcTransform(posFile, frameNums.ToArray(), CamsPositions);

        return new Chunk
        {
            CamsPositions = CamsPositions,
            FirstFrame = frameNums.First(),
            LastFrame = frameNums.Last(),
            scale = gnssTrans.scale,
            position = gnssTrans.offset,
            rotation = gnssTrans.rotation,
            GNSSOrigin = gnssTrans.GNSSOrigin,
        };
    }

    ///
    /// convert rotation matrix (in AliceVision coordinate system)
    /// to Quaternion (in Unity coordinates system)
    ///
    public static Quaternion R2Quaternion(float[] R)
    {
        var forward = new Vector3(R[2], R[5], R[8]);
        var up = new Vector3(R[1], R[4], R[7]);

        var quat =
            Quaternion.LookRotation(forward, up);
        quat.x = -quat.x;
        quat.w = -quat.w;

        return quat;
    }

    void InterpolateCamPos(Chunk chunk, uint frameNum, out Vector3 position, out Quaternion rotation)
    {
        uint from;
        for (from = frameNum;
             !chunk.CamsPositions.ContainsKey(from);
             from -= 1)
        {
        }

        uint to;
        for (to = frameNum;
             !chunk.CamsPositions.ContainsKey(to);
             to += 1)
        {
        }

        var len = (float)to - from;
        var cur = (float)frameNum - from;
        var t = cur/len;

        var fromTrans = chunk.CamsPositions[from].transform;
        var toTrans = chunk.CamsPositions[to].transform;

        position = Vector3.Lerp(fromTrans.position, toTrans.position, t);
        rotation = Quaternion.Slerp(fromTrans.rotation, toTrans.rotation, t);
    }

    public void GotoFrame(uint FrameNum)
    {
        var frameName = FrameNum.ToString("D4");
        PositionName.text = frameName;
        Background.ShowImage(frameName);

        var chunk = ChunksSequence.GetChunk(FrameNum);
        if (chunk != CurrentChunk)
        {
            CurrentChunk = chunk;
            GNSSTransform.SetTransform(chunk.position, chunk.rotation, chunk.scale, chunk.GNSSOrigin);
        }

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (chunk.CamsPositions.ContainsKey(FrameNum))
        {
            var t = chunk.CamsPositions[FrameNum].transform;
            position = t.position;
            rotation = t.rotation;

            PositionName.color = Color.green;
        }
        else
        {
            InterpolateCamPos(chunk, FrameNum, out position, out rotation);
            PositionName.color = Color.black;
        }

        var camTrans = RenderCamera.transform;

        camTrans.localPosition = position;
        camTrans.localRotation = rotation;
    }

    public void ToggleGNSSMarkers()
    {
        GNSSTransform.ToggleGNSSMarkers();
    }

    // //debug wrapper
    // public void AddTestModels()
    // {
    //     GNSSTransform.AddTestModels();
    // }
}
