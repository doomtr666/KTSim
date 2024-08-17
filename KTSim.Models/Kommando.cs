namespace KTSim.Models;

public class KommandoBoy : IAgent
{
    public string Name => "Kommando Boy";

    public float BaseDiameter => 32.0f;

    public float Movement => 6.0f;

    public Position Position { get; set; }

    public Side Side { get; }
    public OrderType Order { get; set; }
    public AgentState State { get; set; }

    public KommandoBoy(Position position, Side side)
    {
        Position = position;
        Side = side;
        Order = OrderType.Engage;
        State = AgentState.Ready;
    }
}