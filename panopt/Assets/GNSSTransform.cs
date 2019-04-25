using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

using Hagring;
using Hagring.DJI;

class Scaler
{
    Vector3 CamsOrigin;
    GPSPosition GNSSOrigin;

    List<(Vector3 gnss, Vector3 sfm)> PositionPairs =
        new List<(Vector3 gnss, Vector3 sfm)>();

    List<(float gnss, float sfm)> Magnitudes =
        new List<(float gnss, float sfm)>();

    public Scaler(Vector3 CamsOrigin, GPSPosition GNSSOrigin)
    {
        this.CamsOrigin = CamsOrigin;
        this.GNSSOrigin = GNSSOrigin;
    }

    public void AddPosPair(GPSPosition GNSSPos, Vector3 CamPos)
    {
        var from = GNSSOrigin.GetVector(GNSSPos);
        var to = CamPos - CamsOrigin;

        var newPosPair = (gnss: from, sfm: to);

        foreach (var posPair in PositionPairs)
        {
            var gnss = posPair.gnss - newPosPair.gnss;
            var sfm = posPair.sfm - newPosPair.sfm;
            Magnitudes.Add((gnss: gnss.magnitude, sfm: sfm.magnitude));

        }
        PositionPairs.Add(newPosPair);
    }

    void DumpMagnitudes()
    {
        string gnss = "";
        string sfm = "";

        int i = 0;
        foreach (var mag in Magnitudes)
        {
            i += 1;

            gnss += mag.gnss + "; ";
            sfm += mag.sfm + "; ";
        }

        File.WriteAllText("/home/boris/mags.m",
            string.Format("gnss = [{0}]\nsfm = [{1}]", gnss, sfm));

    }

    public double Calculate()
    {
        var gnss = Vector<double>.Build.Dense(Magnitudes.Count);
        var sfm = Vector<double>.Build.Dense(Magnitudes.Count);

        DumpMagnitudes();

        /*
         * perform least squares fitting between
         * GNSS and SFM magnitudes
         */
        int i = 0;
        foreach (var mag in Magnitudes)
        {
            gnss[i] = mag.gnss;
            sfm[i] = mag.sfm;
            i += 1;
        }

        var scale = (sfm*gnss) / (gnss*gnss);

        return scale;
    }
}

class Rotator
{
    Vector3 CamsOrigin;
    GPSPosition GNSSOrigin;
    float Scale;
    Matrix<double> B;

    public Rotator(Vector3 CamsOrigin, GPSPosition GNSSOrigin, float Scale)
    {
        this.CamsOrigin = CamsOrigin;
        this.GNSSOrigin = GNSSOrigin;
        this.Scale = Scale;

        B = Matrix<double>.Build.Dense(3, 3);
    }

    public void AddPosPair(GPSPosition GNSSPos, Vector3 CamPos)
    {
        var from = GNSSOrigin.GetVector(GNSSPos) * Scale;
        var to = CamPos - CamsOrigin;

        var v = Matrix<double>.Build.Dense(3, 1,
            new double[] {to.x, to.y, to.z}
        );

        var w = Matrix<double>.Build.Dense(1, 3,
            new double[] {from.x, from.y, from.z}
        );

        B = B + (v * w);
    }

    static Quaternion R2Quaternion(Matrix<double> R)
    {
        var forward = new Vector3((float)R[0,2], (float)R[1,2], (float)R[2,2]);
        var up = new Vector3((float)R[0,1], (float)R[1,1], (float)R[2,1]);

        var quat = Quaternion.LookRotation(forward, up);

        return quat;
    }

    public Quaternion Calculate()
    {
        var svd = B.Svd();
        var U = svd.U;
        var VT = svd.VT;

        var M = Matrix<double>.Build.Diagonal(3, 3,
            new double[] {1.0, 1.0, U.Determinant() * VT.Determinant()});

        var R = U * M * VT;

        return R2Quaternion(R);
    }
}

class Transformer
{
    Vector3 CamsOrigin;
    GPSPosition GNSSOrigin;
    float Scale;
    Quaternion Rotation;

    public Transformer(Vector3 CamsOrigin, GPSPosition GNSSOrigin, float Scale, Quaternion Rotation)
    {
        this.CamsOrigin = CamsOrigin;
        this.GNSSOrigin = GNSSOrigin;
        this.Scale = Scale;
        this.Rotation = Rotation;
    }

    public Vector3 ToSfm(GPSPosition GNSSPos)
    {
        var from = GNSSOrigin.GetVector(GNSSPos);
        var to = (Rotation * (from * Scale)) + CamsOrigin;

        return to;
    }
}

public class GNSSTransform : MonoBehaviour
{
    public GameObject GNSSMarkers;
    public GameObject SFMParent;
    public GameObject GNSSPrefab;
    public GameObject SFMPrefab;
    public GameObject RedBall;

    List<(GPSPosition pos, GameObject obj)> Models = new List<(GPSPosition pos, GameObject obj)>();

