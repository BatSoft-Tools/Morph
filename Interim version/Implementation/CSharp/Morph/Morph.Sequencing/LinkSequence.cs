using Morph.Lib;

namespace Morph.Sequencing
{
  public abstract class LinkSequence : Link
  {
    protected LinkSequence()
      : base(LinkTypeSequence.instance)
    {
    }

    public abstract object FindLinkObject();

    #region Link

    public override int Size()
    {
      return 9;
    }

    #endregion
  }

  public class LinkTypeSequence : LinkType
  {
    static internal LinkTypeSequence instance = new LinkTypeSequence();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Sequence; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool IsStart, ToSender, z;
      Reader.ReadLinkByte(out IsStart, out ToSender, out z);
      int Value1 = Reader.ReadInt32();
      int Value2 = Reader.ReadInt32();
      if (IsStart)
        if (ToSender)
          return new LinkSequenceStartReply(Value1, Value2, z);
        else
          return new LinkSequenceStartSend(Value1, Value2, z);
      else  //  IsIndex
        if (ToSender)
          return new LinkSequenceIndexReply(Value1, Value2, z);
        else
          return new LinkSequenceIndexSend(Value1, Value2, z);
    }

    #endregion
  }
}