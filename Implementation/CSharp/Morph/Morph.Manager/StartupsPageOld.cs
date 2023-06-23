using Morph.Daemon.Client;

namespace Morph.Manager;

public class StartupsPageOld : ContentPage
{
    private ListView listView;

    List<DaemonStartup> startups = new(new DaemonStartup[] {
            new DaemonStartup() { ServiceName = "abc", FileName = "xyz" },
            new DaemonStartup() { ServiceName = "123", FileName = "456" } });

    public StartupsPageOld()
    {
        listView = new ListView
        {
            ItemsSource = startups,

            ItemTemplate = new DataTemplate(() =>
            {
                Label serviceNameLabel = new Label();
                serviceNameLabel.Margin = new Thickness(50, 0);
                serviceNameLabel.SetBinding(Label.TextProperty, "ServiceName");

                Label fileNameLabel = new Label();
                fileNameLabel.Margin = new Thickness(50, 0);
                fileNameLabel.SetBinding(Label.TextProperty, "FileName");

                Label timeoutLabel = new Label();
                timeoutLabel.Margin = new Thickness(50, 0);
                timeoutLabel.SetBinding(Label.TextProperty, "Timeout");

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

        //< Grid >
        //    < Grid.RowDefinitions >
        //            < RowDefinition Height = "Auto" />
        //    </ Grid.RowDefinitions >
        //
        //    < Grid.ColumnDefinitions >
        //         < ColumnDefinition Width = "30*" />
        //         < ColumnDefinition Width = "30*" />
        //         < ColumnDefinition Width = "30*" />
        //    </ Grid.ColumnDefinitions >
        //
        //    < Label Grid.Column = "0" Text = "xxx" />
        //    < Label Grid.Column = "1" Text = "xxx" />
        //    < Label Grid.Column = "2" Text = "xxx" />
        //</ Grid >

        var grid = new Grid();

        var row = new RowDefinition();
        row.Height = GridLength.Auto;
        grid.RowDefinitions.Add(row);

        var label1 = new Label();
        label1.Text = "label1";

        var label2 = new Label();
        label2.Text = "label2";

        var label3 = new Label();
        label3.Text = "label3";

        var col1 = new ColumnDefinition();
        col1.Width = 50;
        grid.ColumnDefinitions.Add(col1);

        var col2 = new ColumnDefinition();
        col2.Width = 50;
        grid.ColumnDefinitions.Add(col2);

        var col3 = new ColumnDefinition();
        col3.Width = 50;
        grid.ColumnDefinitions.Add(col3);

        var header = grid;

        //var header = new DataTemplate(() =>
        //    {
        //        Label serviceNameLabel = new Label();
        //        serviceNameLabel.Margin = new Thickness(20, 0);
        //        serviceNameLabel.Text = "Service Name";

        //        Label fileNameLabel = new Label();
        //        fileNameLabel.Margin = new Thickness(20, 0);
        //        serviceNameLabel.Text = "File Name";

        //        Label timeoutLabel = new Label();
        //        timeoutLabel.Margin = new Thickness(20, 0);
        //        serviceNameLabel.Text = "Timeout";

        //        return new ViewCell
        //        {
        //            View = new StackLayout
        //            {
        //                Padding = new Thickness(20, 20),
        //                Orientation = StackOrientation.Horizontal,
        //                Children = { serviceNameLabel, fileNameLabel, timeoutLabel }
        //            }
        //        };
        //    });

        listView.Header = header;

        Content = new VerticalStackLayout
        {
            Children = { listView }
        };
    }
}