using System;
using Morph.Base;
using Morph.Core;
using Morph.Endpoint;
using Morph.Internet;
using Morph.Sequencing;

namespace Morph.Daemon.Client
{
  static public class MorphManager
  {
    static MorphManager()
    {
      //  Register link types
      LinkTypes.Register(new LinkTypeEnd());
      LinkTypes.Register(new LinkTypeMessage());
      LinkTypes.Register(new LinkTypeData());
      LinkTypes.Register(new LinkTypeInternet());
      LinkTypes.Register(new LinkTypeService());
      LinkTypes.Register(new LinkTypeServlet());
      LinkTypes.Register(new LinkTypeMember());
      LinkTypes.Register(new LinkTypeSequence());
      //  Ensure that initial responses are handled by at least some thread(s)
      ThreadCount = 2;
      //  Initialise so that MorphApartment ID's and MorphApartmentProxy ID's will be generated by the Morph Daemon
      MorphApartment.IDFactory = new MorphManagerApartmentItems("Morph.Apartments", ReplyTimeout);
      MorphApartmentProxy.IDFactory = new MorphManagerApartmentItems("Morph.ApartmentProxies", ReplyTimeout);
    }

    static public TimeSpan ReplyTimeout = new TimeSpan(0, 20, 0);

    static public int ThreadCount
    {
      set { ActionHandler.SetThreadCount(value); }
    }

    static private MorphManagerServices _Services = null;
    static public MorphManagerServices Services
    {
      get
      {
        if (_Services == null)
          _Services = new MorphManagerServices(ReplyTimeout);
        return _Services;
      }
    }

    static private MorphManagerStartups _Startups = null;
    static public MorphManagerStartups Startups
    {
      get
      {
        if (_Startups == null)
          _Startups = new MorphManagerStartups(ReplyTimeout);
        return _Startups;
      }
    }

    static public void startup(int threadCount)
    {
      ThreadCount = threadCount;
      //      ListenerManager.Obtain(LinkInternet.MorphPort).StartAll();
    }

    static public void shutdown()
    {
      //      ListenerManager.Find(LinkInternet.MorphPort).StopAll();
      ActionHandler.SetThreadCount(0);
      Connections.CloseAll();
    }
  }
}