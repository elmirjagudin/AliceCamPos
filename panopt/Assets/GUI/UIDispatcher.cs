using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIDispatcher : MonoBehaviour
{
    public Text VideoLabel;
    public ProgressBar ProgressBar;
    public Frames Frames;
    public MediaControllers MediaControllers;

    void Awake()
    {
        SourceVideo.ImportStartedEvent += HandleImportStarted;
        SourceVideo.ImportProgressEvent += UpdateProgressBar;
        SourceVideo.ImportFinishedEvent += HideProgressBar;
        SourceVideo.ImportCanceledEvent += HandleImportCanceled;

        SourceVideo.VideoOpenedEvent += HandleVideoOpened;

        Frames.VideoLoadedEvent += HandleVideoLoaded;
    }

    void SetCurrentVideo(string videoFile)
    {
        VideoLabel.text = Path.GetFileName(videoFile);
    }

    void HandleVideoOpened(string videoFile)
    {
        SetCurrentVideo(videoFile);
        Frames.OpenVideo(videoFile);
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

    void HandleImportCanceled()
    {
        MainThreadRunner.Run(delegate ()
        {
            ProgressBar.Hide();
            SetCurrentVideo("");
        });
    }

    void HandleVideoLoaded(uint FirstFrame, uint LastFrame)
    {
        MediaControllers.Init(FirstFrame, LastFrame);
    }
}
