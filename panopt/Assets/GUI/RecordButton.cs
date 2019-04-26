using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class RecordButton : MonoBehaviour
{
    const string DEFAULT_REC_FILE = "panopticon.mov";

    public Frames Frames;

    string GetFilePath(string LastUsedFile)
    {
        string directory = "";
        string defaultFile = DEFAULT_REC_FILE;

        if (LastUsedFile.Length > 0)
        {
            directory = Path.GetDirectoryName(LastUsedFile);
            defaultFile = Path.GetFileName(LastUsedFile);
        }

        var path = StandaloneFileBrowser.SaveFilePanel(
            "Record to...", directory, defaultFile, "mov");

        if (path.Length == 0)
        {
            return null;
        }

        return path;
    }

    public void PickTargetFile()
    {
        var recFile = GetFilePath(Persisted.LastRecordingFile);
        if (recFile == null)
        {
            /* open dialog canceled */
            return;
        }

        Persisted.LastRecordingFile = recFile;
        Frames.StartRecording(recFile);
    }
}
