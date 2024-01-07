using System.Net.Sockets;
using Morph.Lib;
using Morph.Params;

namespace Morph.Daemon
{
  public class ApartmentObject : AwareSocketObject<int>
  {
    internal ApartmentObject(ApartmentObjects Owner, Socket Socket, int ID)
      : base(Owner, ID, Socket)
    {
      _ID = ID;
    }

    private int _ID;
    public int ID
    {
      get { return _ID; }
    }
  }

  public class ApartmentObjects : AwareObjects<int>, IMorphParameters
  {
    protected ApartmentObjects(string TypeName)
      : base(TypeName)
    {
    }

    /**
     * Daemon clients will need to generate a few daemon apartment proxies while connecting
     * to the daemon (in particular for the daemon's Apartments and ApartmentProxies services).
     * The apartment proxies on the clients should be numbered incrementally from 1 onwards, so
     * by starting the daemon generated numbering a little higher, there should never be any clashes 
     * between apartment proxy ID's generated on daemon clients and apartment proxy ID's generated here.
     */
    private IDSeed _IDSeed = new IDSeed(10);

    public int obtain(LinkMessageDaemon Message)
    {
      int ID = _IDSeed.Generate();
      lock (_Lock)
      {
        new ApartmentObject(this, Message.SourceSocket, ID);
        return ID;
      }
    }

    public void release(LinkMessageDaemon Message, int id)
    {
      lock (_Lock)
      {
        //  Can't remove something that's not there
        ApartmentObject Obj = (ApartmentObject)FindByKey(id);
        if (Obj == null)
          return;
        //  Only the owner may remove a service
        if (!Message.SourceSocket.Equals(Obj.Socket))
          throw new EMorphDaemon("Permission denied.");
        //  Deregister the service
        Obj.Dispose();
      }
    }
  }

  public class ApartmentsImpl : ApartmentObjects
  {
    internal ApartmentsImpl()
      : base("Apartments")
    {
      instance = this;
    }

    static private ApartmentsImpl instance;

    static internal ApartmentObject Find(int ApartmentID)
    {
      return (ApartmentObject)instance.FindByKey(ApartmentID);
    }
  }

  public class ApartmentProxiesImpl : ApartmentObjects
  {
    internal ApartmentProxiesImpl()
      : base("ApartmentProxies")
    {
      instance = this;
    }

    static private ApartmentProxiesImpl instance;

    static internal ApartmentObject Find(int ApartmentProxyID)
    {
      return (ApartmentObject)instance.FindByKey(ApartmentProxyID);
    }
  }
}