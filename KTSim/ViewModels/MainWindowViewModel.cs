using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using KTSim.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace KTSim.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    ObservableCollection<IShape> _items = [];

    private Simulator _simulator = new Simulator();
    private IEnumerator<IOperativeAction> _actions;

    private DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, (s, e) => NextStep());
        _timer.Start();

        _actions = _simulator.NextStep().GetEnumerator();

        //Render();
    }

    void NextStep()
    {
        Render();

        //_items.Clear();

        _actions.MoveNext();
        var action = _actions.Current;

        if (action is OperativeMoveAction moveAction)
        {
            Items.Add(new Line(moveAction.Operative.Position.X, moveAction.Operative.Position.Y, moveAction.Destination.X, moveAction.Destination.Y, "Blue"));
        }

        if (action is OperativeDashAction dashAction)
        {
            Items.Add(new Line(dashAction.Operative.Position.X, dashAction.Operative.Position.Y, dashAction.Destination.X, dashAction.Destination.Y, "Green"));
        }

        //_simulation.NextStep();
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

        // drop zones
        foreach (var dropZone in _simulator.KillZone.DropZones)
        {
            var color = dropZone.Side == Side.Attacker ? "Pink" : "LightBlue";
            Items.Add(CreateRectangle(dropZone.Position.X, dropZone.Position.Y, dropZone.Width, dropZone.Height, color, color));
        }

        // terrains
        foreach (var terrain in _simulator.KillZone.Terrains)
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

        // objectives
        foreach (var objective in _simulator.KillZone.Objectives)
            Items.Add(CreateCircle(objective.Position.X, objective.Position.Y, Objective.Radius, "Black", "Orange"));

        // operatives
        foreach (var operative in _simulator.Operatives)
        {
            var fillColor = operative.Side == Side.Attacker ? "Red" : "Blue";
            var strokeColor = "White";

            if (operative.Status == OperativeStatus.Neutralized)
            {
                fillColor = "Gray";
                strokeColor = "Black";
            }

            if (operative.Status == OperativeStatus.Activated)
            {
                strokeColor = "Black";
            }

            Items.Add(CreateCircle(operative.Position.X, operative.Position.Y, operative.Type.BaseDiameter / 2, strokeColor, fillColor));
        }
    }
}
