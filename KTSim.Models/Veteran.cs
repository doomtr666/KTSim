namespace KTSim.Models;

public class VeteranTrooper : IAgent
{
    public string Name => "Veteran Trooper";

    public float BaseDiameter => 25.0f;

    public float Movement => 6.0f;

    public Position Position { get; set; }

    public Side Side { get; }
    public OrderType Order { get; set; }
    public AgentState State { get; set; }

    public VeteranTrooper(Position position, Side side)
    {
        Position = position;
        Side = side;
    }
}