    public
        (Quaternion rotation, float scale, Vector3 offset, GPSPosition GNSSOrigin) CalcTransform(
            string CaptionsFile, uint[] FrameNums, Dictionary<uint, GameObject> SFMPositions)
    {
        Vector3 CamsOrigin;
        GPSPosition GNSSOrigin;

        var GNSSPositions = LoadGNSSCoords(CaptionsFile);

        GetOrigins(FrameNums, SFMPositions, GNSSPositions, out CamsOrigin, out GNSSOrigin);

        var NumPositions = FrameNums.Length / 2;
        Scaler scaler = new Scaler(CamsOrigin, GNSSOrigin);

        foreach (var posPair in PositionPairs(FrameNums, SFMPositions, GNSSPositions))
        {
            scaler.AddPosPair(posPair.gnss, posPair.sfm);
        }

        var scale = scaler.Calculate();
        Rotator rotator = new Rotator(CamsOrigin, GNSSOrigin, (float)scale);
        foreach (var posPair in PositionPairs(FrameNums, SFMPositions, GNSSPositions))
        {
            rotator.AddPosPair(posPair.gnss, posPair.sfm);
        }

        var rotation = rotator.Calculate();

        var transformer = new Transformer(CamsOrigin, GNSSOrigin, (float)scale, rotation);

        return (rotation, (float)scale, CamsOrigin, GNSSOrigin);
    }

    public void ToggleGNSSMarkers()
    {
        GNSSMarkers.SetActive(!GNSSMarkers.activeSelf);
    }

    public void CreateGNSSMarkers(string CaptionsFile)
    {
        foreach (var item in GetGNSSCoords(CaptionsFile))
        {
            var pos = item.pos;
            var below = new GPSPosition(pos.Projection, pos.North, pos.East, pos.Altitude - item.RelHeight);

            var obj = Instantiate(RedBall);
            obj.name = $"{item.ts}: {below}";
            AddObject(below, obj);
            obj.transform.parent = GNSSMarkers.transform;
        }
    }

    public void SetTransform(Vector3 position, Quaternion rotation, float scale, GPSPosition GNSSOrigin)
    {
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        gameObject.transform.localScale = Vector3.one * scale;

        foreach (var op in Models)
        {
            var obj = op.obj;
            var pos = op.pos;
            obj.transform.localPosition = GNSSOrigin.GetVector(pos);
        }
    }

    IEnumerable<(Vector3 sfm, GPSPosition gnss)> PositionPairs(
        uint[] FrameNums,
        Dictionary<uint, GameObject> SFMPositions,
        Dictionary<uint, GPSPosition> GNSSPositions)
    {
        /*
         * figure out a good start frame,
         * so that we get close in time to recorded GNSS coordinates
         *
         * GNSS coordinates are recorded at N+0.5 seconds in the
         * (note: subtittles timestamp seems to be 0.5 seconds wrong)
         *
         * if first frames timestamp is close N+0.5, use it
         * otherwise, pick next frame (as we have ~2FPS here)
         */
        var firstFrame = FrameNums[0];
        var pts = firstFrame * 1001;
        var ts = ((double)pts) / 30000.0;
        var frac = ts % 1.0;
        bool closeToHalf = frac > 0.25 && frac <= 0.75;

        int j = closeToHalf ? 0 : 1;
        for (;j < FrameNums.Length; j += 2)
        {
            var frame = FrameNums[j];
            var sfm = SFMPositions[frame].transform.position;
            var gnss = GNSSPositions[GNSSTimeStamp(frame)];

            yield return (sfm: sfm, gnss: gnss);
        }
    }

    void AddPosition(string name, double Longitude, double Latitude, double Altitude)
    {
        var toSweref = GeodesyProjections.fromWGS84Converter("sweref_99_13_30");
        AddPosition(name, toSweref(Longitude, Latitude, Altitude), PrimitiveType.Cylinder);
    }

    void AddPosition(string name, GPSPosition pos,
                     PrimitiveType objType = PrimitiveType.Sphere)
    {
        var obj = GameObject.CreatePrimitive(objType);
        obj.name = name;
        AddObject(pos, obj);
    }

    void AddPositionRB(string name, GPSPosition pos)
    {
        var obj = Instantiate(RedBall);
        obj.name = name;
        AddObject(pos, obj);
    }

    void AddObject(GPSPosition pos, GameObject obj)
    {
        obj.transform.parent = gameObject.transform;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        Models.Add((pos, obj));
    }

