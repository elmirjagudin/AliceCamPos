using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cams : MonoBehaviour
{
    const string IMAGES_DIR = "/home/boris/fsplit";
    const string CAMERAS_SFM = "/home/boris/fsplit/frames/MeshroomCache/StructureFromMotion/4bd22665fda64a8203518022cef32e0deced2f43/cameras.sfm";

    public Text PositionName;
    public Camera RenderCamera;
    public GameObject PhotogramMesh;
    public GameObject CamPrefab;
    public BackgroundImage Background;
    Queue<GameObject> CamsPositions = new Queue<GameObject>();

    void Start()
    {
        Background.ImagesDir = IMAGES_DIR;

        var rot180z = Quaternion.Euler(0, 0, 180);


        foreach (var pose in AliceSfm.Load(CAMERAS_SFM))
        {
            var cam = Instantiate(CamPrefab, gameObject.transform);
            cam.name = pose.Item1;

            var unityPos = new Vector3(
                    -pose.Item2[0],
                    pose.Item2[1],
                    pose.Item2[2]);

            cam.transform.position = unityPos;
            cam.transform.rotation = R2Quaternion(pose.Item3) * rot180z;

            CamsPositions.Enqueue(cam);
        }

        /* order frames by thier name aka number */
        CamsPositions = new Queue<GameObject>(CamsPositions.OrderBy(z => z.name));

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

        var camTrans = RenderCamera.transform;

        camTrans.parent = NextCam.transform;
        camTrans.localPosition = Vector3.zero;
        camTrans.localRotation = Quaternion.identity;
        PositionName.text = NextCam.name;
        Background.ShowImage(NextCam.name);
    }
}
