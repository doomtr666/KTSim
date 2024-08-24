namespace KTSim.Models;

public enum OperativeStatus
{
    Ready,
    Activated,
    Neutralized,
}

public class OperativeState
{
    public int Index { get; }
    public IOperativeType Type { get; }
    public TurnSide Side { get; }
    public OperativeStatus Status { get; set; }
    public Position Position { get; set; }

    public OperativeState(int index, IOperativeType type, TurnSide side, OperativeStatus state, Position position)
    {
        Index = index;
        Type = type;
        Side = side;
        Status = state;
        Position = position;
    }

    public OperativeState Copy()
    {
        return new OperativeState(Index, Type, Side, Status, Position);
    }
}
