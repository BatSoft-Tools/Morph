using System;
using System.Collections;
using Morph.Lib;

namespace Morph.Sequencing
{
  public class SequenceSender : IDisposable
  {
    internal SequenceSender(int SenderID, bool IsLossless)
    {
      fSenderID = SenderID;
      this.IsLossless = IsLossless;
      lock (SequenceSenders.All)
        SequenceSenders.All.Add(SenderID, this);
    }

    #region IDisposable Members

    public void Dispose()
    {
      lock (SequenceSenders.All)
        SequenceSenders.All.Remove(fSenderID);
    }

    #endregion

    private int fSequenceID = 0;
    public int SequenceID
    {
      get { return fSequenceID; }
      set { fSequenceID = value; }
    }

    private int fSenderID;
    public int SenderID
    {
      get { return fSenderID; }
    }

    private int fIndex = 0;
    private Hashtable fNotAcked = null;

    public bool IsLossless
    {
      get { return fNotAcked != null; }
      set
      {
        if (IsLossless != value)
          if (value)
            fNotAcked = new Hashtable();
          else
            throw new EMorphUsage("Losslessness cannot be turned off.  Instead, replace current sequence with a lossy sequence.");
      }
    }

    public bool IsStopped
    {
      get { return fSenderID == 0; }
    }

    public void AddNextLink(bool IsLast, LinkMessage Message)
    {
      lock (this)
      {
        //  Already stopped
        if (fSenderID == 0)
          throw new EMorphUsage("Cannot use a sequence that has been stopped.");
        //  Next
        if (fSequenceID != 0)
        {
          ++fIndex;
          if (fNotAcked != null)
            lock (fNotAcked)
              fNotAcked[fIndex] = Message;
          Message.PathTo.Push(new LinkSequenceIndexSend(fSequenceID, fIndex, IsLast));
        }
        //  Last, but never even started
        else if (IsLast)
        {
          Expire();
          return;
        }
        //  Start
        if ((fSequenceID == 0) || (fIndex == 1))
          Message.PathTo.Push(new LinkSequenceStartSend(fSequenceID, fSenderID, IsLossless));
      }
    }

    public void Expire()
    {
      lock (this)
      {
        fSenderID = 0;
        if ((fNotAcked == null) || (fNotAcked.Count == 0))
          Dispose();
      }
    }

    public void Halt()
    {
      lock (this)
      {
        fSenderID = 0;
        Dispose();
      }
    }

    private void TryEnd()
    {
      lock (this)
      {
        if ((fNotAcked != null) && (fNotAcked.Count == 0))
          Dispose();
      }
    }

    internal void Ack(int Index)
    {
      if (fNotAcked != null)
        lock (fNotAcked)
          fNotAcked.Remove(Index);
    }

    internal void Resend(int Index)
    {
      if (fNotAcked != null)
      {
        LinkMessage Message;
        lock (fNotAcked)
          Message = (LinkMessage)fNotAcked[Index];
        Message.ActionNext();
      }
    }
  }

  public static class SequenceSenders
  {
    static internal Hashtable All = new Hashtable();
    static private IDSeed fSenderIDSeed = new IDSeed();

    static internal SequenceSender Find(int SenderID)
    {
      lock (All)
        return (SequenceSender)All[SenderID];
    }

    static internal SequenceSender New(LinkStack Path, bool IsLossless)
    {
      lock (All)
      {
        int SenderID;
        do
        {
          SenderID = fSenderIDSeed.Generate();
        } while (All.Contains(SenderID));
        return new SequenceSender(SenderID, IsLossless);
      }
    }
  }
}