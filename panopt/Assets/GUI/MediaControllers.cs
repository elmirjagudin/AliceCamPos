using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaControllers : MonoBehaviour
{
    public FrameSlider FrameSlider;

    public void Init(uint FirstFrame, uint LastFrame)
    {
        FrameSlider.Init(FirstFrame, LastFrame);

        Frames.PlaybackModeChangedEvent += HandlePlaybackModeChanged;

        gameObject.SetActive(true);
    }

    void HandlePlaybackModeChanged(Frames.PlaybackMode mode)
    {
        /*
         * Hide media controllers while video is recorded,
         * and show em again once recording is done.
         */
        bool newActive = true;

        switch (mode)
        {
            case Frames.PlaybackMode.Record:
                newActive = false;
                break;
            case Frames.PlaybackMode.Step:
                newActive = true;
                break;
        }

        if (newActive != gameObject.activeSelf)
        {
            gameObject.SetActive(newActive);
        }
    }
}
