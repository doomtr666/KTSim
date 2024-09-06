namespace KTSim.Models;

public class Match
{
    public KillZone KillZone { get; }
    public OperativeState[] InitialOperativeStates { get; }
    public IOperativeAction[] PlayedActions { get; }
    public TeamSide[] InitiativeRolls { get; }
    public int AttackerScore { get; }
    public int DefenderScore { get; }

    public Match(KillZone killZone, OperativeState[] initialOperativeStates, IOperativeAction[] playedActions, TeamSide[] initiativeRolls, int attackerScore, int defenderScore)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
        PlayedActions = playedActions;
        InitiativeRolls = initiativeRolls;
        AttackerScore = attackerScore;
        DefenderScore = defenderScore;
    }
}
