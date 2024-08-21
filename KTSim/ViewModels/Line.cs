using System;

namespace KTSim.Gui.ViewModels;

public partial class Line : ViewModelBase, IShape
{
    float _x1;
    float _y1;
    float _x2;
    float _y2;

    public float X => _x1;
    public float Y => _y1;

    public string StrokeColor { get; }

    public string Data { get; }

    public Line(float x1, float y1, float x2, float y2, string strokeColor = "Black")
    {
        _x1 = Math.Min(x1, x2);
        _y1 = Math.Min(y1, y2);
        _x2 = Math.Max(x1, x2);
        _y2 = Math.Max(y1, y2);

        StrokeColor = strokeColor;

        Data = FormattableString.Invariant($"M0,0L{_x2 - _x1},{_y2 - y1}");
    }
}
