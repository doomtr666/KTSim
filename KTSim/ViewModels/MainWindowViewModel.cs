using System;
using Avalonia.Threading;
using KTSim.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KTSim.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    MatchPlayer? _player = null!;

    [ObservableProperty]
    IOperativeAction? _lastAction;

    // simulation
    private MatchTrainer _trainer = new MatchTrainer();

    private DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(800), DispatcherPriority.Normal, (s, e) => NextStep());
        _timer.Start();
    }

    void NextStep()
    {
        if (Player == null || Player.IsFinished)
        {
            var match = _trainer.GenerateMatch();
            Player = new MatchPlayer(match);
        }

        LastAction = Player.NextStep();
    }
}