    public void AddTestModels()
    {
        // foreach (var p in Positions)
        // {
        //     var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        //     c.transform.position = transformer.ToSfm(p);
        //     c.transform.localScale = Vector3.one * 0.05f;
        //     c.name = p.ToString();
        // }

        AddPosition("S", 13.21285566, 55.71090721, 102.3);
        AddPosition("O", 13.21270959, 55.71090706, 102.3);
        AddPosition("P", 13.21271005, 55.71095111, 102.3);
        AddPosition("R", 13.21278098, 55.71095192, 102.3);
        AddPosition("Q", 13.21285350, 55.71095194, 102.3);

        AddPosition("Pnt1",  13.21404847, 55.71092960, 103.2 + .4);
        AddPositionRB("APnt1",
                 new GPSPosition("sweref_99_13_30", 6176415.2 + .25, 132025.7 - .26, 103.6));

        AddPosition("Pnt3",  13.21392685, 55.71085484, 103.2 + .4);
        AddPositionRB("APnt3",
                new GPSPosition("sweref_99_13_30", 6176406.9 + .25, 132018.1 - .26, 103.6));

        AddPosition("Pnt5",  13.21380042, 55.71085481, 103.2 + .4);
        AddPositionRB("APnt5",
                new GPSPosition("sweref_99_13_30", 6176406.9 + .24, 132010.1 - .18, 103.6));

        AddPosition("Pnt8",  13.21372798, 55.71085486, 103.1 + .4);
        AddPositionRB("APnt8",
                new GPSPosition("sweref_99_13_30", 6176407 + .24, 132005.6 - .18, 103.5));

        AddPosition("Pnt11", 13.21386312, 55.71097488, 103.1 + .4);
        AddPositionRB("APnt11",
                new GPSPosition("sweref_99_13_30", 6176420.3 + .21, 132014.1 - .17, 103.5));

        AddPosition("Pnt13", 13.21367242, 55.71092992, 102.9 + .4);
        AddPositionRB("APnt13",
                new GPSPosition("sweref_99_13_30", 6176415.3 + .21, 132002.1- .17, 103.3));

        AddPosition("Pnt14", 13.21351337, 55.71099739, 103.0 + .4);
        AddPosition("Pnt16", 13.21324542, 55.71090776, 102.7 + .4);
        AddPosition("Pnt18", 13.21324485, 55.71093010, 102.7 + .4);
        AddPosition("Pnt20", 13.21313172, 55.71090771, 102.5 + .4);
        AddPosition("Pnt22", 13.21305239, 55.71090753, 102.5 + .4);
        AddPosition("Pnt24", 13.21297380, 55.71090752, 102.5 + .4);
        AddPosition("Pnt26", 13.21312557, 55.71075311, 102.5 + .4);
        AddPosition("Pnt28", 13.21316558, 55.71075306, 102.5 + .4);
        AddPosition("Pnt30", 13.21320524, 55.71075316, 102.6 + .4);


        // for (int i = 1; i < 11; i += 1)
        // {
        //     AddPosition("P" + i,
        //         new GPSPosition("sweref_99_13_30", 6176417.94631892+i*2.5, 131941.659494839, 102.3));
        //     AddPosition("R" + i,
        //         new GPSPosition("sweref_99_13_30", 6176418.01803302+i*2.5, 131946.118340288, 102.3));
        //     AddPosition("Q" + i,
        //         new GPSPosition("sweref_99_13_30", 6176418.00138237+i*2.5, 131950.676764551, 102.3));
        // }
    }

    void GetOrigins(uint[] FrameNums,
                    Dictionary<uint, GameObject> CamsPositions,
                    Dictionary<uint, GPSPosition> GNSSPositions,
                    out Vector3 CamsOrigin,
                    out GPSPosition GNSSOrigin)
    {
        uint originFrame = FrameNums[0];
        //uint originFrame = FrameNums[FrameNums.Length / 2];
        //uint originFrame = FrameNums[FrameNums.Length - 1];
        CamsOrigin = CamsPositions[originFrame].transform.position;
        GNSSOrigin = GNSSPositions[GNSSTimeStamp(originFrame)];
    }

    static uint GNSSTimeStamp(uint FrameNum)
    {
        var pts = FrameNum * 1001;
        var ts = ((double)(pts) / 30000.0) - .5;

        return (uint)Math.Round(ts);
    }

    public static Dictionary<uint, GPSPosition> LoadGNSSCoords(string CaptionsFile)
    {
        var Positions = new Dictionary<uint, GPSPosition>();
        foreach (var entry in GetGNSSCoords(CaptionsFile))
        {
            Positions[entry.ts] = entry.pos;
        }

        return Positions;
    }

    public static IEnumerable<(uint ts, GPSPosition pos, double RelHeight)> GetGNSSCoords(string CaptionsFile)
    {
        var cp = new CaptionParser(CaptionsFile);

        var toSweref = GeodesyProjections.fromWGS84Converter("sweref_99_13_30");

        uint TimeStamp;
        double Latitude;
        double Longitude;
        double Altitude;
        double RelativeHeight;
        float Pitch;
        float Roll;
        float Yaw;

        var Positions = new Dictionary<uint, GPSPosition>();

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
                /* done loading captions */
                break;
            }

            yield return (TimeStamp / 1000, toSweref(Longitude, Latitude, Altitude), RelativeHeight);
        }
    }
}
