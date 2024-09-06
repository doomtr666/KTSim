namespace KTSim.Models;

[Flags]
public enum OperativeActionType
{
    Move = 1 << 0,
    Dash = 1 << 1,
    Shoot = 1 << 2,
}

public interface IOperativeAction
{
    int Operative { get; }
}

public class OperativeMoveAction : IOperativeAction
{
    public int Operative { get; }

    public Position Destination { get; }

    public OperativeMoveAction(int operative, Position destination)
    {
        Operative = operative;
        Destination = destination;
    }

    public override string ToString()
    {
        return $"{Operative} Move To ({Destination.X};{Destination.Y})";
    }
}

public class OperativeDashAction : IOperativeAction
{
    public int Operative { get; }
    public Position Destination { get; set; }

    public OperativeDashAction(int operative, Position destination)
    {
        Operative = operative;
        Destination = destination;
    }

    public override string ToString()
    {
        return $"{Operative} Dash To ({Destination.X};{Destination.Y})";
    }
}

public class OperativeShootAction : IOperativeAction
{
    public int Operative { get; }
    public int Target { get; set; }

    public OperativeShootAction(int operative, int target)
    {
        Operative = operative;
        Target = target;
    }

    public override string ToString()
    {
        return $"{Operative} Shoot At {Target}";
    }
}
