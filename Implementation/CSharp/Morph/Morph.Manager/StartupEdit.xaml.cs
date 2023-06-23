using Morph.Daemon.Client;

namespace Morph.Manager;

public partial class StartupEdit : ContentPage
{
	public DaemonStartup daemonStartup;

	public StartupEdit(DaemonStartup daemonStartup)
	{
		this.daemonStartup = daemonStartup;
		InitializeComponent();
		ServiceName.Text = daemonStartup.ServiceName;
	}
}