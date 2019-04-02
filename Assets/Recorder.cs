using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class Recorder
{
    [DllImport("video")]
    static extern int recorder_init(
        ref IntPtr recorder, string filename,
        int width, int height,
        int timebase_numerator, int timebase_denominator);

    [DllImport("video")]
    static extern void recorder_close(IntPtr recorder);

    [DllImport("video")]
    static unsafe extern int recorder_encode_frame(IntPtr recorder, void *pixels, long pts);

    IntPtr recorder;

    public Recorder(
        string filename,
        int width, int height,
        int timebase_numerator, int timebase_denominator)
    {
        var ret = recorder_init(ref recorder,
                                filename,
                                width, height,
                                timebase_numerator, timebase_denominator);
        if (ret != 0)
        {
            throw new Exception("recorder_init() error");
        }
    }

    public void Encode(NativeArray<byte> pixels, long pts)
    {
        unsafe
        {
            var ret = recorder_encode_frame(
                recorder,
                NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixels),
                pts);
            if (ret != 0)
            {
                throw new Exception("recorder_encode_frame() error");
            }
        }
    }

    public void Close()
    {
        recorder_close(recorder);
    }

}
