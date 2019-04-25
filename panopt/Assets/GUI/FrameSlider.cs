using UnityEngine;
using UnityEngine.UI;


public class FrameSlider : MonoBehaviour
{
    public Slider Slider;
    public Frames Frames;

    /*
     * wraps UnityEngine.UI.Slider.value property so that we
     * can store current frame as uint (rather then float)
     */
    uint _CurrentFrame;
    public uint CurrentFrame
    {
        get { return _CurrentFrame; }
        set
        {
            _CurrentFrame = value;
            Slider.value = value;
        }
    }

    public void Init(uint FirstFrame, uint LastFrame)
    {
        Slider.minValue = FirstFrame;
        Slider.maxValue = LastFrame;

        Frames.FrameChangedEvent += HandleFrameChanged;
    }

    public void ValueChanged()
    {
        var FrameNumber = (uint)Slider.value;
        if (FrameNumber != CurrentFrame)
        {
            Frames.GotoFrame((uint)Slider.value);
        }
    }

    void HandleFrameChanged(uint FrameNumber)
    {
        if (FrameNumber != CurrentFrame)
        {
            CurrentFrame = FrameNumber;
        }
    }
}
