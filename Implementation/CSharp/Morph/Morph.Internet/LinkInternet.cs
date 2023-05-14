using System.Net;
using System.Net.Sockets;
using Morph.Lib;

namespace Morph.Internet
{
  public abstract class LinkInternet : Link
  {
    protected LinkInternet(IPEndPoint EndPoint)
      : base(LinkTypeInternet.instance)
    {
      fEndPoint = EndPoint;
    }

    public const int MorphPort = 0xE000;

    private IPEndPoint fEndPoint;
    public IPEndPoint EndPoint
    {
      get { return fEndPoint; }
    }

    static public LinkInternet New(EndPoint EndPoint)
    {
      if (!(EndPoint is IPEndPoint))
        throw new EMorphImplementation();
      if (EndPoint.AddressFamily == AddressFamily.InterNetworkV6)
        return new LinkInternetIPv6((IPEndPoint)EndPoint);
      else
        return new LinkInternetIPv4((IPEndPoint)EndPoint);
    }

    public override bool Equals(object obj)
    {
      return (obj is LinkInternet) && (((LinkInternet)obj).EndPoint.Equals(fEndPoint));
    }

    public override int GetHashCode()
    {
      return fEndPoint.GetHashCode();
    }

    public override string ToString()
    {
      return "{Internet EndPoint=" + EndPoint.ToString() + '}';
    }
  }

  public class LinkTypeInternet : LinkType
  {
    static internal LinkTypeInternet instance = new LinkTypeInternet();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Internet; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool IsIPv6, IsString, HasPort;
      Reader.ReadLinkByte(out IsIPv6, out IsString, out HasPort);
      if (IsIPv6)
        return LinkInternetIPv6.ReadNew(Reader, IsString, HasPort);
      else
        return LinkInternetIPv4.ReadNew(Reader, IsString, HasPort);
    }

    #endregion
  }
}