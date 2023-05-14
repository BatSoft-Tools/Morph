using Morph.Lib;

namespace Morph.Sequencing
{
  public class LinkSequenceIndexSend : LinkSequence
  {
    public LinkSequenceIndexSend(int SequenceID, int Index, bool IsLast)
      : base()
    {
      fSequenceID = SequenceID;
      fIndex = Index;
      fIsLast = IsLast;
    }

    private int fSequenceID;
    public int SequenceID
    {
      get { return fSequenceID; }
    }

    private int fIndex;
    public int Index
    {
      get { return fIndex; }
    }

    private bool fIsLast;
    public bool IsLast
    {
      get { return fIsLast; }
    }

    public override object FindLinkObject()
    {
      return Sequences.Find(fSequenceID);
    }

    #region Link

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, false, false, fIsLast);
      Writer.WriteInt32(fSequenceID);
      Writer.WriteInt32(fIndex);
    }

    public override void Action(LinkMessage Message)
    {
      if (Message.PathTo.Peek() == this)
        Message.PathTo.Pop();
      Sequence Sequence = Sequences.Find(fSequenceID);
      if (Sequence == null)
        throw new EMorph("SequenceID " + fSequenceID.ToString() + " not found.");
      if (fIsLast)
        Sequence.Stop(fIndex);
      else
        Sequence.Index(fIndex, Message);
    }

    #endregion

    public override string ToString()
    {
      string str = "{Sequence";
      str += " SequenceID=" + fSequenceID.ToString();
      str += " Index=" + fIndex.ToString();
      if (fIsLast)
        str += " IsLast";
      return str + '}';
    }
  }

  public class LinkSequenceIndexReply : LinkSequence
  {
    public LinkSequenceIndexReply(int SenderID, int Index, bool Resend)
      : base()
    {
      fSenderID = SenderID;
      fIndex = Index;
      fResend = Resend;
    }

    private int fSenderID;
    public int SenderID
    {
      get { return fSenderID; }
    }

    private int fIndex;
    public int Index
    {
      get { return fIndex; }
    }

    private bool fResend;
    public bool Resend
    {
      get { return fResend; }
    }

    public override object FindLinkObject()
    {
      return SequenceSenders.Find(fSenderID);
    }

    #region Link

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, false, true, fResend);
      Writer.WriteInt32(fSenderID);
      Writer.WriteInt32(fIndex);
    }

    public override void Action(LinkMessage Message)
    {
      if (Message.PathTo.Peek() == this)
        Message.MoveToNextLink();
      SequenceSender Sender = SequenceSenders.Find(fSenderID);
      if (Sender == null)
        throw new EMorph("Sequence SenderID " + fSenderID.ToString() + " not found.");
      if (fResend)
        Sender.Resend(fIndex);
      else
        Sender.Ack(fIndex);
      Message.ActionNext();
    }

    #endregion

    public override string ToString()
    {
      string str = "{Sequence";
      str += " SenderID=" + fSenderID.ToString();
      str += " Index=" + fIndex.ToString();
      if (fResend)
        str += " Resend";
      else
        str += " Ack";
      return str + '}';
    }
  }
}