using Microsoft.Maui.Controls;
using Morph.Daemon.Client;

namespace Morph.Manager;

public class ServicesGrid : ContentPage
{
    public List<DaemonService> morphServices = new(new DaemonService[] {
            new DaemonService() { ServiceName = "abc", AccessLocal = true , AccessRemote = false},
            new DaemonService() { ServiceName = "123", AccessLocal = true , AccessRemote = false } });

    private Grid grid;

    public ServicesGrid()
    {
        grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(30) },
                new RowDefinition { Height = new GridLength(30) },
                new RowDefinition { Height = new GridLength(30) }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition{ Width=new GridLength(200, GridUnitType.Auto)},
                new ColumnDefinition{ Width=new GridLength(100)},
                new ColumnDefinition{ Width=new GridLength(100)}
            }
        };
        Content = grid;

        PopulateGrid();
    }

    private void PopulateGrid()
    {
        int row = 0;

        void AddLabel(int col, string text, bool isHeader)
        {
            Label label = new Label
            {
                Text = text,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(5),
            };
            if (isHeader)
            {
                label.FontAttributes = FontAttributes.Bold;
                //label.Scale = 1.1;
            }
            grid.Add(label, col, row);
        }

        void AddRow(string text0, string text1, string text2, bool isHeader)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            AddLabel(0, text0, isHeader);
            AddLabel(1, text1, isHeader);
            AddLabel(2, text2, isHeader);
            row++;
        }

        grid.Clear();
        AddRow("Service Name", "Local Access", "Remote Access", true);
        foreach (var service in morphServices)
            AddRow(service.ServiceName, service.AccessLocal.ToString(), service.AccessRemote.ToString(), false);
    }
}