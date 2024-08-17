namespace KTSim.Models;

public class AIMemoryItem
{
    public AIState State { get; init; } = null!;
    public AIAction Action { get; init; } = null!;
    public float Reward { get; init; } = 0.0f;
    public AIState NextState { get; init; } = null!;
}

public class AIMemory
{
    const int MaxMemory = 1000;

    private List<AIMemoryItem> _memory = new List<AIMemoryItem>();

    public AIMemory()
    {
    }

    public void Remember(AIState state, AIAction action, float reward, AIState nextState)
    {
        if (_memory.Count >= MaxMemory)
        {
            _memory.RemoveAt(0);
        }

        var item = new AIMemoryItem
        {
            State = state,
            Action = action,
            Reward = reward,
            NextState = nextState
        };

        _memory.Add(item);
    }

    public List<AIMemoryItem> Sample(int batchSize)
    {
        return _memory.OrderBy(x => Random.Shared.Next()).Take(batchSize).ToList();
    }
}

