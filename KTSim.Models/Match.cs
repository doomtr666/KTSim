namespace KTSim.Models;

public class Match
{
    public KillZone KillZone { get; }
    public List<OperativeState> InitialOperativeStates { get; }
    public List<IOperativeAction> PlayedActions { get; }

    public Match(KillZone killZone, List<OperativeState> initialOperativeStates, List<IOperativeAction> playedActions)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
        PlayedActions = playedActions;
    }
}

