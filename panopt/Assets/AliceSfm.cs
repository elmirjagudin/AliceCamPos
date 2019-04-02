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

class View
{
    public string poseId;
    public string path;
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
    public View[] views;
    public PoseDesc[] poses;
}

#pragma warning restore

public class AliceSfm : MonoBehaviour
{
    public static IEnumerable<Tuple<string, float[], float[]>> Load(string SfmFile)
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
