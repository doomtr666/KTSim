namespace KTSim.Models;

public enum OrderType
{
    Conceal,
    Engage,
}

public enum AgentState
{
    Ready,
    Activated,
}

public interface IAgent
{
    string Name { get; }
    float BaseDiameter { get; }
    float Movement { get; }

    Side Side { get; }

    OrderType Order { get; set; }
    AgentState State { get; set; }
    Position Position { get; set; }
}

