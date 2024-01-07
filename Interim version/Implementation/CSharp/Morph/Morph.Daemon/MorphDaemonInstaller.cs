using Bat.Library.Service;
using System.ComponentModel;
using System.ServiceProcess;

namespace Morph.Daemon
{
  [RunInstallerAttribute(true)]
  public class MorphDaemonInstaller : WindowsServiceInstaller
  {
    protected override void Initialise(out ServiceAccount Account, out ServiceStartMode StartMode, out string ServiceName, out string DisplayName, out string Description)
    {
      Account = ServiceAccount.NetworkService;
      StartMode = ServiceStartMode.Automatic;
      ServiceName = MorphDaemonService.InstallName;
      DisplayName = MorphDaemonService.DisplayName;
      Description = MorphDaemonService.Description;
    }
  }
}