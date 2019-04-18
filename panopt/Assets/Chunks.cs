using System;
using System.Collections.Generic;

public struct TimeBase
{
    public uint Numerator;
    public uint Denominator;
}

public class Chunks
{
    const uint CHUNK_DURATION = 20 * 1000; /* in miliseconds */

    static uint FrameCloseTo(uint TimeStamp, TimeBase TimeBase)
    {
        var tbNum = (double) TimeBase.Numerator;
        var tbDen = (double) TimeBase.Denominator;
        var ts = ((double) TimeStamp) / 1000.0;

        var frame = (uint)Math.Round(ts * (tbDen / tbNum)) + 1;

        return frame;
    }

    static IEnumerable<uint> KeyFrames(uint StartTimeStamp, uint EndTimeStamp, TimeBase TimeBase)
    {
        for (uint time = StartTimeStamp; time < EndTimeStamp; time += 500)
        {
            yield return FrameCloseTo(time, TimeBase);
        }
    }

    public static IEnumerable<IEnumerable<uint>> GetChunks(TimeBase TimeBase, uint LastFrame)
    {
        var lastPTS = TimeBase.Numerator * (LastFrame - 1);
        var lastTimeStamp = (uint)((double)(lastPTS)/(double)TimeBase.Denominator * 1000.0);

        for (uint ts = 0; ts < lastTimeStamp - CHUNK_DURATION; ts += CHUNK_DURATION)
        {
            yield return KeyFrames(ts, ts + CHUNK_DURATION, TimeBase);
        }

        yield return KeyFrames(lastTimeStamp - CHUNK_DURATION, lastTimeStamp, TimeBase);

    }
}