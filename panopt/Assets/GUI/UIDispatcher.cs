using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDispatcher : MonoBehaviour
{
    static Dictionary<LoginMenu.Error, (string  title, string message)> LoginErrors =
        new Dictionary<LoginMenu.Error, (string  title, string message)>
    {
        { LoginMenu.Error.Authentication, ("authentication failed", "invalid credentials") },
        { LoginMenu.Error.Connection, ("connection error", "check your internet connection") }
    };

    public GameObject ShowMenu;
    public Text VideoLabel;
    public ProgressBar ProgressBar;
    public Frames Frames;
    public MediaControllers MediaControllers;
    public ErrorMessage ErrorMessage;


    void Awake()
    {
        SourceVideo.ImportStartedEvent += HandleImportStarted;
        SourceVideo.ImportProgressEvent += UpdateProgressBar;
        SourceVideo.ImportFinishedEvent += HideProgressBar;
        SourceVideo.ImportCanceledEvent += HandleImportCanceled;

        SourceVideo.VideoOpenedEvent += HandleVideoOpened;

        Frames.VideoLoadedEvent += HandleVideoLoaded;

        LoginMenu.LoginErrorEvent += HandleLoginError;
        LoginMenu.LoggedInEvent += HandleLoggedInEvent;
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

    void HandleLoginError(LoginMenu.Error Error)
    {
        var ErrMsg = LoginErrors[Error];
        MainThreadRunner.Run(() => ErrorMessage.Show(ErrMsg.title, ErrMsg.message));
    }

    void HandleLoggedInEvent(CloudAPI.Model[] models)
    {
        ShowMenu.SetActive(true);
    }
}
