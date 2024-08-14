using System.Collections.Generic;
using KTSim.Models;

namespace KTSim.Gui.ViewModels;

public interface IShape
{
    public float X { get; }

    public float Y { get; }
}

public class Circle : IShape
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public string Color { get; init; } = "";
}

public class Rectangle : IShape
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public string Color { get; init; } = "";
}

public partial class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static

    public List<IShape> Items { get; } = [];

    private KillZone _killZone = new KillZone();

    public MainWindowViewModel()
    {
        foreach (var dropZone in _killZone.DropZones)
            Items.Add(new Rectangle { X = dropZone.Position.X, Y = dropZone.Position.Y, Width = dropZone.Width, Height = dropZone.Height, Color = dropZone.Side == Side.Attacker ? "Pink" : "LightBlue" });

        foreach (var objective in _killZone.Objectives)
            Items.Add(CreateCircle((int)objective.Position.X, (int)objective.Position.Y, Objective.Radius, "Orange"));

        foreach (var agent in _killZone.Agents)
            Items.Add(CreateCircle((int)agent.Position.X, (int)agent.Position.Y, agent.BaseDiameter / 2, agent.Side == Side.Attacker ? "Red" : "Blue"));

    }

    Circle CreateCircle(int x, int y, float radius, string color)
    {
        return new Circle { X = x - radius, Y = y - radius, Width = 2 * radius, Color = color };
    }
}
