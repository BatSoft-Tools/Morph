using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Morph.Lib;

namespace Morph.Sequencing
{
  public class Sequence : IDisposable
  {
    internal Sequence(int SequenceID, bool IsLossless)
    {
      fSequenceID = SequenceID;
      this.IsLossless = IsLossless;
      lock (Sequences.All)
        Sequences.All.Add(SequenceID, this);
      Start();
    }

    #region IDisposable Members

    public void Dispose()
    {
      lock (Sequences.All)
        Sequences.All.Remove(fSequenceID);
    }

    #endregion

    private int fSequenceID;
    public int SequenceID
    {
      get { return fSequenceID; }
    }

    private int fSenderID = 0;
    public int SenderID
    {
      get { return fSenderID; }
      set { fSenderID = value; }
    }

    private IImpl fImpl = null;
    public bool IsLossless
    {
      get { return fImpl is ImplLossless; }
      set
      {
        if (fImpl != null)
          if (IsLossless == value)
            return;
          else if (!value)
            throw new EMorphUsage("Sequence cannot change from lossless to lossy.");
        lock (fQueue)
          if (fCurrentIndex == 0)
            if (value)
              fImpl = new ImplLossless(this);
            else
              fImpl = new ImplLossy(this);
          else
            throw new EMorphUsage("Sequence has already started.");
      }
    }

    private LinkStack fPathToProxy;
    public LinkStack PathToProxy
    {
      get { return fPathToProxy; }
      set { fPathToProxy = value; }
    }

    private TimeSpan fTimeout = Sequences.DefaultTimeout;
    public TimeSpan Timeout
    {
      get { return fTimeout; }
      set
      {
        if (value != null)
          fTimeout = value;
      }
    }

    internal void Index(int Index, LinkMessage Message)
    {
      fImpl.Add(Index, Message);
    }

    internal void Stop(int Index)
    {
      throw new System.Exception("The method or operation is not implemented.");
    }

    private void SendReply(LinkSequence LinkReply)
    {
      try
      {
        LinkMessage Message = new LinkMessage(fPathToProxy.Clone(), null);
        Message.PathTo.Append(LinkReply);
        Message.ActionNext();
      }
      catch (Exception x)
      {
        MorphErrors.NotifyAbout(x);
      }
    }

    #region Thread control

    private bool fIsStopped = false;
    private bool fIsStoppedForce;
    private int fCurrentIndex = 0;
    private AutoResetEvent fGate = new AutoResetEvent(false);
    private Hashtable fQueue = new Hashtable();

    private void Start()
    {
      new Thread(new ThreadStart(ExecutionMethod)).Start();
    }

    public void Stop(bool Force)
    {
      fIsStoppedForce = Force;
      fIsStopped = true;
      lock (fQueue)
        fGate.Set();
    }

    protected void ExecutionMethod()
    {
      while (fImpl.ExecutionIteration()) ;
      //  Send termination message
      //  SendReply(new LinkSequence(fCurrentIndex));
    }

    private interface IImpl
    {
      bool ExecutionIteration();
      void Add(int NewIndex, LinkMessage Message);
    }

    #region Lossy

    private class ImplLossy : IImpl
    {
      internal ImplLossy(Sequence Owner)
      {
        fOwner = Owner;
      }

      private Sequence fOwner;

      public bool ExecutionIteration()
      {
        //  Might be time to stop
        if (fOwner.fIsStopped)
          lock (fOwner.fQueue)
            if (fOwner.fIsStoppedForce || (fOwner.fQueue.Count == 0))
              return false;
        //  Get next item
        object item = null;
        lock (fOwner.fQueue)
          if (fOwner.fQueue.Count > 0)
          {
            while (item == null)
              item = fOwner.fQueue[++fOwner.fCurrentIndex];
            fOwner.fQueue.Remove(fOwner.fCurrentIndex);
          }
        //  Handle empty queue
        if (item == null)
          fOwner.fGate.WaitOne();
        //  Handle message
        else
          try
          {
            ((LinkMessage)item).ActionNext();
          }
          catch (Exception x)
          {
            MorphErrors.NotifyAbout(x);
          }
        return true;
      }

      public void Add(int NewIndex, LinkMessage Message)
      {
        lock (fOwner.fQueue)
        {
          if (fOwner.fCurrentIndex <= NewIndex)
            fOwner.fQueue[NewIndex] = Message;
          //  Make sure the execution thread is awake
          fOwner.fGate.Set();
        }
      }
    }

    #endregion

    #region Lossless

    private class ImplLossless : IImpl
    {
      internal ImplLossless(Sequence Owner)
      {
        fOwner = Owner;
      }

