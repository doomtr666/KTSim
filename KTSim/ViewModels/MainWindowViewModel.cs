using System;
using Avalonia.Threading;
using KTSim.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace KTSim.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MatchTrainer Simulator => _simulator;

    [ObservableProperty]
    IOperativeAction? _lastAction;

    // simulation
    private MatchTrainer _simulator = new MatchTrainer();
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
        _actions.MoveNext();
        LastAction = _actions.Current;
    }
}
