using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class VideoButton : MonoBehaviour
{
    string GetFilePath()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Pick Video...", Persisted.LastUsedDirectory, "mov", false);

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

        Persisted.LastUsedDirectory = Path.GetDirectoryName(path);
        SourceVideo.Open(path);
    }
}
