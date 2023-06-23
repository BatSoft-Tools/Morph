using Morph.Daemon.Client;

namespace Morph.Manager;

public partial class StartupsPage : ContentPage
{
    List<DaemonStartup> startups = new(new DaemonStartup[] {
            new DaemonStartup() { ServiceName = "abc", FileName = "xyz" },
            new DaemonStartup() { ServiceName = "123", FileName = "456" } });

    public StartupsPage()
    {
        InitializeComponent();
        StartupsListView.ItemsSource = startups;
    }

    async void OnServiceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem != null)
        {
            await Navigation.PushAsync(new StartupEdit(e.SelectedItem as DaemonStartup));
        }
    }
}