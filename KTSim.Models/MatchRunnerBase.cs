namespace KTSim.Models;

public class MatchRunnerBase
{
    public KillZone KillZone { get; protected set; } = null!;
    public List<OperativeState> InitialOperativeStates { get; protected set; } = [];
    public List<OperativeState> CurrentOperativeStates { get; } = [];

    public MatchRunnerBase()
    {
    }

    public MatchRunnerBase(KillZone killZone, List<OperativeState> initialOperativeStates)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
    }

    public virtual void Reset()
    {
        CurrentOperativeStates.Clear();
        if (InitialOperativeStates == null)
            return;
        foreach (var operativeState in InitialOperativeStates)
            CurrentOperativeStates.Add(operativeState.Copy());
    }

    protected void ApplyAction(IOperativeAction action)
    {
        switch (action)
        {
            case OperativeMoveAction moveAction:
                CurrentOperativeStates[moveAction.Operative].Position = moveAction.Destination;
                break;

            case OperativeDashAction dashAction:
                CurrentOperativeStates[dashAction.Operative].Position = dashAction.Destination;
                break;

            case OperativeShootAction shootAction:
                CurrentOperativeStates[shootAction.Target].Status = OperativeStatus.Neutralized;
                break;

            default:
                throw new InvalidOperationException();
        }
    }

}
