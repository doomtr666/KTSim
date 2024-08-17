namespace KTSim.Models;

public class AIAction
{
    public int AgentIndex { get; set; }
    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public int AttackIndex { get; set; }

    public AIAction()
    {
    }
}