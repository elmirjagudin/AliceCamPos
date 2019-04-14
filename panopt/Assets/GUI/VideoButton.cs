using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class VideoButton : MonoBehaviour
{
    public Text VideoLabel;

    string LastPickedDirectory = "";

    public void PickVideo()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Pick Video...", LastPickedDirectory, "mov", false);

        if (paths.Length == 0)
        {
            /* open dialog canceled */
            return;
        }

        var pickedPath = paths[0];
        var pickedFileName = Path.GetFileName(pickedPath);
        LastPickedDirectory = Path.GetDirectoryName(pickedPath);

        VideoLabel.text = pickedFileName;
    }
}
