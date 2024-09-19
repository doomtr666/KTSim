using System;
using Avalonia.Threading;
using KTSim.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace KTSim.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    MatchPlayer? _player = null!;

    [ObservableProperty]
    IOperativeAction? _lastAction;

    private List<Match> _matches = [];
    private Mutex _mutex = new Mutex();

    // simulation
    private MatchTrainer _trainer = new MatchTrainer();

    private DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Normal, (s, e) => NextStep());
        _timer.Start();

        _matches.Add(_trainer.GenerateMatch());

        Task.Run(() =>
        {
            while (true)
            {
                var match = _trainer.GenerateMatch();

                _mutex.WaitOne();
                if (_matches.Count > 100)
                {
                    _matches.RemoveAt(0);
                }
                _matches.Add(match);
                _mutex.ReleaseMutex();
            }
        });
    }

    void NextStep()
    {
        if (Player == null || Player.IsFinished)
        {
            _mutex.WaitOne();
            var match = _matches.Last();
            _mutex.ReleaseMutex();

            Player = new MatchPlayer(match);
        }

        LastAction = Player.NextStep();

    }
}
