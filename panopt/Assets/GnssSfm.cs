using System.IO;
using System.Collections.Generic;
using Hagring;
using Hagring.DJI;
using Newtonsoft.Json;

public class GnssSfm
{
    public static void MakeSfm(string videoFile)
    {
        var views = new List<View>();
        var poses = new List<PoseDesc>();

        foreach (var vp in GetViewPoses(videoFile))
        {
            views.Add(vp.view);
            poses.Add(vp.pose);
        }

        var sfm = new Sfm
        {
            views = views.ToArray(),
            poses = poses.ToArray()
        };

        var json = JsonConvert.SerializeObject(sfm, Formatting.Indented);
        File.WriteAllText("/home/boris/area51/sfmAlign/panopt/falafel_low/gnss.sfm", json);
    }

    static (View view, PoseDesc pose) GnssToViewPose(GPSPosition origin, GPSPosition gnssPos, uint TimeStamp)
    {
        var frameNum = FrameChunks.FrameCloseTo(TimeStamp + 500, SourceVideo.TIME_BASE);
        var id = ViewIDs.Get(frameNum);

        var pos = origin.GetVector(gnssPos);
        var pose = new PoseDesc
        {
            poseId = $"{id}",
            pose = new Pose
            {
                transform = new Transform
                {
                    center = new float[] {pos.x, -pos.y, pos.z},
                    rotation = new float[]
                    {
                        1, 0, 0,
                        0, 1, 0,
                        0, 0, 1,
                    },
                }
            }
        };

        var view = new View
        {
            viewId = pose.poseId,
            poseId = pose.poseId,
            path = frameNum.ToString("D4") + ".jpg",
            metadata = new Metadata {},
        };

        return (view, pose);
    }

    static IEnumerable<(View view, PoseDesc pose)> GetViewPoses(string videoFile)
    {
        var toSweref = GeodesyProjections.fromWGS84Converter("sweref_99_13_30");
        var posFile = PrepVideo.GetPositionsFilePath(videoFile);
        var cp = new CaptionParser(posFile);

        uint TimeStamp;
        double Latitude;
        double Longitude;
        double Altitude;
        double RelativeHeight;
        float Pitch;
        float Roll;
        float Yaw;

        cp.ReadPose(out TimeStamp,
                    out Latitude, out Longitude, out Altitude, out RelativeHeight,
                    out Pitch, out Roll, out Yaw);
        var origin = toSweref(Longitude, Latitude, Altitude);
        yield return GnssToViewPose(origin, origin, TimeStamp);

        while (true)
        {
            try
            {
                cp.ReadPose(out TimeStamp,
                            out Latitude, out Longitude, out Altitude, out RelativeHeight,
                            out Pitch, out Roll, out Yaw);
            }
            catch (EndOfStreamException)
            {
                Log.Msg("GnssSfm: done loading captions");
                break;
            }

            var gnssPos = toSweref(Longitude, Latitude, Altitude);
            yield return GnssToViewPose(origin, gnssPos, TimeStamp);
        }
    }
}