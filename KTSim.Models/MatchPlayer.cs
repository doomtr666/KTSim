namespace KTSim.Models;

public class MatchPlayer : MatchRunnerBase
{

    Match _match;

    public bool IsFinished { get; private set; }

    private int _actionIndex;

    public MatchPlayer(Match match)
        : base(match.KillZone, match.InitialOperativeStates)
    {
        _match = match;
        _actionIndex = 0;
        IsFinished = false;

        Reset();
    }

    public override void Reset()
    {
        base.Reset();
        _actionIndex = -1;
        IsFinished = false;
    }

    public IOperativeAction? NextStep()
    {
        if (_actionIndex >= _match.PlayedActions.Count - 1)
        {
            IsFinished = true;
            return null;
        }

        if (_actionIndex > 0)
        {
            var previousAction = _match.PlayedActions[_actionIndex];
            ApplyAction(previousAction);
            CurrentOperativeStates[previousAction.Operative].Status = OperativeStatus.Activated;
        }


        if (CurrentOperativeStates.Where(x => x.Status == OperativeStatus.Ready).Count() == 0)
        {
            foreach (var operativeState in CurrentOperativeStates)
            {
                if (operativeState.Status != OperativeStatus.Neutralized)
                    operativeState.Status = OperativeStatus.Ready;
            }
        }


        var action = _match.PlayedActions[_actionIndex + 1];
        _actionIndex++;
        return action;
    }

}
