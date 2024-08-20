using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KTSim.Gui.ViewModels;

public partial class Line : ViewModelBase, IShape
{
    [ObservableProperty]
    string _data = "";

    [ObservableProperty]
    string _strokeColor = "";

    [ObservableProperty]
    float _startX;

    [ObservableProperty]
    float _startY;

    [ObservableProperty]
    float _endX;

    [ObservableProperty]
    float _endY;

    public float X => StartX;

    public float Y => StartY;

    public Line(float startX, float startY, float endX, float endY, string strokeColor)
    {
        if (startX > endX)
        {
            var temp = startX;
            startX = endX;
            endX = temp;
        }

        if (startY > endY)
        {
            var temp = startY;
            startY = endY;
            endY = temp;
        }

        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        StrokeColor = strokeColor;

        Data = FormattableString.Invariant($"M0,0L{EndX - StartX},{EndY - StartY}"); 
    }
}