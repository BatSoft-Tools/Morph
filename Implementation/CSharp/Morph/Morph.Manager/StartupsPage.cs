using Morph.Daemon.Client;

namespace Morph.Manager;

public class StartupsPage : ContentPage
{
	private ListView listView;

    List<DaemonStartup> startups = new List<DaemonStartup>(new DaemonStartup[] {
            new DaemonStartup() { serviceName = "abc", fileName = "xyz" },
            new DaemonStartup() { serviceName = "123", fileName = "456" } });

    public StartupsPage()
	{
        listView = new ListView
        {
            ItemsSource = startups,

            ItemTemplate = new DataTemplate(() =>
            {
                Label serviceNameLabel = new Label();
                serviceNameLabel.Margin = new Thickness(20, 0);
                serviceNameLabel.SetBinding(Label.TextProperty, "serviceName");

                Label fileNameLabel = new Label();
                fileNameLabel.Margin = new Thickness(20, 0);
                fileNameLabel.SetBinding(Label.TextProperty, "fileName");

                Label timeoutLabel = new Label();
                timeoutLabel.Margin = new Thickness(20, 0);
                timeoutLabel.SetBinding(Label.TextProperty, "timeout");

                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(20, 20),
                        Orientation = StackOrientation.Horizontal,
                        Children = { serviceNameLabel, fileNameLabel, timeoutLabel }
                    }
                };
            })
        };

        Content = new VerticalStackLayout
        {
            Children = { listView }
        };
    }
}