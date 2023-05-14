using System.Collections.Generic;
using Morph.Lib;
#if LOG_MESSAGES
using Bat.Library.Logging;
#endif

namespace Morph
{
  public class Functions
  {
    #region Read/Write link byte

    private const byte _x = 0x10;   //  Flag X
    private const byte _y = 0x20;   //  Flag Y
    private const byte _z = 0x40;   //  Flag Z
    private const byte _MSB = 0x80; //  Flag MSB

    static public byte ToLinkByte(byte LinkTypeID, bool x, bool y, bool z)
    {
      return (byte)(_MSB | (z ? _z : (byte)0) | (y ? _y : (byte)0) | (x ? _x : (byte)0) | LinkTypeID);
    }

    static public void FromLinkByte(byte LinkTypeID, out bool x, out bool y, out bool z)
    {
      x = (LinkTypeID & _x) != 0;
      y = (LinkTypeID & _y) != 0;
      z = (LinkTypeID & _z) != 0;
    }

    #endregion

    #region Sizes

    static public int SizeOf(string Str)
    {
      return 4 + 2 * Str.Length;
    }

    #endregion
  }

  public abstract class Link
  {
    protected Link(LinkType LinkType)
    {
      fLinkType = LinkType;
    }

    private LinkType fLinkType;
    public LinkType LinkType
    {
      get { return fLinkType; }
    }

    public abstract int Size();
    public abstract void Write(StreamWriter Writer);
    public abstract void Action(LinkMessage Message);
  }

  public class Links
  {
    private List<Link> fLinks = new List<Link>();

    public void Push(Link link)
    {
      fLinks.Add(link);
    }

    public void Write(StreamWriter writer)
    {
      for (int i = fLinks.Count - 1; i >= 0; i--)
        fLinks[i].Write(writer);
    }
  }

  public enum LinkTypeID
  {
    End = 0x0,
    Data = 0x8,
    Message = 0x4,
    Information = 0xC,
    Service = 0x2,
    Servlet = 0xA,
    Member = 0x6,
    Exception = 0xE,
    Internet = 0x1,
    Bluetooth = 0x9,
    _5 = 0x5,
    _D = 0xD,
    Sequence = 0x3,
    Stream = 0xB,
    Encoding = 0x7,
    Batch = 0xF
  };

  public interface LinkType
  {
    LinkTypeID ID
    {
      get;
    }

    Link ReadLink(StreamReader Reader);
  }

  public class LinkTypes
  {
    static private LinkType[] fLinkTypes = new LinkType[16];
    static public LinkType ByNumber(int LinkTypeID)
    {
      return fLinkTypes[LinkTypeID];
    }

    static public void Register(LinkType LinkType)
    {
      byte LinkTypeID = (byte)LinkType.ID;
      if (fLinkTypes[LinkTypeID] != null)
        throw new EMorph("A link type for " + LinkTypeID + " is already registered");
      fLinkTypes[LinkTypeID] = LinkType;
    }

    static public Link ReadLink(StreamReader Reader)
    {
      if (!Reader.CanRead)
        return null;
      LinkType LinkType = fLinkTypes[Reader.PeekInt8() & 0xF];
      if (LinkType == null)
        throw new EMorph("Link type not available");
      return LinkType.ReadLink(Reader);
    }

    static public void ReadStream(StreamReader Reader)
    {
      while (Reader.CanRead)
      {
        Link link = ReadLink(Reader);
        if (link is LinkEnd)
          break;
        if (link is LinkMessage)
          ((LinkMessage)link).Action(null);
        else
          throw new EMorph("Unexpected link type");
      }
    }

#if LOG_MESSAGES
    static LinkTypes()
    {
      Log.Default = new Log("C:\\Temp\\Morph.log");
    }

    static public string AppName;
#endif
  }
}