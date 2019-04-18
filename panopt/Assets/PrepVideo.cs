using System;
using System.IO;
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
    uint LastFrame;

    Mode ParseMode;

    public FFMPEGOutputParser(StreamReader Output, SplitProgress ProgressCB)
    {
        this.ProgressCB = ProgressCB;
        this.Output = Output;

        ParseMode = Mode.ParseDuration;
    }

    public uint ParseOutput()
    {
        while (ParseLine(Output.ReadLine())) { /* nop */ }

        return LastFrame;
    }

    bool ParseLine(string line)
    {
        if (line == null)
        {
            return false;
        }

        switch (ParseMode)
        {
            case Mode.ParseDuration:
                ParseDuration(line);
                break;
            case Mode.ParseFrame:
                ParseFrame(line);
                break;
        }

        return true;
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

    public static void ExtractSubtitles(string ffmpegBinary, string VideoFile)
    {
        var destDir = GetDestinationDir(VideoFile);
        var posFile = Path.Combine(destDir, POSITIONS_FILE);

        var proc = Utils.Run(ffmpegBinary, "-y", "-i", VideoFile, posFile);

        proc.WaitForExit();

        var exitCode = proc.ExitCode;
        if (exitCode != 0)
        {
            var err = string.Format("{0} failed, exit code {1}", ffmpegBinary, exitCode);
            throw new Exception(err);
        }
    }

    public static void SplitFrames(string ffmpegBinary, string VideoFile, SplitProgress ProgressCB,
                                   out uint NumFrames, out string ImagesDir)
    {
        ImagesDir = GetDestinationDir(VideoFile);
        var frameTemplate = Path.Combine(ImagesDir, "%04d.jpg");

        Directory.CreateDirectory(ImagesDir);

        var proc = Utils.Run(ffmpegBinary, "-i", VideoFile, frameTemplate);
        proc.Start();

        var oparser = new FFMPEGOutputParser(proc.StandardError, ProgressCB);
        NumFrames = oparser.ParseOutput();

        proc.WaitForExit();
        var exitCode = proc.ExitCode;
        if (exitCode != 0)
        {
            var err = string.Format("{0} failed, exit code {1}", ffmpegBinary, exitCode);
            throw new Exception(err);
        }
    }
}
