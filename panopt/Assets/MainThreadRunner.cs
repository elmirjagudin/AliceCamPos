using UnityEngine;
using Hagring;
using System.Threading;

///
/// Class and GameObject to run code on the main thread, usefull when we
/// need to use some Unity API, that is only available from the main thread.
///
/// To schedule a task to be call MainThreadRunner.Run().
///
public class MainThreadRunner : MonoBehaviour
{
    public delegate void Task();
    static SyncQueue<Task> Tasks = new SyncQueue<Task>();

    /* used to check if we are on the main thread */
    static Thread mainThread = Thread.CurrentThread;

    void Update()
    {
        /*
         * Note, we only run one task each frame,
         * because it's easier that way and
         * it's good enought right now.
         */
        Task task;
        if (!Tasks.CheckAndDequeue(out task))
        {
            /* no task to run on the main thread, we are done */
            return;
        }

        /* do the needful */
        task();
    }

    ///
    /// Run 'task' on the main rendering thread.
    ///
    /// If we are alreay on the main thread, the task will be run right away.
    ///
    /// Otherwise it's added to a tasks queue, and will be run on the main threads
    /// N frames later.
    ///
    public static void Run(Task task)
    {
        if (Thread.CurrentThread == mainThread)
        {
            /* already on main thread, run right away !*/
            task();
            return;
        }

        /* add to work thread, so we can run it later from main thread */
        Tasks.Enqueue(task);
    }
}
