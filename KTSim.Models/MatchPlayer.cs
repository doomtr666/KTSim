using Microsoft.Extensions.Logging;

namespace KTSim.Models;

public class MatchPlayer
{
    Match _match;

    MatchState _matchState;

    public bool IsFinished => _matchState.IsFinished;

    public KillZone KillZone => _match.KillZone;
    public OperativeState[] CurrentOperativeStates => _matchState.OperativeStates;

    private int _actionIndex;

    private IOperativeAction? _previousAction;

    public MatchPlayer(Match match)
    {
        _match = match;
        _matchState = new MatchState(match.KillZone, match.InitialOperativeStates, match.InitiativeRolls);
        _actionIndex = 0;
    }

    public IOperativeAction? NextStep()
    {
        if (IsFinished)
            return null;

        var action = _match.PlayedActions[_actionIndex];
        if (_previousAction != null)
        {
            Logger.Instance.LogDebug($"Previous action: {_previousAction}");
            _matchState.ApplyAction(_previousAction);
        }
        _previousAction = action;
        _actionIndex++;

        return action;
    }

}