      private Sequence fOwner;
      private int fFurthestIndex = 1;

      public bool ExecutionIteration()
      {
        //  Might be time to stop
        if (fOwner.fIsStopped)
          lock (fOwner.fQueue)
            if (fOwner.fIsStoppedForce || (fOwner.fQueue.Count == 0))
              return false;
        //  Get next item
        object item = null;
        lock (fOwner.fQueue)
          if (fOwner.fQueue.Count > 0)
            item = fOwner.fQueue[fOwner.fCurrentIndex];
        //  Handle empty queue
        if (item == null)
        {
          fOwner.fGate.WaitOne();
          return true;
        }
        //  Handle message
        if (item is LinkMessage)
        {
          try
          {
            fOwner.fQueue.Remove(fOwner.fCurrentIndex);
            fOwner.fCurrentIndex++;
            ((LinkMessage)item).ActionNext();
          }
          catch (Exception x)
          {
            MorphErrors.NotifyAbout(x);
          }
          return true;
        }
        //  Handle timeout
        if (item is DateTime)
        {
          //  Send Resend requests for those messages that have timed out
          List<int> Resends = new List<int>();
          DateTime EarliestDue = DateTime.MaxValue;
          lock (fOwner.fQueue)
            for (int i = fOwner.fCurrentIndex; i < fFurthestIndex; i++)
            {
              item = fOwner.fQueue[i];
              if (!(item is DateTime))
                break;
              DateTime DueAt = (DateTime)item;
              //  If it was due before now then...
              if (DueAt.CompareTo(DateTime.Now) < 0)
              {
                //  ...update the due time
                DueAt = DueAt.Add(fOwner.fTimeout);
                fOwner.fQueue[i] = DueAt;
                //  ...make a note to send Resend request
                Resends.Add(i);
              }
              if (DueAt.CompareTo(EarliestDue) < 0)
                EarliestDue = DueAt;
            }
          //  Send Resend requests (outside the lock)
          for (int i = 0; i < Resends.Count; i++)
            fOwner.SendReply(new LinkSequenceIndexReply(fOwner.fSenderID, Resends[i], true));
          //  Wait for the earliest timeout to expire
          //  Note: if EarliestDue = MaxValue, then fGate will also be set, yielding no wait.
          int WaitTime = EarliestDue.Subtract(DateTime.Now).Milliseconds;
          if (WaitTime > 0)
            fOwner.fGate.WaitOne(WaitTime, false);
        }
        return true;
      }

      public void Add(int NewIndex, LinkMessage Message)
      {
        if (fOwner.fCurrentIndex == 0)
          fOwner.fCurrentIndex = 1;
        lock (fOwner.fQueue)
          //  May be a resend for a message that's already been digested
          if (fOwner.fCurrentIndex <= NewIndex)
          {
            //  Add the message into the queue. (Replacing an existing message is fine.)
            fOwner.fQueue[NewIndex] = Message;
            //  Update the furthest index
            if (fFurthestIndex < NewIndex)
            {
              fFurthestIndex++;
              //  Fill in any Lossy with "expected by" times
              if (fFurthestIndex < NewIndex)
              {
                DateTime ExpectedBy = DateTime.Now.Add(fOwner.Timeout);
                do
                {
                  fOwner.fQueue[fFurthestIndex] = ExpectedBy;
                  fFurthestIndex++;
                } while (fFurthestIndex < NewIndex);
              }
            }
            //  Make sure the execution thread is awake
            fOwner.fGate.Set();
          }
        //  Send Ack
        fOwner.SendReply(new LinkSequenceIndexReply(fOwner.fSenderID, NewIndex, false));
      }
    }

    #endregion

    #endregion

    public Link StartLink()
    {
      lock (fQueue)
        if (fCurrentIndex == 0)
          return new LinkSequenceStartReply(fSequenceID, fSenderID, IsLossless);
        else
          return null;
    }
  }

  public static class Sequences
  {
    static internal Hashtable All = new Hashtable();
    static private IDSeed fSequenceIDSeed = new IDSeed();

    static internal Sequence Find(int SequenceID)
    {
      lock (All)
        return (Sequence)All[SequenceID];
    }

    static public Sequence New(LinkStack Path, bool IsLossless)
    {
      lock (All)
      {
        int SequenceID;
        do
        {
          SequenceID = fSequenceIDSeed.Generate();
        } while (All.Contains(SequenceID));
        return new Sequence(SequenceID, IsLossless);
      }
    }

    static public TimeSpan DefaultTimeout = new TimeSpan(0, 0, 5);
  }
}