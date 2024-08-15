using CommunityToolkit.Mvvm.ComponentModel;

namespace KTSim.Gui.ViewModels;

public partial class Rectangle : ViewModelBase, IShape
{
    [ObservableProperty]
    float _x;

    [ObservableProperty]
    float _y;

    [ObservableProperty]
    float _width;

    [ObservableProperty]
    float _height;

    [ObservableProperty]
    string _strokeColor = "";

    [ObservableProperty]
    string _fillColor = "";
}
