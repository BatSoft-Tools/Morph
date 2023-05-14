using System.Net;
using Morph.Endpoint;
using Morph.Internet;
using Morph.Lib;

namespace Morph.Daemon
{
  public class LinkServiceDaemon : LinkService
  {
    internal LinkServiceDaemon(string ServiceName)
      : base(ServiceName)
    {
    }

    public override void Action(LinkMessage Message)
    {
      RegisteredService service = ServicesImpl.Find(ServiceName);
      //  If the service was not found then...
      if (service == null)
      { //  ...and if it can be started up then
        RegisteredStartup startup = StartupImpl.Find(ServiceName);
        if (startup != null)
        { //  ...start the service.
          service = startup.Startup();
        }
      }
      //  If still not found, then it's either a Morph daemon service or an error, so handle normally.
      if (service == null)
        base.Action(Message);
      else
      { //  The service was found.
        //  Validate access.
        IPEndPoint RemoteEndPoint = (IPEndPoint)((LinkMessageDaemon)Message).SourceSocket.RemoteEndPoint;
        bool IsLocal = Connections.IsEndPointOnThisDevice(RemoteEndPoint);
        if ((IsLocal && !service.AccessLocal) || (!IsLocal && !service.AccessRemote))
          throw new EMorph("Access denied");
        //  Send it down the socket that holds the implementation.
        Message.PathTo.Push(LinkInternet.New(service.Socket.RemoteEndPoint));
        Message.ActionNext();
      }
    }
  }

  public class LinkApartmentDaemon : LinkApartment
  {
    internal LinkApartmentDaemon(int ApartmentID)
      : base(ApartmentID)
    {
    }

    public override void Action(LinkMessage Message)
    {
      ApartmentObject apartment = ApartmentsImpl.Find(ApartmentID);
      if (apartment == null)
        base.Action(Message);
      else
      {
        Message.PathTo.Push(LinkInternet.New(apartment.Socket.RemoteEndPoint));
        Message.ActionNext();
      }
    }
  }

  public class LinkApartmentProxyDaemon : LinkApartmentProxy
  {
    internal LinkApartmentProxyDaemon(int ApartmentProxyID)
      : base(ApartmentProxyID)
    {
    }

    public override void Action(LinkMessage Message)
    {
      ApartmentObject apartment = ApartmentProxiesImpl.Find(ApartmentProxyID);
      if (apartment == null)
        base.Action(Message);
      else
      {
        Message.PathTo.Push(LinkInternet.New(apartment.Socket.RemoteEndPoint));
        Message.ActionNext();
      }
    }
  }

  public class LinkTypeServiceDaemon : LinkType
  {
    static internal LinkTypeServiceDaemon instance = new LinkTypeServiceDaemon();

    static public void Register()
    {
      LinkTypes.Register(instance);
    }

    #region LinkType Members

    public LinkTypeID ID
    {
      get { return LinkTypeID.Service; }
    }

    public Link ReadLink(StreamReader Reader)
    {
      bool IsApartment, IsProxy, z;
      Reader.ReadLinkByte(out IsApartment, out IsProxy, out z);
      if (IsApartment)
        if (IsProxy)
          return new LinkApartmentProxyDaemon(Reader.ReadInt32());
        else
          return new LinkApartmentDaemon(Reader.ReadInt32());
      else
        return new LinkServiceDaemon(Reader.ReadString());
    }

    #endregion
  }
}