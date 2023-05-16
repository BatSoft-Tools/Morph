using System.Configuration.Install;
using System.ServiceProcess;

namespace Bat.Library.Service
{
  /* Instructions:
   * 1.  Declare a subclass that implements the Initialise() method.
   * 2.  Preceed your sublass with:
   *       [RunInstallerAttribute(true)]
   * 3.  Using section:
   *       using Bat.Library.Service;
   *       using System.ComponentModel;
   *       using System.ServiceProcess;
   */
  public abstract class WindowsServiceInstaller : Installer
  {
    private ServiceInstaller _ServiceInstaller;
    private ServiceProcessInstaller _ServiceProcessInstaller;

    public WindowsServiceInstaller()
    {
      ServiceAccount account;
      ServiceStartMode startMode;
      string serviceName, displayName, description;
      Initialise(out account, out startMode, out serviceName, out displayName, out description);

      // Instantiate installers for process and services.
      _ServiceInstaller = new ServiceInstaller();
      _ServiceProcessInstaller = new ServiceProcessInstaller();

      // The services run under the system account.
      _ServiceProcessInstaller.Account = account;

      // The services are started upon startup.
      _ServiceInstaller.StartType = startMode;

      // ServiceName must equal those on ServiceBase derived classes.            
      _ServiceInstaller.ServiceName = serviceName;
      _ServiceInstaller.DisplayName = displayName;
      _ServiceInstaller.Description = description;

      // Add installers to collection. Order is not important.
      Installers.Add(_ServiceInstaller);
      Installers.Add(_ServiceProcessInstaller);
    }

    protected abstract void Initialise(out ServiceAccount account, out ServiceStartMode startMode, out string serviceName, out string displayName, out string description);
  }
}