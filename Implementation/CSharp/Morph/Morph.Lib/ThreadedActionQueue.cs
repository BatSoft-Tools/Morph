using System;
using System.Collections.Generic;
using System.Threading;

namespace Morph.Lib
{
  public interface Action
  {
    void Execute();
  }

  public class ThreadedActionQueue
  {
    public ThreadedActionQueue()
    {
    }

    public ThreadedActionQueue(int ThreadCount)
    {
      SetThreadCount(ThreadCount);
    }

    #region Threading

    //  Used for managing the number of threads
    private int fThreadCount = 0;
    private int fThreadCountTarget = 0;
    private object fThreadCountSemaphore = new Object();

    //  This gate lets the threads rest when there's no work to do
    private AutoResetEvent fGateRest = new AutoResetEvent(true);

    //  This gate may be useful when tidying up
    private ManualResetEvent fGateNoThreads = new ManualResetEvent(true);

    private void CreateThread()
    {
      fGateNoThreads.Reset();
      (new Thread(new ThreadStart(ThreadCode))).Start();
      fThreadCount++;
    }

    private void ThreadCode()
    {
      while (true)
      { //  Wait
        fGateRest.WaitOne();
        //  Is this thread superfluous?
        if (fThreadCountTarget < fThreadCount)
          lock (fThreadCountSemaphore)
            if (fThreadCountTarget < fThreadCount)
            {
              fThreadCount--;
              //  Release the next thread 
              fGateRest.Set();
              //  Might be idle now
              if (fThreadCount == 0)
                fGateNoThreads.Set();
              return;
            }
        //  Get the next action
        Action Action = null;
        lock (fActions)
          if (fActions.Count > 0)
            Action = fActions.Dequeue();
        //  If the queue is empty, then make the threads wait
        if (Action != null)
        {
          fGateRest.Set();
          //  Run the Action
          try
          {
            Action.Execute();
          }
          catch (Exception x)
          {
            HandleException(x);
          }
        }
      }
    }

    public void SetThreadCount(int ThreadCount)
    {
      if (ThreadCount < 0)
        throw new Exception("ThreadCount cannot be less than 0");
      //  Set the target
      fThreadCountTarget = ThreadCount;
      //  Ensure at least one thread exists, to create other threads.
      lock (fThreadCountSemaphore)
        while (fThreadCount < fThreadCountTarget)
          CreateThread();
      //  Release a thread
      fGateRest.Set();
    }

    public void WaitUntilNoThreads()
    {
      WaitUntilNoThreads(Timeout.Infinite);
    }

    public bool WaitUntilNoThreads(int TimeoutMilliseconds)
    {
      SetThreadCount(0);
      return fGateNoThreads.WaitOne(TimeoutMilliseconds, false);
    }

    #endregion

    #region Exception handling

    public event ExceptionEventHandler Error;

    private void HandleException(Exception x)
    {
      try
      {
        if (Error != null)
          Error(this, new ExceptionArgs(x));
      }
      catch
      {
        //  Ignore exceptions thrown from an exception handler
      }
    }

    #endregion

    #region Action queue

    private Queue<Action> fActions = new Queue<Action>();

    public void Push(Action Action)
    {
      lock (fActions)
        fActions.Enqueue(Action);
      fGateRest.Set();
    }

    //  Used for getting a rough estimate when examining efficiency
    public int Count
    {
      get
      {
        lock (fActions)
          return fActions.Count;
      }
    }

    #endregion
  }
}