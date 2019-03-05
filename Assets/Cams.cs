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

    readonly Dictionary<string, Quaternion> Rotations = new Dictionary<string, Quaternion>
    {
        { "m",  new Quaternion(-0.0037116f, -0.0021093f, 0.0014032f, -0.9999899f)},
        { "l",  new Quaternion(-0.0157204f, -0.1185136f, -0.2037793f, -0.97169f)},
        { "j",  new Quaternion(0.0005303f, -0.3360986f, -0.6818714f, -0.6496837f )},
        { "c",  new Quaternion(-0.0010273f, 0.2162388f, 0.4211243f, -0.880848f)},
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
            cam.transform.rotation = Rotations[cam.name];

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
            print("toggle it");
            PhotogramMesh.SetActive(!PhotogramMesh.activeSelf);
        }
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
