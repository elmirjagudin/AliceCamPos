using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

using Hagring;
using Hagring.DJI;

public class Transformer
{
    Matrix<double> T;
    Vector3 CamsOrigin;
    GPSPosition GNSSOrigin;
    int NumPositions;
    int i; // current position, TODO rename to e.g. CurrentPos

    Matrix<double> A;
    Matrix<double> b;

    public Transformer(int NumPositions, Vector3 CamsOrigin, GPSPosition GNSSOrigin)
    {
        this.NumPositions = NumPositions;
        this.CamsOrigin = CamsOrigin;
        this.GNSSOrigin = GNSSOrigin;

        /*
         * each position defined 3 rows in the matrix
         * and matrix have 9 columns
         */
        A = Matrix<double>.Build.Dense(NumPositions * 3, 9);
        b = Matrix<double>.Build.Dense(NumPositions * 3, 1);
    }

    public Vector3 ToCam(GPSPosition GNSSPos)
    {
        var v = GNSSOrigin.GetVector(GNSSPos);

        var s = Matrix<double>.Build.DenseOfArray(new double [,]
        {
            { v.x },
            { v.y },
            { v.z },
        });

        var d = T * s;
        return (new Vector3((float)d[0,0], (float)d[1,0], (float)d[2,0])) + CamsOrigin;
    }

    public void AddPosPair(GPSPosition GNSSPos, Vector3 CamPos)
    {
        var from = GNSSOrigin.GetVector(GNSSPos);

        A[i*3, 0] = from.x;
        A[i*3, 1] = from.y;
        A[i*3, 2] = from.z;

        A[i*3+1, 3] = from.x;
        A[i*3+1, 4] = from.y;
        A[i*3+1, 5] = from.z;

        A[i*3+2, 6] = from.x;
        A[i*3+2, 7] = from.y;
        A[i*3+2, 8] = from.z;

        var to = CamPos - CamsOrigin;
        b[i*3, 0] = to.x;
        b[i*3 + 1, 0] = to.y;
        b[i*3 + 2, 0] = to.z;

        i += 1;
    }

    public void Solve()
    {
        var x = A.Solve(b);

        T = Matrix<double>.Build.DenseOfArray(new double [,]
        {
            { x[0,0], x[1,0], x[2,0] },
            { x[3,0], x[4,0], x[5,0] },
            { x[6,0], x[7,0], x[8,0] },
        });
    }
}

public class GNSSTransform
{
    public static void CalcTransform(string CaptionsFile,
                                     uint[] FrameNums,
                                     Dictionary<uint, GameObject> CamsPositions)
    {
        var Positions = LoadGNSSCoords(CaptionsFile);
        Vector3 CamsOrigin;
        GPSPosition GNSSOrigin;

        GetOrigins(FrameNums, CamsPositions, Positions, out CamsOrigin, out GNSSOrigin);

        var NumPositions = FrameNums.Length / 2;
        if ((FrameNums.Length % 2) != 0)
        {
            NumPositions += 1;
        }

        /*
         * each position defined 3 rows in the matrix
         * and matrix have 9 columns
         */

        int ExtraPos = 1;
        var A = Matrix<double>.Build.Dense((NumPositions + ExtraPos) * 3, 9);
        var b = Matrix<double>.Build.Dense((NumPositions + ExtraPos) * 3, 1);

        Transformer transformer = new Transformer(NumPositions + ExtraPos, CamsOrigin, GNSSOrigin);


        for (int i = 0; i < NumPositions; i += 1)
        {
            var frame = FrameNums[i*2];
            var GNSSPos = GNSSOrigin.GetVector(Positions[GNSSTimeStamp(frame)]);

            transformer.AddPosPair(Positions[GNSSTimeStamp(frame)],
                                   CamsPositions[frame].transform.position);
        }

        /* hack to add 'height' dimension to equations system */
        transformer.AddPosPair(
            new GPSPosition(
                "sweref_99_13_30",
                6176470.36054859, 132025.788479135, 132.890071478672 - 30),
                new Vector3(-1.26f, -0.573f, 2.695f));

        transformer.Solve();

        Test(transformer, Positions.Values);
    }

    static void Test(Transformer transformer, IEnumerable<GPSPosition> Positions)
    {
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "downer";
        cyl.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        cyl.transform.position = new Vector3(-1.26f, -0.573f, 2.695f);

        foreach (var p in Positions)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.position = transformer.ToCam(p);
            sphere.name = p.ToString();
Log.Msg("{0} -> {1}", p, sphere.transform.position);
        }


        GameObject XMark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        XMark.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        var Xp = new GPSPosition("sweref_99_13_30", 6176490.0535561-5, 132029.138267629, 133);
        // A
        //var Xp = new GPSPosition("sweref_99_13_30", 6176483.22, 131949.16, 102.38);
        XMark.transform.position = transformer.ToCam(Xp);
Log.Msg("{0} -> {1}", Xp, XMark.transform.position);
        XMark.name = "A";
    }

    static void GetOrigins(uint[] FrameNums,
                           Dictionary<uint, GameObject> CamsPositions,
                           Dictionary<uint, GPSPosition> GNSSPositions,
                           out Vector3 CamsOrigin,
                           out GPSPosition GNSSOrigin)
    {
        uint first = FrameNums[0];
        CamsOrigin = CamsPositions[first].transform.position;
        GNSSOrigin = GNSSPositions[GNSSTimeStamp(first)];
    }

    static uint GNSSTimeStamp(uint FrameNum)
    {
        var pts =  FrameNum * 1001;
        var ts = (double)(pts) / 30000.0;

        return (uint)Math.Round(ts);
    }

    static Dictionary<uint, GPSPosition> LoadGNSSCoords(string CaptionsFile)
    {
        var cp = new CaptionParser(CaptionsFile);

        var toSweref = GeodesyProjections.fromWGS84Converter("sweref_99_13_30");

        uint TimeStamp;
        double Latitude;
        double Longitude;
        double Altitude;
        float Pitch;
        float Roll;
        float Yaw;

        var Positions = new Dictionary<uint, GPSPosition>();

        while (true)
        {
            try
            {
                cp.ReadPose(out TimeStamp,
                            out Latitude, out Longitude, out Altitude,
                            out Pitch, out Roll, out Yaw);
            }
            catch (EndOfStreamException)
            {
                Log.Msg("done loading captions");
                break;
            }

            Positions.Add(TimeStamp / 1000, toSweref(Longitude, Latitude, Altitude));
        }

        return Positions;
    }
}
