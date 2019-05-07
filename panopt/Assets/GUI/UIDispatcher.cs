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

    public GameObject ShowMainMenu;
    public GameObject ShowModelsMenu;
    public GameObject ModelsMenu;
    public Models Models;
    public LoginMenu LoginMenu;
    public Text VideoLabel;
    public ProgressBar ProgressBar;
    public Frames Frames;
    public MediaControllers MediaControllers;
    public ErrorMessage ErrorMessage;

    void Awake()
    {
        ModelAssets.StartedDownloadingEvent += HandleAssetsDownloadStarted;
        ModelAssets.AssetDownloadedEvent += HandleAssetsDownloaded;
        ModelAssets.FinishedDownloadingEvent += HandleFinishedDownloading;
        ModelAssets.AbortedDownloadingEvent += HandleAssetsDownloadAborted;

        Models.StartedLoadingPrefabsEvent += HandleStartedLoadingPrefabs;
        Models.PrefabLoadedEvent += HandlePrefabLoaded;
        Models.FinishedLoadingPrefabsEvent += HandleFinishedLoadingPrefabs;

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
        Models.Load();
    }

    void HandlePrefabLoaded(int done, int total)
    {
        MainThreadRunner.Run(() =>
            ProgressBar.SetProgress("loading models", done, total));
    }

    void HandleStartedLoadingPrefabs()
    {
        MainThreadRunner.Run(() =>
            ProgressBar.Show("loading models", null));
    }

    void HandleFinishedLoadingPrefabs()
    {
        MainThreadRunner.Run(delegate()
        {
            ProgressBar.Hide();
            Frames.OpenVideo();
            ShowModelsMenu.SetActive(true);
            ModelsMenu.SetActive(true);
        });
    }

    void HandleImportStarted(string videoFile, SourceVideo.CancelImport CancelImport)
    {
        MainThreadRunner.Run(() => ShowImportUI(videoFile, CancelImport));
    }

    void ShowImportUI(string videoFile, SourceVideo.CancelImport CancelImport)
    {
        SetCurrentVideo(videoFile);
        ProgressBar.Show("importing", () => CancelImport());
    }

    string GetDownloadingProgress(int Downloaded, int AssetsCount)
    {
        return $"downloading models {Downloaded}/{AssetsCount}";
    }

    void HandleAssetsDownloadStarted(int AssetsCount)
    {
        if (AssetsCount == 0)
        {
            /*
             * don't show progress bar if no assets
             * will be downloaded
             */
            return;
        }

        MainThreadRunner.Run(() =>
            ProgressBar.Show(GetDownloadingProgress(1, AssetsCount),
                             ModelAssets.AbortDownloading));
    }

    void HandleAssetsDownloaded(int Downloaded, int AssetsCount)
    {
        var progress = (float)Downloaded/(float)AssetsCount;

        MainThreadRunner.Run(() =>
            ProgressBar.SetProgress(GetDownloadingProgress(Downloaded, AssetsCount), progress));
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

    void HandleFinishedDownloading()
    {
        MainThreadRunner.Run(delegate ()
        {
            ProgressBar.Hide();
            ShowMainMenu.SetActive(true);
        });
    }

    void HandleAssetsDownloadAborted()
    {
        MainThreadRunner.Run(delegate ()
        {
            ProgressBar.Hide();
            LoginMenu.gameObject.SetActive(true);
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
        Models.Init(models);
    }
}
