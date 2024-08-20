namespace KTSim.Models;

public enum OperativeStatus
{
    Ready,
    Activated,
    Neutralized,
}

public class OperativeState
{
    public IOperativeType Type { get; }
    public Side Side { get; }
    public OperativeStatus Status { get; set; }
    public Position Position { get; set; }

    public OperativeState(IOperativeType type, Side side, OperativeStatus state, Position position)
    {
        Type = type;
        Side = side;
        Status = state;
        Position = position;
    }

    public OperativeState Copy()
    {
        return new OperativeState(Type, Side, Status, Position);
    }
}
