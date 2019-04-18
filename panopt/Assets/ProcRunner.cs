using System;
using System.Threading;
using System.Diagnostics;

public class ProcessAborted : Exception {};

public class ProcRunner
{
    Process proc;
    AutoResetEvent ExitedEvent = new AutoResetEvent(false);

    public delegate void StderrLineHandler(string line);
    public event StderrLineHandler StderrLineEvent;

    public ProcRunner(string bin, params string[] args)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = bin;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        /* add process arguments */
        var argsStr = "";
        foreach (var arg in args)
        {
            argsStr += $" {arg}";
        }
        psi.Arguments = argsStr;

        proc = new Process();
        proc.StartInfo = psi;
        proc.ErrorDataReceived += ErrorDataReceived;
        proc.Exited += Exited;
        proc.EnableRaisingEvents = true;
    }

    ///
    /// starts the process and returns
    ///
    public void StartAsync()
    {
        proc.Start();
        proc.BeginErrorReadLine();
    }

    ///
    /// check if the process is running, blocking up to timeout miliseconds
    /// returns true if process is still running after the timeout elapses,
    /// false is the process exited
    ///
    /// throws ProcessAborted exception if process is aborted via AbortEvent signal
    /// throws exception if process exits with error exit code
    ///
    /// if timeout is -1, will block until the process exits or is aborted
    ///
    public bool IsRunning(AutoResetEvent AbortEvent, int timeout)
    {
        var res = WaitHandle.WaitAny(new WaitHandle[]{AbortEvent, ExitedEvent}, timeout);
        if (res == 0)
        {
            proc.Kill();
            ExitedEvent.WaitOne();

            throw new ProcessAborted();
        }

        if (res == 1)
        {
            if (proc.ExitCode != 0)
            {
                var psi = proc.StartInfo;
                throw new Exception(
                    $"{psi.FileName} {psi.Arguments} failed, exit code {proc.ExitCode}");
            }

            return false;
        }

        return true;
    }

    ///
    /// starts the process and blocks until the process exits
    /// or it is aborted by signaling the specified AbortEvent
    ///
    public void Start(AutoResetEvent AbortEvent)
    {
        StartAsync();
        IsRunning(AbortEvent, -1);
    }

    void Exited(object sender, EventArgs e)
    {
        ExitedEvent.Set();
    }

    void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        StderrLineEvent?.Invoke(e.Data);
    }
}