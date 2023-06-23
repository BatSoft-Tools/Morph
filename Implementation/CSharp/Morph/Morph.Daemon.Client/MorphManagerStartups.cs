using System;

namespace Morph.Daemon.Client
{
  public class MorphManagerStartups : DaemonClient
  {
    public MorphManagerStartups(TimeSpan DefaultTimeout)
      : base("Morph.Startup", DefaultTimeout)
    {
    }

    public void refresh()
    {
      ServletProxy.SendMethod("refresh", null);
    }

    /**
     * Timeout in in seconds
     */
    public void add(string serviceName, string fileName, string parameters, int timeout)
    {
      ServletProxy.CallMethod("add", new object[] { serviceName, fileName, parameters, timeout });
    }

    public void remove(string serviceName)
    {
      ServletProxy.CallMethod("remove", new object[] { serviceName });
    }

    public DaemonStartup[] listServices()
    {
      return (DaemonStartup[])ServletProxy.CallMethod("listServices", null);
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

  public class DaemonStartup
  {
    private string serviceName;
    private string fileName;
    private int timeout;

    public string ServiceName { get => serviceName; set => serviceName = value; }
    public string FileName { get => fileName; set => fileName = value; }
    public int Timeout { get => timeout; set => timeout = value; }
  }
}