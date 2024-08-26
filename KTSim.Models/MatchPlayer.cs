namespace KTSim.Models;

public class MatchPlayer
{

    Match _match;

    MatchState _matchState;

    public bool IsFinished { get; private set; }

    public KillZone KillZone => _match.KillZone;
    public OperativeState[] CurrentOperativeStates => _matchState.OperativeStates.ToArray();

    private int _actionIndex;

    public MatchPlayer(Match match)
    {
        _match = match;
        _matchState = new MatchState(match.KillZone, match.InitialOperativeStates.ToArray());
        _actionIndex = 0;
        IsFinished = false;
    }

    public IOperativeAction? NextStep()
    {
        if (_actionIndex >= _match.PlayedActions.Length - 1)
        {
            IsFinished = true;
            return null;
        }

        if (_matchState.TurningPointFinished())
            _matchState.ReadyOperatives();

        if (_actionIndex > 0)
        {
            var previousAction = _match.PlayedActions[_actionIndex];
            _matchState.ApplyAction(previousAction);
            _matchState.OperativeStates[previousAction.Operative].Status = OperativeStatus.Activated;
        }

        var action = _match.PlayedActions[_actionIndex + 1];
        _actionIndex++;
        return action;
    }

}
