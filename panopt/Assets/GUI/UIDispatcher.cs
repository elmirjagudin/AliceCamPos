using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIDispatcher : MonoBehaviour
{
    public Text VideoLabel;
    public ProgressBar ProgressBar;

    void Awake()
    {
        SourceVideo.VideoOpenedEvent += SetCurrentVideo;
        SourceVideo.ImportStartedEvent += HandleImportStarted;
        SourceVideo.ImportProgressEvent += UpdateProgressBar;
        SourceVideo.ImportFinishedEvent += HideProgressBar;
    }

    void SetCurrentVideo(string videoFile)
    {
        VideoLabel.text = Path.GetFileName(videoFile);
    }

    void HandleImportStarted(string videoFile, SourceVideo.CancelImport CancelImport)
    {
        MainThreadRunner.Run(() => ShowImportUI(videoFile, CancelImport));
    }

    void ShowImportUI(string videoFile, SourceVideo.CancelImport CancelImport)
    {
        SetCurrentVideo(videoFile);
        ProgressBar.Show(() => CancelImport());
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
