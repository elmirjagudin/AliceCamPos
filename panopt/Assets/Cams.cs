using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Brab;

public class Cams : MonoBehaviour
{
    const string FILE_EXT = "jpg";

    public Text PositionName;
    public Camera RenderCamera;
    public GameObject CamPrefab;
    public GNSSTransform GNSSTransform;
    public BackgroundImage Background;

    Dictionary<uint, GameObject> CamsPositions = new Dictionary<uint, GameObject>();

    public void Init(string VideoFile, out uint FirstFrame, out uint LastFrame)
    {
        var ImagesDir = PrepVideo.GetImagesDir(VideoFile);

        Background.ImagesDir = ImagesDir;
        Background.FileExt = FILE_EXT;

        var rot180z = Quaternion.Euler(0, 0, 180);

        foreach (var pose in AliceSfm.Load(ImagesDir))
        {
            var frameNum = Parse.Uint(pose.Item1);
            if (CamsPositions.ContainsKey(frameNum))
            {
                /*
                 * don't add overlapping frames between chunks
                 */
                continue;
            }

            var cam = Instantiate(CamPrefab, gameObject.transform);
            cam.name = pose.Item1;

            var unityPos = new Vector3(
                    -pose.Item2[0],
                    pose.Item2[1],
                    pose.Item2[2]);

            cam.transform.position = unityPos;
            cam.transform.rotation = R2Quaternion(pose.Item3) * rot180z;

            CamsPositions.Add(frameNum, cam);
        }

        var frameNums = CamsPositions.Keys.OrderBy(x=>x);
        FirstFrame = frameNums.First();
        LastFrame = frameNums.Last();

        var posFile = PrepVideo.GetPositionsFilePath(VideoFile);
        GNSSTransform.CalcTransform(posFile, frameNums.ToArray(), CamsPositions);
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

    void InterpolateCamPos(uint frameNum, out Vector3 position, out Quaternion rotation)
    {
        uint from;
        for (from = frameNum;
             !CamsPositions.ContainsKey(from);
             from -= 1)
        {
        }

        uint to;
        for (to = frameNum;
             !CamsPositions.ContainsKey(to);
             to += 1)
        {
        }

        var len = (float)to - from;
        var cur = (float)frameNum - from;
        var t = cur/len;

        var fromTrans = CamsPositions[from].transform;
        var toTrans = CamsPositions[to].transform;

        position = Vector3.Lerp(fromTrans.position, toTrans.position, t);
        rotation = Quaternion.Slerp(fromTrans.rotation, toTrans.rotation, t);
    }

    public void GotoFrame(uint FrameNum)
    {
        var frameName = FrameNum.ToString("D4");
        PositionName.text = frameName;
        Background.ShowImage(frameName);

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (CamsPositions.ContainsKey(FrameNum))
        {
            var t = CamsPositions[FrameNum].transform;
            position = t.position;
            rotation = t.rotation;

            PositionName.color = Color.green;
        }
        else
        {
            InterpolateCamPos(FrameNum, out position, out rotation);
            PositionName.color = Color.black;
        }

        var camTrans = RenderCamera.transform;

        camTrans.localPosition = position;
        camTrans.localRotation = rotation;
    }
}
