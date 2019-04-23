using System;
using System.Linq;
using System.Collections.Generic;

public struct TimeBase
{
    public uint Numerator;
    public uint Denominator;
}

public class Chunks
{
    const uint MIN_OVERLAP = 20;                         /* in frames */
    const int CHUNK_LENGTH = 40;                         /* in frames */
    const uint CHUNK_DURATION = CHUNK_LENGTH / 2 * 1000; /* in miliseconds */

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

    static void GetChunkParameters(int Length, out int ChunkNums, out int Overlap)
    {
        ChunkNums = (Length / CHUNK_LENGTH);

        do
        {
            ChunkNums += 1;
            Overlap = -(Length - ChunkNums * CHUNK_LENGTH) / (ChunkNums - 1);
        } while (Overlap < MIN_OVERLAP);
    }

    public static IEnumerable<IEnumerable<uint>> GetChunks(TimeBase TimeBase, uint LastFrame)
    {
        var lastPTS = TimeBase.Numerator * (LastFrame - 1);
        var lastTimeStamp = (uint)((double)(lastPTS)/(double)TimeBase.Denominator * 1000.0);


        var frames = KeyFrames(0, lastTimeStamp, TimeBase).ToArray();

        int ChunkNums, Overlap;
        GetChunkParameters(frames.Length, out ChunkNums, out Overlap);

        for (int chunk = 0; chunk < ChunkNums; chunk += 1)
        {
            //TODO: take care of the last chunk, as right now it can potentionally be shorter
            // then CHUNK_LENGTH, it needs to be moved 'left' a bit more then Overlap
            var start = chunk * (CHUNK_LENGTH - Overlap);
            yield return frames.Skip(start).Take(CHUNK_LENGTH);
        }
    }
}