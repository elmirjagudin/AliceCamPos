using UnityEngine;
using UnityEngine.UI;

public class MainMenu : Menu
{
    public Button RecordButton;

    void Awake()
    {
        SourceVideo.VideoOpenedEvent += HandleVideoOpened;
        SourceVideo.ImportStartedEvent += (_, __) => Hide();
        Frames.PlaybackModeChangedEvent += HandlePlaybackModeChanged;
    }

    void HandleVideoOpened(string _)
    {
        MainThreadRunner.Run(delegate
        {
            RecordButton.interactable = true;
            gameObject.SetActive(false);
        });
    }

    void Hide()
    {
        MainThreadRunner.Run(() => gameObject.SetActive(false));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOnScreen();
        }
    }

    void HandlePlaybackModeChanged(Frames.PlaybackMode mode)
    {
        if (mode == Frames.PlaybackMode.Record)
        {
            Hide();
        }
    }
}
