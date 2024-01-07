using Morph.Lib;

namespace Morph
{
  public class LinkEnd : Link
  {
    internal LinkEnd(LinkType LinkType)
      : base(LinkType)
    {
    }

    #region Link

    public override int Size()
    {
      return 1;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteInt8((byte)LinkTypeID.End);
    }

    public override void Action(LinkMessage Message)
    {
      //  Do nothing
    }

    #endregion

    public override bool Equals(object obj)
    {
      return obj is LinkEnd;
    }

    public override int GetHashCode()
    {
      return 0;
    }

    public override string ToString()
    {
      return "{End}";
    }
  }

  public class LinkTypeEnd : LinkType
  {
    static private LinkEnd fEnd = new LinkEnd(null);
    static public LinkEnd End
    {
      get { return fEnd; }
    }

    static public void Register()
    {
      LinkTypes.Register(new LinkTypeEnd());
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.End; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      Reader.ReadInt8();
      return fEnd;
    }

    #endregion
  }
}