using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTexRecorder : MonoBehaviour
{
    public RenderTexture renderTexture;
    long pts = 0;
    Texture2D cpuTex;
    Recorder recorder;

    public void StartRecording(string VideoFile)
    {
        cpuTex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        recorder = new Recorder(
            VideoFile,
            renderTexture.width, renderTexture.height,
            1, 30000);

        gameObject.SetActive(true);
    }

    public void StopRecording()
    {
        gameObject.SetActive(false);
    }

    static public Texture2D GetRTPixels(Texture2D tex, RenderTexture rt)
    {
        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the supplied RenderTexture as the active one
        RenderTexture.active = rt;

        // read the RenderTexture image into Texture2D
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

        // Restorie previously active render texture
        RenderTexture.active = currentActiveRT;
        return tex;
    }

    void Update()
    {
        GetRTPixels(cpuTex, renderTexture);

        var data = cpuTex.GetRawTextureData<byte>();
        recorder.Encode(data, pts);
        pts += 1001;
    }

    void OnDisable()
    {
        recorder.Close();
    }
}
