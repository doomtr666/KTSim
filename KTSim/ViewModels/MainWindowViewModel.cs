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

    public string StrokeColor { get; init; } = "";
    public string FillColor { get; init; } = "";
}

public class Rectangle : IShape
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }
    public string StrokeColor { get; init; } = "";
    public string FillColor { get; init; } = "";
}

public partial class MainWindowViewModel : ViewModelBase
{
    public List<IShape> Items { get; } = [];

    private KillZone _killZone = new KillZone();

    public MainWindowViewModel()
    {
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
}
