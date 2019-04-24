using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

/*
 * disable warning "Field `XXX' is never assigned to" for
 * our JSON deserialization object fields
 */
#pragma warning disable 0649
class Metadata
{
    [JsonProperty(PropertyName = "drone-dji:GpsLongtitude")]
    public double longitude;

    [JsonProperty(PropertyName = "drone-dji:GpsLatitude")]
    public double latitude;

    [JsonProperty(PropertyName = "drone-dji:AbsoluteAltitude")]
    public double altitude;
}

class Intrinsic
{
    public string intrinsicId = "1";

    //TODO: this is DUPLICATED at other place in the code
    //TODO: make it DRY
    public string width = "3840";
    public string height = "2160";
    public string serialNumber = "unknown";
    public string type = "radial3";
    public string initializationMode = "unknown";
    public string pxInitialFocalLength = "2720.4401499999999";
    public string pxFocalLength = "2800.1921305708561";
    public string[] principalPoint = new string[] {"1937.4609267594676", "1032.2384580459113"};
    public string[] distortionParams = new string[] {
        "0.00077539660382329283",
        "-0.0094863655387147482",
        "0.011155234182817129"
    };
    public string locked = "0";
}

class View
{
    public string viewId;
    public string poseId;
    public string path;
    public string intrinsicId = "1";
    public Metadata metadata;
}

class Transform
{
    public float[] center;
    public float[] rotation;
}

class Pose
{
    public Transform transform;
}

class PoseDesc
{
    public string poseId;
    public Pose pose;
}

class Sfm
{
    public string[] version = new string[] {"1", "0", "0"};
    public View[] views;
    public PoseDesc[] poses;
    public Intrinsic[] intrinsics = new Intrinsic[] { new Intrinsic {} };
}

#pragma warning restore

public class AliceSfm : MonoBehaviour
{
    static IEnumerable<string> SfmFileNames(string ImagesDir)
    {
        yield return Path.Combine(ImagesDir, "chunk3.sfm");

//         for (int i = 1; ; i += 1)
//         {
//             var path = Path.Combine(ImagesDir, $"chunk{i}_aligned.sfm");
// L.M($"{path} {File.Exists(path)}");

//             if (!File.Exists(path))
//             {
//                 /* no more chunks! */
//                 break;
//             }
//             yield return path;
//         }
    }

    public static IEnumerable<Tuple<string, float[], float[]>> Load(string ImagesDir)
    {
        foreach (var sfmFile in SfmFileNames(ImagesDir))
        {
            foreach (var view in LoadFile(sfmFile))
            {
                yield return view;
            }
        }
    }

    static IEnumerable<Tuple<string, float[], float[]>> LoadFile(string SfmFile)
    {
        var sfm = JsonConvert.DeserializeObject<Sfm>(File.ReadAllText(SfmFile));

        var PoseTransforms = new Dictionary<string, Transform>();
        foreach (var poseDesk in sfm.poses)
        {
            PoseTransforms[poseDesk.poseId] = poseDesk.pose.transform;
        }

        foreach (var view in sfm.views)
        {
            var viewName = Path.GetFileNameWithoutExtension(view.path);
            if (!PoseTransforms.ContainsKey(view.poseId))
            {
                Debug.LogFormat("no view pose for {0}/{1}, skipping", viewName, view.poseId);
                continue;
            }

            var t = PoseTransforms[view.poseId];

            yield return Tuple.Create(viewName, t.center, t.rotation);
        }
    }
}
