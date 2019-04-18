using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace Hagring
{

///
/// Provides a thread safe Queue, where access to all
/// public methods and properties is synchonized with
/// a lock.
///
public class SyncQueue<T>
{
    /*
     * Simply wrap a standard queue object,
     * use lock on 'this' object to synchronize
     * access to all implemented methods and properties
     */
    Queue<T> queue = new Queue<T>();

    public void Enqueue(T item)
    {
        lock (this)
        {
            queue.Enqueue(item);
        }
    }

    public int Count
    {
        get
        {
            lock (this)
            {
                return queue.Count;
            }
        }
    }

    ///
    /// Method that allows to check if there are any
    /// items on the queue and dequeue it taking lock
    /// only once.
    ///
    /// return true if an item was dequeued, false if the
    /// the queue is empty
    ///
    public bool CheckAndDequeue(out T Item)
    {
        lock (this)
        {
            if (queue.Count < 1)
            {
                Item = default(T);
                return false;
            }
            Item = queue.Dequeue();
            return true;
        }
    }

    public T Dequeue()
    {
        lock (this)
        {
            return queue.Dequeue();
        }
    }

    public T[] ToArray()
    {
        lock (this)
        {
            return queue.ToArray();
        }
    }

    public void Clear()
    {
        lock (this)
        {
            queue.Clear();
        }
    }
}

/// <summary>
/// This is a wrapper object, that allows us to start
/// a new thread and catch all unhandled exceptions in that thread.
/// </summary>
class ThreadRunner
{
    ThreadStart EntryPoint;
    Thread thread;

    public ThreadRunner(ThreadStart EntryPoint, string Name)
    {
        this.EntryPoint = EntryPoint;
        thread = new Thread(Run);
        thread.Name = Name;
    }

    public Thread Start()
    {
        thread.Start();

        return thread;
    }

    public void Run()
    {
        Log.Msg("Starting {0} thread ", thread.Name);
        try
        {
            EntryPoint();
        }
        catch (Exception e)
        {
            /* log the unhandled exception */
            UnityEngine.Debug.LogException(e);
        }
    }
}

///
/// Allow to create a 'lazy loaded' value.
///
/// The the 'loader' code will be run first
/// time the value is accessed with Get() method,
/// subsequent calls to Get() will returned
/// cached value.
///
public class LazyLoader<T> where T : class
{
    public delegate T Load();

    Load loader;
    T value;

    public LazyLoader(Load loader)
    {
        this.loader = loader;
        value = null;
    }

    ///
    /// Drop the cached value, next call to
    /// Get() will use loader() to (re)load
    /// the value.
    ///
    public void Reset()
    {
        value = null;
    }

    public T Get()
    {
        if (value == null)
        {
            value = loader();
        }
        return value;
    }
}

public class Utils
{
    //
    // basically a 'mkdir -p <dir>'
    //
    public static void Mkdir(string dir)
    {
        if (Directory.Exists(dir))
        {
            return;
        }
        Directory.CreateDirectory(dir);
    }

    public static void DeleteEmptySubDirs(DirectoryInfo root)
    {
        foreach (var dir in root.GetDirectories())
        {
            DeleteEmptySubDirs(dir);

            var isEmpty = dir.GetFiles().Length == 0 &&
                          dir.GetDirectories().Length == 0;
            if (isEmpty)
            {
                dir.Delete();
            }
        }
    }

    /*
     * do 'rm -rf <DirPath>'
     */
    public static void RemoveDir(string DirPath)
    {
        if (!Directory.Exists(DirPath))
        {
            /* does not exist, don't try to remove or we'll get an exception */
            return;
        }
        Directory.Delete(DirPath, true);
    }

    ///
    /// Creates new file at specified path, returning stream
    /// object to use for writing data to the file.
    ///
    /// Will also create all sub-directories, if needed,
    /// for the specified file path.
    ///
    public static FileStream CreateFile(string FilePath)
    {
        Mkdir(Path.GetDirectoryName(FilePath));
        return File.Create(FilePath);
    }

    ///
    /// Works exactly as CreateFile(), but wrapps the file
    /// stream into an StreamWriter object.
    ///
    public static StreamWriter CreateFileWriter(string FilePath)
    {
        return new StreamWriter(CreateFile(FilePath));
    }

    ///
    /// replace secret key/password in the string with '****'.
    ///
    public static string CensorSecret(string str, string secret)
    {
        return str.Replace(secret, "****");
    }

    /// <summary>
    /// A wrapper method for starting new threads.
    ///
    /// Will run the specified function in a new thread.
    /// Any unhandled exception raised inside the new thread
    /// will be caught and logged.
    /// </summary>
    /// <param name="EntryPoint">Code to run in new thread</param>
    /// <param name="ThreadName">Name of new thread, for debugging purposes.</param>
    public static Thread StartThread(ThreadStart EntryPoint, string ThreadName)
    {
        return new ThreadRunner(EntryPoint, ThreadName).Start();
    }

    ///
    /// Calculate angle difference between two angles.
    ///
    public static double AngleDiff(double angle1, double angle2)
    {
        double diff = 0;

        if (angle1 > 180)
        {
            // this sensor is not 0 - 180 and 0- (-)180. It has instead 0 - 360. Convert the y value
            // to fit the 0- (+-)180 range instead
            angle1 = angle1 - 360; // if y = 270 >>>  270 - 360 = -90  Yes that is what we want.
        }

        if (angle2 > 180)
        {
            // this sensor is not 0 - 180 and 0- (-)180. It has instead 0 - 360. Convert the y value
            // to fit the 0- (+-)180 range instead
            angle2 = angle2 - 360; // if y = 270 >>>  270 - 360 = -90  Yes that is what we want.
        }

        //GPS_IMU_calibration
        // Difference between IMU and GPS direction is calculated by:
        // diff = angle1 - angle2 (GPS_direction - IMU_Y_direction)
        // Rotation in Unity is negative counterclockwise and positive clockwise
        if ((angle1 >= 0 && angle2 >= 0) ||
            (angle1 < 0 && angle2 < 0))
        {
            // Same sign
            diff = angle1 - angle2;
        }
        else
        {
            // different sign
            diff = angle1 - angle2;
            if (diff < -180)
            {
                diff = diff + 360; //eg. -340 + 360 = 20 rotation clockwise
            }
            else if (diff > 180)
            { // eg. 5  - -177 = 182
                diff = diff - 360; // eg. 182 - 360 = -178  rotation counterclockwise
            }
        }

        return diff;
    }

    ///
    /// Convert angle in degrees to radians
    ///
    static public double toRad(double angle)
    {
        return (Math.PI / 180) * angle;
    }

    ///
    /// Convert angle in radians to degrees
    ///
    static public double toDegrees(double angle)
    {
        return angle * (180 / Math.PI);
    }
}
}
