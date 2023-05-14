using Morph.Lib;

namespace Morph.Endpoint
{
  public class LinkServlet : Link
  {
    internal LinkServlet(int ServletID)
      : base(LinkTypeServlet.instance)
    {
      fServetID = ServletID;
    }

    private int fServetID;
    public int ServletID
    {
      get { return fServetID; }
    }

    #region Link implementation

    public override int Size()
    {
      return 5;
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, false, false, false);
      Writer.WriteInt32(ServletID);
    }

    public override void Action(LinkMessage Message)
    {
      //  In this implementation we let LinkApartment do the work,
      //  so this Action should not be called, meaning that there's a mistake somewhere.
      throw new EMorph("Unexpected link type");
    }

    #endregion

    public override bool Equals(object obj)
    {
      return (obj is LinkServlet) && (((LinkServlet)obj).ServletID == fServetID);
    }

    public override int GetHashCode()
    {
      return fServetID;
    }

    public override string ToString()
    {
      return "{Servlet ID=" + ServletID.ToString() + '}';
    }
  }

  public class LinkTypeServlet : LinkType
  {
    static internal LinkTypeServlet instance = new LinkTypeServlet();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Servlet; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      Reader.ReadInt8();
      return new LinkServlet(Reader.ReadInt32());
    }

    #endregion
  }
}