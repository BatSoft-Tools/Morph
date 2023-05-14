using System;
using Morph.Endpoint;
using Morph.Params;
using Morph.Sequencing;

namespace Morph.Daemon.Client
{
  public class MorphManagerServices : DaemonClient
  {
    #region Internal

    internal MorphManagerServices(TimeSpan DefaultTimeout)
      : base("Morph.Services", DefaultTimeout)
    {
    }

    private Service RegisterLocalService(string serviceName, bool accessLocal, bool accessRemote, ApartmentFactory apartmentFactory)
    {
      Service Service = Services.Register(serviceName, apartmentFactory);
      //  Try to register the service with the Morph Daemon
      try
      { //  Tell the Morph daemon to redirect service requests to here
        ServletProxy.CallMethod("start", new object[] { serviceName, accessLocal, accessRemote });
        //  Done
        return Service;
      }
      catch
      { //  If that fails, then tidy up
        Services.Deregister(serviceName);
        throw;
      }
    }

    #endregion

    public Service startServiceShared(string serviceName, bool accessLocal, bool accessRemote, object defaultObject, InstanceFactories instanceFactories)
    {
      ApartmentFactory apartmentFactory = new ApartmentFactoryShared(defaultObject, instanceFactories);
      return RegisterLocalService(serviceName, accessLocal, accessRemote, apartmentFactory);
    }

    public Service startServiceSessioned(string serviceName, bool accessLocal, bool accessRemote, DefaultServletObjectFactory defaultObjectFactory, InstanceFactories instanceFactories, TimeSpan timeout, SequenceLevel sequenceLevel)
    {
      ApartmentFactory apartmentFactory = new ApartmentFactorySession(defaultObjectFactory, instanceFactories, timeout, sequenceLevel);
      return RegisterLocalService(serviceName, accessLocal, accessRemote, apartmentFactory);
    }

    public Service startServiceSessioned(string serviceName, bool accessLocal, bool accessRemote, ApartmentFactorySession apartmentFactory)
    {
      return RegisterLocalService(serviceName, accessLocal, accessRemote, apartmentFactory);
    }

    public void stopService(string serviceName)
    {
      try
      {
        ServletProxy.CallMethod("stop", new object[] { serviceName });
      }
      finally
      {
        Services.Deregister(serviceName);
      }
    }

    public void stopService(Service service)
    {
      try
      {
        ServletProxy.CallMethod("stop", new object[] { service.Name });
      }
      finally
      {
        service.Deregister();
      }
    }

    public DaemonService[] listServices()
    {
      return (DaemonService[])ServletProxy.CallMethod("listServices", null);
    }

    public void listen(DaemonServiceCallback callback)
    {
      ServletProxy.CallMethod("listen", new object[] { callback });
    }

    public void unlisten(DaemonServiceCallback callback)
    {
      ServletProxy.CallMethod("unlisten", new object[] { callback });
    }
  }

  public struct DaemonService
  {
    public string serviceName;
    public bool accessLocal;
    public bool accessRemote;
  }
}