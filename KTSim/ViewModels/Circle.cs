using CommunityToolkit.Mvvm.ComponentModel;

namespace KTSim.Gui.ViewModels;

public partial class Circle : ViewModelBase, IShape
{
    [ObservableProperty]
    float _x;

    [ObservableProperty]
    float _y;

    [ObservableProperty]
    float _width;

    [ObservableProperty]
    string _strokeColor = "";

    [ObservableProperty]
    string _fillColor = "";
}
