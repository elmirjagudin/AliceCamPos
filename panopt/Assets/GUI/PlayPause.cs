using UnityEngine;
using UnityEngine.UI;

public class PlayPause : MonoBehaviour
{
    public Frames Frames;
    public Sprite Play;
    public Sprite Pause;

    void Awake()
    {
        Frames.PlaybackModeChangedEvent += HandlePlaybackModeChange;
    }

    Image CurrentImage()
    {
        return GetComponent<Image>();
    }

    bool IsPaused()
    {
        return CurrentImage().sprite == Play;
    }

    public void Toggle()
    {
        if (IsPaused())
        {
            Frames.Play();
        }
        else
        {
            Frames.Pause();
        }
    }

    void SetSprite(Sprite sprite)
    {
        CurrentImage().sprite = sprite;
    }

    void HandlePlaybackModeChange(Frames.PlaybackMode mode)
    {
        switch (mode)
        {
            case Frames.PlaybackMode.Play:
                SetSprite(Pause);
                break;
            case Frames.PlaybackMode.Step:
                SetSprite(Play);
                break;
            // TODO: disable on other modes?
        }
    }
}
