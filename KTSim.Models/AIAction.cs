namespace KTSim.Models;

public enum OperativeActionType
{
    Move,
    Dash,
    Shoot,
}

public interface IOperativeAction
{
    OperativeActionType Type { get; }
}

public class OperativeMoveAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Move;

    public Position Destination { get; set; }

    public override string ToString()
    {
        return $"Move To ({Destination.X},{Destination.Y})";
    }
}

public class OperativeDashAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Dash;
    public Position Destination { get; set; }

    public override string ToString()
    {
        return $"Dash To ({Destination.X}{Destination.Y})";
    }
}

public class OperativeShootAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Shoot;
    public int TargetIndex { get; set; }

    public override string ToString()
    {
        return $"Shoot At ({TargetIndex})";
    }
}

public class AIAction
{
    public OperativeState Operative { get; set; }

    public List<IOperativeAction> Actions { get; set; }

    public AIAction(OperativeState operative, List<IOperativeAction> actions)
    {
        Operative = operative;
        Actions = actions;
    }
}