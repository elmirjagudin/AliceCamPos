using System;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using Hagring;

public delegate void SplitProgress(float done);

class FFMPEGOutputParser
{
    static Regex DurationRegex = new Regex(
        @"\ *Duration: (?<hour>\d\d):(?<min>\d\d):(?<sec>\d\d).(?<csec>\d\d)",
        RegexOptions.Compiled);

    static Regex FrameRegex = new Regex(
        @"^frame=\ *(?<frame>\d*).*time=(?<hour>\d\d):(?<min>\d\d):(?<sec>\d\d).(?<csec>\d\d)",
        RegexOptions.Compiled);

    enum Mode { ParseDuration, ParseFrame };

    SplitProgress ProgressCB;
    StreamReader Output;
    uint Duration;
    public uint LastFrame { get; private set; }

    Mode ParseMode;

    public FFMPEGOutputParser(SplitProgress ProgressCB)
    {
        this.ProgressCB = ProgressCB;
        ParseMode = Mode.ParseDuration;
    }

    public void ParseLine(string line)
    {
        switch (ParseMode)
        {
            case Mode.ParseDuration:
                ParseDuration(line);
                break;
            case Mode.ParseFrame:
                ParseFrame(line);
                break;
        }
    }

    static uint Get(GroupCollection groups, string name)
    {
        var txt = groups[name].Value;
        return uint.Parse(txt, CultureInfo.InvariantCulture);
    }

    uint TimeInCentiseconds(GroupCollection groups)
    {
        var hour = Get(groups, "hour");
        var min = Get(groups, "min");
        var sec = Get(groups, "sec");
        var csec = Get(groups, "csec");
        return (((((hour * 60) + min) * 60) + sec) * 100) + csec;
    }

    void ParseDuration(string line)
    {
        var groups = DurationRegex.Match(line).Groups;
        if (groups.Count != 5)
        {
            /* no match */
            return;
        }

        Duration = TimeInCentiseconds(groups);
        ParseMode = Mode.ParseFrame;
    }

    void ParseFrame(string line)
    {
        var groups = FrameRegex.Match(line).Groups;
        if (groups.Count != 6)
        {
            /* not a current frame line, ignore */
            return;
        }

        LastFrame = Get(groups, "frame");
        ProgressCB((float)TimeInCentiseconds(groups) / (float)Duration);
    }
}

public class PrepVideo
{
    const string DATA_DIR = "panopt";
    const string POSITIONS_FILE = "positions.srt";

    static string GetDestinationDir(string VideoFile)
    {
        var dirName = Path.GetDirectoryName(VideoFile);
        var fileName = Path.GetFileNameWithoutExtension(VideoFile);

        return Path.Combine(dirName, DATA_DIR, fileName);
    }

    public static string GetPositionsFilePath(string VideoFile)
    {
        return Path.Combine(GetDestinationDir(VideoFile), POSITIONS_FILE);
    }

    public static void ExtractSubtitles(string ffmpegBinary, string VideoFile,
                                        AutoResetEvent AbortEvent)
    {
        var posFile = GetPositionsFilePath(VideoFile);

        var runner = new ProcRunner(ffmpegBinary, "-y", "-i", VideoFile, posFile);
        runner.Start(AbortEvent);
    }

    public static void SplitFrames(
        string ffmpegBinary, string VideoFile, SplitProgress ProgressCB,
        AutoResetEvent AbortEvent,
        out uint NumFrames, out string ImagesDir)
    {
        ImagesDir = GetDestinationDir(VideoFile);
        var frameTemplate = Path.Combine(ImagesDir, "%04d.jpg");

        Directory.CreateDirectory(ImagesDir);

        var runner = new ProcRunner(ffmpegBinary, "-i", VideoFile, frameTemplate);
        var outputParser = new FFMPEGOutputParser(ProgressCB);
        runner.StderrLineEvent += outputParser.ParseLine;
        runner.Start(AbortEvent);

        NumFrames = outputParser.LastFrame;
    }
}
