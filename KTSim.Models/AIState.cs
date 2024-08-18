namespace KTSim.Models;

public class AIState
{
    public AIGrid Grid { get; } = null!;
    public List<OperativeState> AgentState { get; init; } = null!;
}