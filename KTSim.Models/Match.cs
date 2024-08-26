namespace KTSim.Models;

public class Match
{
    public KillZone KillZone { get; }
    public OperativeState[] InitialOperativeStates { get; }
    public IOperativeAction[] PlayedActions { get; }

    public int AttackerScore {get;}
    public int DefenderScore {get;}

    public Match(KillZone killZone, OperativeState[] initialOperativeStates, IOperativeAction[] playedActions, int attackerScore, int defenderScore)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
        PlayedActions = playedActions;
        AttackerScore = attackerScore;
        DefenderScore = defenderScore;
    }
}

