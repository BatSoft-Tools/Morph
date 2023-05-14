using System;
using Bat.Library.Logging;
using Bat.Library.Service;
using Morph.Endpoint;
using Morph.Internet;
using Morph.Params;

namespace Morph.Daemon
{
  public class MorphDaemonService : WindowsService
  {
    public MorphDaemonService()
      : base()
    {
      ServiceName = InstallName;
      CanHandlePowerEvent = false;
      CanHandleSessionChangeEvent = false;
      CanPauseAndContinue = false;
      CanShutdown = true;
      CanStop = true;
      AutoLog = true;
      IsLogging = false;
    }

    public const string InstallName = "Morph.Daemon";
    public const string DisplayName = "Morph Daemon";
    public const string Description = "Connects applications that communicate via the Morph protocol.";

    public const int ThreadCount = 5;
    public string ServiceName_ServicesStartup = "Morph.Startup";
    public string ServiceName_Services = "Morph.Services";
    public string ServiceName_Apartments = "Morph.Apartments";
    public string ServiceName_ApartmentProxies = "Morph.ApartmentProxies";

    protected override void DoStart(string[] args)
    {
      try
      {
        //  Register link types
        LinkTypeEnd.Register();
        LinkTypeMessageDaemon.Register();
        LinkTypeData.Register();
        LinkTypeInternet.Register();
        LinkTypeServiceDaemon.Register();
        LinkTypeServlet.Register();
        LinkTypeException.Register();
        LinkTypeMember.Register();
        //  Create instance factory
        DaemonFactory DaemonFactory = new DaemonFactory();
        InstanceFactories SimpleFactory = new InstanceFactories();
        //  Register the services
        ActionHandler.SetThreadCount(ThreadCount);
        Services.Register(ServiceName_ServicesStartup, new ApartmentFactoryShared(new StartupImpl(), DaemonFactory));
        Services.Register(ServiceName_Services, new ApartmentFactoryShared(new ServicesImpl(), DaemonFactory));
        Services.Register(ServiceName_Apartments, new ApartmentFactoryShared(new ApartmentsImpl(), SimpleFactory));
        Services.Register(ServiceName_ApartmentProxies, new ApartmentFactoryShared(new ApartmentProxiesImpl(), SimpleFactory));
        ListenerManager.Obtain(LinkInternet.MorphPort).StartAll();
      }
      catch (Exception x)
      {
        Log.Default.Add(x);
      }
    }

    protected override int DoStop()
    {
      try
      {
        //  Deregister the service
        Services.Deregister(ServiceName_ServicesStartup);
        Services.Deregister(ServiceName_Services);
        Services.Deregister(ServiceName_Apartments);
        Services.Deregister(ServiceName_ApartmentProxies);
        ListenerManager.Find(LinkInternet.MorphPort).StopAll();
        ActionHandler.SetThreadCount(0);
        Connections.CloseAll();
        return 0;
      }
      catch (Exception x)
      {
        Log.Default.Add(x);
        return 1;
      }
    }
  }
}