using Morph.Lib;

namespace Morph.Sequencing
{
  public abstract class LinkSequenceStart : LinkSequence
  {
    public LinkSequenceStart(int SequenceID, int SenderID, bool IsLossless)
      : base()
    {
      fSequenceID = SequenceID;
      fSenderID = SenderID;
      fIsLossless = IsLossless;
    }

    protected int fSequenceID;
    public int SequenceID
    {
      get { return fSequenceID; }
    }

    protected int fSenderID;
    public int SenderID
    {
      get { return fSenderID; }
    }

    protected bool fIsLossless;
    public bool IsLossless
    {
      get { return fIsLossless; }
    }

    protected abstract bool ToSender
    {
      get;
    }

    #region Link

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, true, ToSender, fIsLossless);
      Writer.WriteInt32(fSequenceID);
      Writer.WriteInt32(fSenderID);
    }

    #endregion

    public override string ToString()
    {
      string str = "{Sequence";
      str += " SequenceID=" + fSequenceID.ToString();
      str += " SenderID=" + fSenderID.ToString();
      if (fIsLossless)
        str += " Lossless";
      else
        str += " Lossy";
      return str + '}';
    }
  }

  public class LinkSequenceStartSend : LinkSequenceStart
  {
    public LinkSequenceStartSend(int SequenceID, int SenderID, bool IsLossless)
      : base(SequenceID, SenderID, IsLossless)
    {
    }

    protected override bool ToSender
    {
      get { return false; }
    }

    public override object FindLinkObject()
    {
      return Sequences.Find(fSequenceID);
    }

    public override void Action(LinkMessage Message)
    {
      if (fSenderID == 0)
        throw new EMorph("At sequence, SenderID cannot be 0.");
      Sequence Sequence;
      if (fSequenceID == 0)
        //  Start new sequence
        Sequence = Sequences.New(Message.PathFrom, fIsLossless);
      else
      { //  Find existing sequence
        Sequence = Sequences.Find(fSequenceID);
        if (Sequence == null)
          throw new EMorph("SequenceID " + fSequenceID.ToString() + " does not exist.");
        Sequence.IsLossless = fIsLossless || Sequence.IsLossless;
        if (Message.PathFrom != null)
          Sequence.PathToProxy = Message.PathFrom.Clone();
      }
      //  Set the sender's ID
      Sequence.SenderID = fSenderID;
      //  Done
      Message.ActionNext();
    }
  }

  public class LinkSequenceStartReply : LinkSequenceStart
  {
    public LinkSequenceStartReply(int SequenceID, int SenderID, bool IsLossless)
      : base(SequenceID, SenderID, IsLossless)
    {
    }

    protected override bool ToSender
    {
      get { return true; }
    }

    public override object FindLinkObject()
    {
      return SequenceSenders.Find(fSenderID);
    }

    public override void Action(LinkMessage Message)
    {
      if (Message.PathTo.Peek() == this)
        Message.PathTo.Pop();
      if (fSequenceID == 0)
        throw new EMorph("At sequence sender, SequenceID cannot be 0.");
      SequenceSender Sequence;
      if (fSenderID == 0)
      { //  Start new sequence
        Sequence = SequenceSenders.New(Message.PathFrom, fIsLossless);
        fSenderID = Sequence.SenderID;
      }
      else
      { //  Find existing sequence
        Sequence = SequenceSenders.Find(fSenderID);
        if (Sequence == null)
          throw new EMorph("Sequence SenderID " + fSenderID.ToString() + " does not exist.");
        Sequence.IsLossless = fIsLossless || Sequence.IsLossless;
      }
      //  Set the sender's ID
      Sequence.SequenceID = fSequenceID;
    }
  }
}