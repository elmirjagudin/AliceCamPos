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
    public RenderTexRecorder texRecorder;
    public GameObject PhotogramMesh;

    uint FirstFrame;
    uint CurrentFrame;
    uint LastFrame;

    public enum PlaybackMode
    {
        Step,
        Play,
        Record
    }

    PlaybackMode _playbackMode = PlaybackMode.Step;
    PlaybackMode playbackMode
    {
        get { return _playbackMode; }
        set
        {
            _playbackMode = value;
            PlaybackModeChangedEvent?.Invoke(_playbackMode);
        }
    }

    public void OpenVideo()
    {
        cams.Init(SourceVideo.VideoFile, out FirstFrame, out LastFrame);
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
            case PlaybackMode.Step:
                /* nop */
                break;
            case PlaybackMode.Play:
                GotoNextFrame();
                break;
            case PlaybackMode.Record:
                RecordVideoTick();
                break;
        }

        if (Input.GetKeyDown("t") && PhotogramMesh != null)
        {
            PhotogramMesh.SetActive(!PhotogramMesh.activeSelf);
        }

        if (Input.GetKeyDown("m"))
        {
            cams.ToggleGNSSMarkers();
        }
    }

    public void StartRecording(string VideoFile)
    {
        playbackMode = PlaybackMode.Record;
        GotoFrame(FirstFrame);
        texRecorder.StartRecording(VideoFile);
    }

    void RecordVideoTick()
    {
        if (CurrentFrame + 1 > LastFrame)
        {
            /* at last frame, we are done recording */
            playbackMode = PlaybackMode.Step;
            texRecorder.StopRecording();
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
        if (CurrentFrame <= FirstFrame)
        {
            CurrentFrame = LastFrame;
        }

        GotoFrame(CurrentFrame);
    }
}
