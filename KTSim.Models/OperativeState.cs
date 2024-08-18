using TorchSharp.Modules;

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
    public OperativeStatus Status { get; }
    public Position Position { get; }
    public OperativeState(IOperativeType type, Side side, OperativeStatus state, Position position)
    {
        Type = type;
        Side = side;
        Status = state;
        Position = position;
    }
}
