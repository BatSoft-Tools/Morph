using Morph.Daemon.Client;

namespace Morph.Manager;

public partial class ServicesPage : ContentPage
{
	public List<DaemonService> morphServices = new(new DaemonService[] {
			new DaemonService() { ServiceName = "abc", AccessLocal = true , AccessRemote = false},
			new DaemonService() { ServiceName = "123", AccessLocal = true , AccessRemote = false } });

	public ServicesPage()
	{
		InitializeComponent();
		ServicesListView.ItemsSource = morphServices;
	}
}