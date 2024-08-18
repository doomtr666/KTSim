namespace KTSim.Models;

public enum OperativeActionType
{
    Move,
    Sprint,
    Shoot,
}

public interface IOperativeAction
{
    OperativeActionType Type { get; }
}

public class OperativeMoveAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Move;
    public float MoveX { get; set; }
    public float MoveY { get; set; }
}

public class OperativeSprintAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Sprint;
    public float MoveX { get; set; }
    public float MoveY { get; set; }
}

public class OperativeShootAction : IOperativeAction
{
    public OperativeActionType Type => OperativeActionType.Shoot;
    public int TargetIndex { get; set; }
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