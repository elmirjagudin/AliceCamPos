using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIDispatcher : MonoBehaviour
{
    public Text VideoLabel;
    public ProgressBar ProgressBar;

    void Awake()
    {
        SourceVideo.VideoSwitchedEvent += SetCurrentVideo;
        SourceVideo.ImportStartedEvent += ShowProgressBar;
        SourceVideo.ImportProgressEvent += UpdateProgressBar;
        SourceVideo.ImportFinishedEvent += HideProgressBar;
    }

    void SetCurrentVideo(string fileName)
    {
        VideoLabel.text = Path.GetFileName(fileName);
    }

    void ShowProgressBar(SourceVideo.CancelImport CancelImport)
    {
        MainThreadRunner.Run(() => ProgressBar.Show(() => CancelImport()));
    }

    void UpdateProgressBar(string stepName, float done)
    {
        MainThreadRunner.Run(() => ProgressBar.SetProgress(stepName, done));
    }

    void HideProgressBar()
    {
        MainThreadRunner.Run(() => ProgressBar.Hide());
    }

}
