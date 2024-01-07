/**
 * A thread might want to wait until another thread completes some action.
 * The waiting thread would then call Wait(ID), 
 * and the other thread then calls End(ID) to release the waiting thread from the wait.
 * 
 * The waiting thread may prepare a wait by calling Prepare(ID).  
 * If the waiting thread does not call Wait(ID), then it *must* call Unprepare(ID).
 * 
 * The control thread can control Hold(ID) to stall the waiting thread for as long as necessary.
 * If the control thread calls Hold(ID), then it *must* call End(ID).
 */

using System;
using System.Threading;
using Morph.Lib;

namespace Morph.Endpoint
{
  public class NumberedWaits// : IDisposable
  {
    /*
    ~NumberedWaits()
    {
      Dispose();
    }

    #region IDisposable Members

    public void Dispose()
    {
      foreach (NumberedWait Wait in fWaits)
        End(Wait.ID);
    }

    #endregion
    */

    #region Internals

    private class NumberedWait : RegisterItemID
    {
      internal NumberedWait(int ID)
      {
        fID = ID;
      }

      #region RegisterItemID Members

      private int fID;
      public int ID
      {
        get { return fID; }
      }

      #endregion

      internal AutoResetEvent fGate = new AutoResetEvent(false);
      internal bool fHold = false;
      internal bool fHeld = false;
    }

    private RegisterItems<NumberedWait> fWaits = new RegisterItems<NumberedWait>();

    private bool Find(int ID, out NumberedWait Wait)
    {
      lock (fWaits)
        Wait = fWaits.Find(ID);
      return Wait != null;
    }

    private NumberedWait Obtain(int ID)
    {
      NumberedWait Wait;
      if (!Find(ID, out Wait))
      { //  Doesn't already exist, so create and register a new wait
        Wait = new NumberedWait(ID);
        lock (fWaits)
          fWaits.Add(Wait);
      }
      return Wait;
    }

    #endregion

    #region For waiting thread

    public void Prepare(int ID)
    {
      Obtain(ID);
    }

    public void Unprepare(int ID)
    {
      lock (fWaits)
        fWaits.Remove(ID);
    }

    public bool Wait(int ID, TimeSpan Timeout)
    {
      //  Obtain the wait
      NumberedWait Wait = Obtain(ID);
      try
      {
        //  Wait
        bool Result = Wait.fGate.WaitOne((int)Timeout.TotalMilliseconds, false);
        //  Might have been requested to hold
        lock (Wait)
          if (Wait.fHold)
            Wait.fGate.WaitOne();
        //  Done
        return Result || Wait.fHeld;
      }
      finally
      {
        //  Done, so deregister the wait
        Unprepare(ID);
      }
    }

    #endregion

    #region For control thread

    public bool Hold(int ID)
    {
      NumberedWait Wait;
      bool IsFound = Find(ID, out Wait);
      if (IsFound)
        lock (Wait)
        {
          Wait.fHold = true;
          Wait.fHeld = true;
        }
      return IsFound;
    }

    public void End(int ID)
    {
      NumberedWait Wait;
      if (Find(ID, out Wait))
      {
        Wait.fHold = false; //  Don't hold
        Wait.fGate.Set();   //  Release it
      }
    }

    #endregion
  }
}