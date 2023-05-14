using System.Net.Sockets;
using Morph.Params;

namespace Morph.Daemon
{
  public class RegisteredService : AwareSocketObject<string>
  {
    internal RegisteredService(ServicesImpl Services, Socket Socket, string ServiceName, bool AccessLocal, bool AccessRemote)
      : base(Services, ServiceName, Socket)
    {
      _ServiceName = ServiceName;
      _AccessLocal = AccessLocal;
      _AccessRemote = AccessRemote;
      //  Notify any threads that are waiting for this
      RegisteredStartup Startup = StartupImpl.Find(_ServiceName);
      if (Startup != null)
        Startup.ApplicationStarted(this);
    }

    public override void Dispose()
    {
      base.Dispose();
      //  Tidy up startup
      RegisteredStartup Startup = StartupImpl.Find(_ServiceName);
      if (Startup != null)
        Startup.ApplicationStopped();
    }

    private string _ServiceName;
    public string ServiceName
    {
      get { return _ServiceName; }
    }

    private bool _AccessLocal;
    public bool AccessLocal
    {
      get { return _AccessLocal; }
    }

    private bool _AccessRemote;
    public bool AccessRemote
    {
      get { return _AccessRemote; }
    }
  }

  public class ServicesImpl : AwareObjects<string>, IMorphParameters
  {
    #region Internal

    internal ServicesImpl()
      : base("Services")
    {
      instance = this;
    }

    static internal ServiceCallbacks _ServiceCallbacks = new ServiceCallbacks();

    static private ServicesImpl instance;

    static internal RegisteredService Find(string ServiceName)
    {
      return (RegisteredService)instance.FindByKey(ServiceName);
    }

    #endregion

    public void start(LinkMessageDaemon Message, string serviceName, bool accessLocal, bool accessRemote)
    {
      lock (_Lock)
      {
        //  Don't allow duplicates
        if (FindByKey(serviceName) != null)
          throw new EMorphDaemon("Service '" + serviceName + "' is already registered.");
        //  Register the new service
        new RegisteredService(this, Message.SourceSocket, serviceName, accessLocal, accessRemote);
      }
      ServicesImpl._ServiceCallbacks.DoCallbackAdded(serviceName);
    }

    public void stop(LinkMessageDaemon Message, string serviceName)
    {
      lock (_Lock)
      {
        //  Can't remove something that's not there
        RegisteredService Service = (RegisteredService)FindByKey(serviceName);
        if (Service == null)
          return;
        //  Deregister the service
        Service.Dispose();
      }
      ServicesImpl._ServiceCallbacks.DoCallbackRemoved(serviceName);
    }

    public DaemonService[] listServices(LinkMessageDaemon Message)
    {
      AwareObject<string>[] array;
      lock (_Lock)
        array = ListElems();
      if (array == null)
        return null;
      DaemonService[] result = new DaemonService[array.Length];
      for (int i = array.Length - 1; i >= 0; i--)
      {
        RegisteredService elem = (RegisteredService)array[i];
        result[i].serviceName = elem.ServiceName;
        result[i].accessLocal = elem.AccessLocal;
        result[i].accessRemote = elem.AccessRemote;
      }
      return result;
    }

    public void listen(LinkMessageDaemon Message, ServiceCallback callback)
    {
      _ServiceCallbacks.Listen(callback);
    }

    public void unlisten(LinkMessageDaemon Message, ServiceCallback callback)
    {
      _ServiceCallbacks.Removed(callback);
    }
  }

  public struct DaemonService
  {
    public string serviceName;
    public bool accessLocal;
    public bool accessRemote;
  }
}