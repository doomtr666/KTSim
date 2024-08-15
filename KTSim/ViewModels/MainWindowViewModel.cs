using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using KTSim.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KTSim.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    ObservableCollection<IShape> _items = [];

    private KillZone _killZone = new KillZone();

    private DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Normal, (s, e) => NextStep());
        _timer.Start();

        Render();
    }

    void NextStep()
    {
        _killZone.NextStep();
        Render();
    }

    Circle CreateCircle(float x, float y, float radius, string StrokeColor, string FillColor = "")
    {
        return new Circle { X = x - radius, Y = y - radius, Width = 2 * radius, StrokeColor = StrokeColor, FillColor = string.IsNullOrEmpty(FillColor) ? "Transparent" : FillColor };
    }

    Rectangle CreateRectangle(float x, float y, float width, float height, string StrokeColor, string FillColor = "")
    {
        return new Rectangle { X = x, Y = y, Width = width, Height = height, StrokeColor = StrokeColor, FillColor = string.IsNullOrEmpty(FillColor) ? "Transparent" : FillColor };
    }

    Rectangle CreateCenteredRectangle(float x, float y, float width, float height, string StrokeColor, string FillColor = "")
    {
        return new Rectangle { X = x - width / 2, Y = y - height / 2, Width = width, Height = height, StrokeColor = StrokeColor, FillColor = string.IsNullOrEmpty(FillColor) ? "Transparent" : FillColor };
    }

    void Render()
    {
        Items.Clear();

        foreach (var dropZone in _killZone.DropZones)
        {
            var color = dropZone.Side == Side.Attacker ? "Pink" : "LightBlue";
            Items.Add(CreateRectangle(dropZone.Position.X, dropZone.Position.Y, dropZone.Width, dropZone.Height, color, color));
        }

        foreach (var objective in _killZone.Objectives)
            Items.Add(CreateCircle((int)objective.Position.X, (int)objective.Position.Y, Objective.Radius, "Black", "Orange"));

        foreach (var terrain in _killZone.Terrains)
        {
            var StrokeColor = "Black";
            if (terrain.Type.HasFlag(TerrainType.Traversable))
                StrokeColor = "Gray";

            var fillColor = "";
            if (terrain.Type.HasFlag(TerrainType.Heavy))
                fillColor = "DarkGray";
            else if (terrain.Type.HasFlag(TerrainType.Light))
                fillColor = "LightGray";

            Items.Add(CreateCenteredRectangle(terrain.Position.X, terrain.Position.Y, terrain.Width, terrain.Height, StrokeColor, fillColor));
        }

        foreach (var agent in _killZone.Agents)
            Items.Add(CreateCircle((int)agent.Position.X, (int)agent.Position.Y, agent.BaseDiameter / 2, "Black", agent.Side == Side.Attacker ? "Red" : "Blue"));
    }
}
