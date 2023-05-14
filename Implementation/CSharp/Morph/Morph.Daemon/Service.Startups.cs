using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using Morph.Params;

namespace Morph.Daemon
{
  public class RegisteredStartup : AwareObject<string>
  {
    internal RegisteredStartup(StartupImpl Owner, string ServiceName, string FileName, string Parameters, TimeSpan Timeout)
      : base(Owner, ServiceName)
    {
      _ServiceName = ServiceName;
      _FileName = FileName;
      _Parameters = Parameters;
      _Timeout = Timeout;
    }

    private string _ServiceName;
    public string ServiceName
    {
      get { return _ServiceName; }
    }

    private string _FileName;
    public string FileName
    {
      get { return _FileName; }
    }

    private string _Parameters;
    public string Parameters
    {
      get { return _Parameters; }
    }

    private TimeSpan _Timeout;
    public TimeSpan Timeout
    {
      get { return _Timeout; }
    }

    #region Internal

    private bool _IsRunning = false;
    private List<EventWaitHandle> _Waits = new List<EventWaitHandle>();
    private void ApplicationStart()
    { //  Copied from http://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
      //Create process
      Process pProcess = new Process();
      //path and file name of command to run
      pProcess.StartInfo.FileName = _FileName;
      //parameters to pass to program
      pProcess.StartInfo.Arguments = _Parameters;
      //pProcess.StartInfo.UseShellExecute = true;
      //Set output of program to be written to process output stream
      //pProcess.StartInfo.RedirectStandardOutput = false;
      //Optional
      //pProcess.StartInfo.WorkingDirectory = strWorkingDirectory;
      //Start the process
      pProcess.Start();
      //Get program output
      //string strOutput = pProcess.StandardOutput.ReadToEnd();
      //Wait for process to finish
      //pProcess.WaitForExit();
    }

    internal void ApplicationStarted(RegisteredService Service)
    {
      lock (_Waits)
        for (int i = 0; i < _Waits.Count; i++)
          _Waits[i].Set();
    }

    internal void ApplicationStopped()
    {
      _IsRunning = false;
    }

    #endregion

    /*
     * This method has its design flaws, but it is thread safe enough for non-critical use.
     */
    public RegisteredService Startup()
    {
      bool IsStartupThread = false;
      RegisteredService Service = null;
      EventWaitHandle Wait = new AutoResetEvent(false);
      lock (_Waits)
        _Waits.Add(Wait);
      try
      {
        //  Determine if this thread should start the service
        lock (this)
        {
          IsStartupThread = !_IsRunning;
          _IsRunning = true;
        }
        try
        {
          //  Start up
          if (IsStartupThread)
            ApplicationStart();
          //  Wait for it
          if (Wait.WaitOne(Timeout))
            //  Service started
            Service = ServicesImpl.Find(ServiceName);
          else
            //  Waited for too long
            Service = null;
        } //  Tidy up
        finally
        {
          if (IsStartupThread)
            lock (this)
              _IsRunning = Service != null;
        }
      }
      finally
      {
        lock (_Waits)
          _Waits.Remove(Wait);
      }
      //  Final result
      if (Service == null)
        throw new EMorphDaemon("Failed to obtain service \"" + ServiceName + '\"');
      return Service;
    }
  }

  public class StartupImpl : AwareObjects<string>, IMorphParameters
  {
    #region Internal

    internal StartupImpl()
      : base("ServicesStartup")
    {
      instance = this;
      Load();
    }

    static internal StartupImpl instance;
    static internal ServiceCallbacks _ServiceCallbacks = new ServiceCallbacks();

    static public RegisteredStartup Find(string ServiceName)
    {
      return (RegisteredStartup)instance.FindByKey(ServiceName);
    }

    #endregion

    #region Persistance

    private RegistryKey MorphKey()
    {
      RegistryKey Key = Registry.CurrentUser;
      Key = Key.CreateSubKey("Software");
      Key = Key.CreateSubKey("Morph");
      Key = Key.CreateSubKey("Startups");
      return Key;
    }

    private void Load()
    {
      RegistryKey Key = MorphKey();
      foreach (string serviceName in Key.GetSubKeyNames())
      {
        RegistryKey ServiceKey=Key.OpenSubKey(serviceName);
        string fileName = (string)ServiceKey.GetValue("Filename");
        string parameters = (string)ServiceKey.GetValue("Parameters");
        int timeout = (int)ServiceKey.GetValue("Timeout");
        new RegisteredStartup(this, serviceName, fileName, parameters, new TimeSpan(0, 0, timeout));
      }
    }

    #endregion

    public void refresh(LinkMessageDaemon Message)
    {
      Load();
    }

    public void add(LinkMessageDaemon Message, string serviceName, string fileName, string parameters, int timeout)
    {
      RegisteredStartup Startup;
      lock (_Lock)
      {
        //  Don't allow duplicates
        if (FindByKey(serviceName) != null)
          throw new EMorphDaemon("Service '" + serviceName + "' is already registered.");
        //  Register the new service
        Startup = new RegisteredStartup(this, serviceName, fileName, parameters, new TimeSpan(0, 0, timeout));
        try
        { //  Save
          RegistryKey Key = MorphKey().CreateSubKey(serviceName);
          Key.SetValue("Filename", fileName, RegistryValueKind.String);
          Key.SetValue("Parameters", parameters, RegistryValueKind.String);
          Key.SetValue("Timeout", timeout, RegistryValueKind.DWord);
        }
        catch
        {
          Startup.Dispose();
          throw;
        }
      }
      //  Fire event
      StartupImpl._ServiceCallbacks.DoCallbackAdded(serviceName);
    }

    public void remove(LinkMessageDaemon Message, string serviceName)
    {
      lock (_Lock)
      {
        //  Can't remove something that's not there
        RegisteredStartup Service = Find(serviceName);
        if (Service == null)
          return;
        //  Verify access rights

        //  Deregister the service
        Service.Dispose();
        //  Save
        RegistryKey Key = MorphKey();
        Key.DeleteSubKey(serviceName);
      }
      StartupImpl._ServiceCallbacks.DoCallbackRemoved(serviceName);
    }

    public DaemonStartup[] listServices(LinkMessageDaemon Message)
    {
      AwareObject<string>[] array;
      lock (_Lock)
        array = ListElems();
      if (array == null)
        return null;
      DaemonStartup[] result = new DaemonStartup[array.Length];
      for (int i = array.Length - 1; i >= 0; i--)
      {
        RegisteredStartup elem = (RegisteredStartup)array[i];
        result[i].serviceName = elem.ServiceName;
        result[i].fileName = elem.FileName;
        result[i].timeout = (int)elem.Timeout.TotalSeconds;
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

  public struct DaemonStartup
  {
    public string serviceName;
    public string fileName;
    public int timeout;
  }
}