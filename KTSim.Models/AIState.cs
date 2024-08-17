namespace KTSim.Models;

public class AIAgentState
{

}

public class AIState
{
    public AIGrid Grid { get; } = null!;
    public List<AIAgentState> AgentState { get; init; } = null!;
}