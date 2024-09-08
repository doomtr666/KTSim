using Microsoft.Extensions.Logging;

namespace KTSim.Models;

public class MatchPlayer
{
    Match _match;

    MatchState _matchState;

    public bool IsFinished => _matchState.IsFinished;

    public KillZone KillZone => _match.KillZone;
    public OperativeState[] CurrentOperativeStates => _matchState.OperativeStates;
    public int AttackerScore => _matchState.AttackerScore;
    public int DefenderScore => _matchState.DefenderScore;

    private int _actionIndex;
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

        var action = _match.PlayedActions[_actionIndex / 2];
        if (_actionIndex % 2 == 1)
        {
            _matchState.ApplyAction(action);
            action = null;
        }
        _actionIndex++;

        return action;
    }

}
