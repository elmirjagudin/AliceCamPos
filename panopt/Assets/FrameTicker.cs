using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameTicker : MonoBehaviour
{
    public Cams cams;
    public GameObject texRecorder;
    public GameObject PhotogramMesh;

    uint FirstFrame;
    uint CurrentFrame;
    uint LastFrame;

    enum TickingMode
    {
        Manual,
        Roll,
        RecordVideo
    }

    TickingMode tickingMode = TickingMode.Manual;

    void Start()
    {
        cams.Init(out FirstFrame, out LastFrame);
        GotoFrame(FirstFrame);
    }

    void Update()
    {
        switch (tickingMode)
        {
            case TickingMode.Manual:
                ManualTick();
                break;
            case TickingMode.Roll:
                RollTick();
                break;
            case TickingMode.RecordVideo:
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
    }

    void ManualTick()
    {
        if (Input.GetKeyDown("space"))
        {
            tickingMode = TickingMode.Roll;
            return;
        }

        if (Input.GetKeyDown("n"))
        {
            GotoNextFrame();
        }
    }

    void RollTick()
    {
        if (Input.GetKeyDown("space"))
        {
            tickingMode = TickingMode.Manual;
            return;
        }

        GotoNextFrame();
    }

    void StartRecording()
    {
        tickingMode = TickingMode.RecordVideo;
        GotoFrame(FirstFrame);
        texRecorder.SetActive(true);
    }

    void RecordVideoTick()
    {
        if (CurrentFrame + 1 > LastFrame)
        {
            /* at last frame, we are done recording */
            tickingMode = TickingMode.Manual;
            texRecorder.SetActive(false);
            return;
        }

        GotoNextFrame();
    }

    void GotoFrame(uint frame)
    {
        CurrentFrame = frame;
        cams.GotoFrame(CurrentFrame);
    }

    void GotoNextFrame()
    {
        CurrentFrame += 1;

        /* wrap around */
        if (CurrentFrame > LastFrame)
        {
            CurrentFrame = FirstFrame;
        }

        cams.GotoFrame(CurrentFrame);
    }
}