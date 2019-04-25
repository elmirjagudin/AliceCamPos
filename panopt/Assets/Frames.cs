using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Frames : MonoBehaviour
{
    public delegate void VideoLoaded(uint FirstFrame, uint LastFrame);
    public static event VideoLoaded VideoLoadedEvent;

    public delegate void PlaybackModeChanged(PlaybackMode NewMode);
    public static event PlaybackModeChanged PlaybackModeChangedEvent;

    public delegate void FrameChanged(uint FrameNumer);
    public static event FrameChanged FrameChangedEvent;

    public Cams cams;
    public GameObject texRecorder;
    public GameObject PhotogramMesh;

    uint FirstFrame;
    uint CurrentFrame;
    uint LastFrame;

    public enum PlaybackMode
    {
        Idle,
        Step,
        Play,
        Record
    }

    PlaybackMode _playbackMode = PlaybackMode.Idle;
    PlaybackMode playbackMode
    {
        get { return _playbackMode; }
        set
        {
            _playbackMode = value;
            PlaybackModeChangedEvent?.Invoke(_playbackMode);
        }
    }

    public void OpenVideo(string videoFile)
    {
        cams.Init(videoFile, out FirstFrame, out LastFrame);
        cams.AddTestModels();

        VideoLoadedEvent?.Invoke(FirstFrame, LastFrame);

        GotoFrame(FirstFrame);

        playbackMode = PlaybackMode.Step;
    }

    public void Play()
    {
        playbackMode = PlaybackMode.Play;
    }

    public void Pause()
    {
        playbackMode = PlaybackMode.Step;
    }

    void Update()
    {
        switch (playbackMode)
        {
            case PlaybackMode.Idle:
                /* nop */
                break;
            case PlaybackMode.Step:
                StepTick();
                break;
            case PlaybackMode.Play:
                PlayTick();
                break;
            case PlaybackMode.Record:
                RecordVideoTick();
                break;
        }

        if (Input.GetKeyDown("r"))
        {
            StartRecording();
        }

        if (Input.GetKeyDown("t"))
        {
            PhotogramMesh.SetActive(!PhotogramMesh.activeSelf);
        }

        if (Input.GetKeyDown("m"))
        {
            cams.ToggleGNSSMarkers();
        }
    }

    void StepTick()
    {
        if (Input.GetKeyDown("space"))
        {
            playbackMode = PlaybackMode.Play;
            return;
        }

        if (Input.GetKeyDown("n"))
        {
            GotoNextFrame();
        }
    }

    void PlayTick()
    {
        if (Input.GetKeyDown("space"))
        {
            playbackMode = PlaybackMode.Step;
            return;
        }

        GotoNextFrame();
    }

    void StartRecording()
    {
        playbackMode = PlaybackMode.Record;
        GotoFrame(FirstFrame);
        texRecorder.SetActive(true);
    }

    void RecordVideoTick()
    {
        if (CurrentFrame + 1 > LastFrame)
        {
            /* at last frame, we are done recording */
            playbackMode = PlaybackMode.Step;
            texRecorder.SetActive(false);
            return;
        }

        GotoNextFrame();
    }

    public void GotoFrame(uint frame)
    {
        CurrentFrame = frame;
        cams.GotoFrame(CurrentFrame);
        FrameChangedEvent?.Invoke(CurrentFrame);
    }

    public void GotoNextFrame()
    {
        CurrentFrame += 1;

        /* wrap around */
        if (CurrentFrame > LastFrame)
        {
            CurrentFrame = FirstFrame;
        }

        GotoFrame(CurrentFrame);
    }

    public void GotoPreviousFrame()
    {
        CurrentFrame -= 1;

        /* wrap around */
        if (CurrentFrame <= 0)
        {
            CurrentFrame = LastFrame;
        }

        GotoFrame(CurrentFrame);
    }
}
