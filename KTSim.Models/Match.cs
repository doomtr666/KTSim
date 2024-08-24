namespace KTSim.Models;

public class Match
{
    public KillZone KillZone { get; }
    public List<OperativeState> InitialOperativeStates { get; }
    public List<IOperativeAction> PlayedActions { get; }

    public int AttackerScore {get;}
    public int DefenderScore {get;}

    public Match(KillZone killZone, List<OperativeState> initialOperativeStates, List<IOperativeAction> playedActions, int attackerScore, int defenderScore)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
        PlayedActions = playedActions;
        AttackerScore = attackerScore;
        DefenderScore = defenderScore;
    }
}

