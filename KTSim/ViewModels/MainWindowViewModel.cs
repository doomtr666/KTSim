using System.Collections.Generic;

namespace KTSim.Gui.ViewModels;

public interface IShape
{
    public int X { get; }

    public int Y { get; }
}

public class Circle : IShape
{
    public int X { get; init; }

    public int Y { get; init; }

    public float Radius { get; init; }

    public string Color { get; init; } = "";
}

public class Rectangle : IShape
{
    public int X { get; init; }

    public int Y { get; init; }

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

    //private KillZone _killZone = new KillZone();

    public MainWindowViewModel()
    {
        Items.Add(new Circle { X = 10, Y = 10, Radius = 50.0f, Color = "Red" });
        Items.Add(new Circle { X = 60, Y = 10, Radius = 50.0f, Color = "Blue" });
        Items.Add(new Rectangle { X = 120, Y = 10, Width = 50.0f, Height = 25.0f, Color = "Green" });
    }
}
