using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaControllers : MonoBehaviour
{
    public FrameSlider FrameSlider;

    public void Init(uint FirstFrame, uint LastFrame)
    {
        FrameSlider.Init(FirstFrame, LastFrame);
        gameObject.SetActive(true);
    }
}
