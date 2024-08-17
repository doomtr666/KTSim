namespace KTSim.Models;

public class AIModel
{
    const int BatchSize = 128;

    private AIMemory _memory = new AIMemory();

    AIModel()
    {
    }

    public void TrainShort(AIState state, AIAction action, float reward, AIState nextState)
    {

    }

    public void TrainLong()
    {
        var samples = _memory.Sample(BatchSize);
        foreach (var sample in samples)
            TrainShort(sample.State, sample.Action, sample.Reward, sample.NextState);
    }
}

