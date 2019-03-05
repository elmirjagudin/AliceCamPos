using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cams : MonoBehaviour
{
    readonly Dictionary<string, double[]> Positions = new Dictionary<string, double[]>
    {
        { "m", new double[] {0.012067748590025565, -0.0068059954403798173, -0.010467159701220044} },
        { "l", new double[] {0.80106467863888609, -0.11996854878777954, -0.0091171160389423855} },
        { "j", new double[] {1.3567943876231396, -1.567706274314965, 0.59490061318273824} },
        { "c", new double[] {-1.3935643329081679, -0.66734064840186891, 0.50418276390659389} },
    };

    readonly Dictionary<string, float[]> Rotations = new Dictionary<string, float[]>
    {
        {
            "m",
            new float[]
            {
                0.99998716344830074f,    -0.0028221205726920592f,  -0.0042081556642446947f,
                0.0027908050879879675f,   0.99996851009488064f,    -0.0074290124232838936f,
                0.0042289887186166544f,   0.007417172918143017f,    0.99996354993585645f
            }
        },
        {
           "l",
            new float[]
            {
                0.88885703247999037f, 0.39229448087793339f, -0.23672392376685986f,
                -0.39974680480282654f, 0.91645371442213075f,  0.017750531592335202f,
                0.2239099548050425f, 0.078851947310084741f, 0.97141479427925714f
            }
        },
        {
            "j",
            new float[]
            {
                -0.15582171467731171f, 0.88635784361611636f,  -0.4359923924740095f,
                -0.88564495419384448f, 0.070102263711310786f, 0.45904105234012887f,
                0.43743869095680771f, 0.45766302634442529f, 0.77407489687449282f
            }
        },
        {
            "c",
            new float[]
            {
                0.55179016647924173f, -0.74144914716618349f, 0.38181248584003902f,
                0.74233767765774394f, 0.64530651700376207f, 0.18031714128773441f,
               -0.38008207601321864f, 0.18393656862900487f, 0.90647942845630491f
            }
        },
    };

    public Text PositionName;
    public GameObject PhotogramMesh;
    public GameObject CamPrefab;
    public BackgroundImage Background;
    Queue<GameObject> CamsPositions = new Queue<GameObject>();

    void Start()
    {
        Background.ImagesDir = @"C:\Users\brab\Desktop\redFootstool";

        foreach (var item in Positions)
        {
            var cam = Instantiate(CamPrefab, gameObject.transform);
            cam.name = item.Key;

            var unityPos = new Vector3(
                (float)-item.Value[0],
                (float)item.Value[1],
                (float)item.Value[2]);

            cam.transform.position = unityPos;
            cam.transform.rotation = R2Quaternion(Rotations[cam.name]);

            CamsPositions.Enqueue(cam);
        }

        GotoNextCam();
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            GotoNextCam();
        }

        if (Input.GetKeyDown("t"))
        {
            PhotogramMesh.SetActive(!PhotogramMesh.activeSelf);
        }
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

    void GotoNextCam()
    {
        var NextCam = CamsPositions.Dequeue();
        CamsPositions.Enqueue(NextCam);

        var mainCamTrans = Camera.main.transform;

        mainCamTrans.parent = NextCam.transform;
        mainCamTrans.localPosition = Vector3.zero;
        mainCamTrans.localRotation = Quaternion.identity;
        PositionName.text = NextCam.name;
        Background.ShowImage(NextCam.name);
    }
}
