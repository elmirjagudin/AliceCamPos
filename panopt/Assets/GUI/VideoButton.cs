using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class VideoButton : MonoBehaviour
{
    string LastPickedDirectory = "";

    string GetFilePath()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Pick Video...", LastPickedDirectory, "mov", false);

        if (paths.Length == 0)
        {
            return null;
        }

        if (paths.Length == 1 && paths[0].Length == 0)
        {
            /* sometimes we get empty string when open dialog is canceled!? */
            return null;
        }

        return paths[0];
    }

    public void PickVideo()
    {
        var path = GetFilePath();
        if (path == null)
        {
            /* open dialog canceled */
            return;
        }

        LastPickedDirectory = Path.GetDirectoryName(path);
        SourceVideo.Open(path);
    }
}
