namespace KTSim.Models;

public enum OperativeActionType
{
    Move,
    Dash,
    Shoot,
}

public interface IOperativeAction
{
    OperativeState Operative { get; }
}

public class OperativeMoveAction : IOperativeAction
{
    public OperativeState Operative { get; }

    public Position Destination { get; }

    public OperativeMoveAction(OperativeState operative, Position destination)
    {
        Operative = operative;
        Destination = destination;
    }

    public override string ToString()
    {
        return $"Move To ({Destination.X},{Destination.Y})";
    }
}

public class OperativeDashAction : IOperativeAction
{
    public OperativeState Operative { get; }
    public Position Destination { get; set; }

    public OperativeDashAction(OperativeState operative, Position destination)
    {
        Operative = operative;
        Destination = destination;
    }

    public override string ToString()
    {
        return $"Dash To ({Destination.X}{Destination.Y})";
    }
}

public class OperativeShootAction : IOperativeAction
{
    public OperativeState Operative { get; }
    public int TargetIndex { get; set; }

    public OperativeShootAction(OperativeState operative, int targetIndex)
    {
        Operative = operative;
        TargetIndex = targetIndex;
    }

    public override string ToString()
    {
        return $"Shoot At ({TargetIndex})";
    }
}
