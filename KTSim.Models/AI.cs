namespace KTSim.Models;

using TorchSharp;
using TorchSharp.Modules;

public class AINet : torch.nn.Module<torch.Tensor, torch.Tensor>
{
    public const int InputParameters = 9;
    public const int OutputParameters = 8;

    const int InputSize = InputParameters * 20;
    const int OutputSize = OutputParameters * 20;
    const int HidenSize1 = 1024;

    torch.nn.Module<torch.Tensor, torch.Tensor> _inputLayer;
    torch.nn.Module<torch.Tensor, torch.Tensor> _outputLayer;

    public AINet() : base(nameof(AINet))
    {
        _inputLayer = torch.nn.Linear(InputSize, HidenSize1);
        _outputLayer = torch.nn.Linear(HidenSize1, OutputSize);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        var t1 = _inputLayer.forward(input).relu();
        return _outputLayer.forward(t1);
    }
}

public class AITrainer
{
    public AINet Net { get; }

    const int MemorySize = 100000;
    const int BatchSize = 64;
    const float LearningRate = 0.001f;
    const float Gamma = 0.9f;
    const float MaxEpsilon = 0.9f;
    const float MinEpsilon = 0.01f;
    const float EpsilonDecay = -0.005f;

    List<(MatchState, IOperativeAction, MatchState, float)> _memory = [];

    torch.optim.Optimizer _optimizer;
    MSELoss _criterion;

    static int _espilonStep = 0;

    private MovingAverage _validActions = new MovingAverage(1000);

    public AITrainer(TeamSide side)
    {
        Net = new AINet();
        _optimizer = torch.optim.Adam(Net.parameters(), LearningRate);
        _criterion = torch.nn.MSELoss();
    }

    torch.Tensor StateToTensor(MatchState state)
    {
        var flatState = new float[state.OperativeStates.Length * AINet.InputParameters];
        for (int i = 0; i < state.OperativeStates.Length; i++)
        {
            var operativeState = state.OperativeStates[i];
            flatState[i * AINet.InputParameters] = operativeState.Position.X / KillZone.TotalWidth;
            flatState[i * AINet.InputParameters + 1] = operativeState.Position.Y / KillZone.TotalHeight;
            flatState[i * AINet.InputParameters + 2] = operativeState.PerformedActions.HasFlag(OperativeActionType.Move) ? 1 : 0;
            flatState[i * AINet.InputParameters + 3] = operativeState.PerformedActions.HasFlag(OperativeActionType.Dash) ? 1 : 0;
            flatState[i * AINet.InputParameters + 4] = operativeState.PerformedActions.HasFlag(OperativeActionType.Shoot) ? 1 : 0;
            flatState[i * AINet.InputParameters + 5] = operativeState.Status == OperativeStatus.Ready ? 1 : 0;
            flatState[i * AINet.InputParameters + 6] = operativeState.Status == OperativeStatus.Active ? 1 : 0;
            flatState[i * AINet.InputParameters + 7] = operativeState.Status == OperativeStatus.Activated ? 1 : 0;
            flatState[i * AINet.InputParameters + 8] = operativeState.Status == OperativeStatus.Neutralized ? 1 : 0;
        }
        return torch.tensor(flatState);
    }

    (float, IOperativeAction) TensorToActions(MatchState state, torch.Tensor tensor)
    {
        // extract all data form each operative
        var actions = new List<(float, IOperativeAction)>();

        var data = tensor.data<float>().ToArray();

        for (var i = 0; i < 20; i++)
        {

            var shootReward = data[i * AINet.OutputParameters];
            var shootAction = new OperativeShootAction(i, (int)(data[i * AINet.OutputParameters + 1] * 20));
            actions.Add((shootReward, shootAction));

            var moveReward = data[i * AINet.OutputParameters + 2];
            var moveAction = new OperativeMoveAction(i,
                new Position((int)(data[i * AINet.OutputParameters + 3] * KillZone.TotalWidth), (int)(data[i * AINet.OutputParameters + 4] * KillZone.TotalHeight)));
            actions.Add((moveReward, moveAction));

            var dashReward = data[i * AINet.OutputParameters + 5];
            var dashAction = new OperativeDashAction(i,
                new Position((int)(data[i * AINet.OutputParameters + 6] * KillZone.TotalWidth), (int)(data[i * AINet.OutputParameters + 7] * KillZone.TotalHeight)));
            actions.Add((dashReward, dashAction));
        }

        // sort by reward
        actions = actions.OrderByDescending(x => x.Item1).ToList();

        // take the first valid action
        foreach (var action in actions)
        {
            if (state.IsActionValid(action.Item2))
                return action;
        }

        // no valid action
        return (0, null!);
    }

    public IOperativeAction GetActions(MatchState state)
    {
        var stateTensor = StateToTensor(state);
        var pred = Net.forward(stateTensor);
        return TensorToActions(state, pred).Item2;
    }

    public IOperativeAction GenerateAction(MatchState matchState)
    {
        IOperativeAction action = null!;

        if (Random.Shared.NextSingle() > Epsilon())
        {
            action = GetActions(matchState);
            _validActions.Add(action == null ? 0 : 1);
        }

        if (action == null)
            action = matchState.GenerateAction();

        if (action == null)
            return null!;

        // remember the action
        var reward = ComputeReward(matchState, action);
        var nextState = matchState.Copy();
        nextState.ApplyAction(action);
        Remember(matchState, action, nextState, reward);

        return action;
    }

    float Epsilon()
    {
        var epsilon = MinEpsilon + (MaxEpsilon - MinEpsilon) * MathF.Exp(EpsilonDecay * _espilonStep);
        return epsilon;
    }

    public void Remember(MatchState state, IOperativeAction action, MatchState nextState, float reward)
    {
        if (_memory.Count >= MemorySize)
            _memory.RemoveAt(0);

        _memory.Add((state, action, nextState, reward));
    }

    void Train(MatchState state, IOperativeAction action, MatchState nextState, float reward)
    {
        var s1 = StateToTensor(state);
        var s2 = StateToTensor(nextState);

        var pred = Net.forward(s1);
        var target = pred.clone();
        var predNext = Net.forward(s2);

        var (maxNextReward, nextAction) = TensorToActions(nextState, predNext);

        if (nextAction == null)
            maxNextReward = -1;

        var qNew = reward + Gamma * maxNextReward;

        var data = target.data<float>().ToArray();

#if false
        // penalize invalid actions
        for (var i = 0; i < 20; i++)
        {
            var shootAction = new OperativeShootAction(i, (int)(data[i * AINet.OutputParameters + 1] * 20));
            if (!state.IsActionValid(shootAction))
                data[i * AINet.OutputParameters] = -10;
            else
                if ((float)data[i * AINet.OutputParameters] < 0) data[i * AINet.OutputParameters] = 0;

            var moveAction = new OperativeMoveAction(i,
                new Position((int)(data[i * AINet.OutputParameters + 3] * KillZone.TotalWidth), (int)(data[i * AINet.OutputParameters + 4] * KillZone.TotalHeight)));
            if (!state.IsActionValid(moveAction))
                data[i * AINet.OutputParameters + 2] = -10;
            else
                if ((float)data[i * AINet.OutputParameters + 2] < 0) data[i * AINet.OutputParameters + 2] = 0;

            var dashAction = new OperativeDashAction(i,
                new Position((int)(data[i * AINet.OutputParameters + 6] * KillZone.TotalWidth), (int)(data[i * AINet.OutputParameters + 7] * KillZone.TotalHeight)));
            if (!state.IsActionValid(dashAction))
                data[i * AINet.OutputParameters + 5] = -10;
            else
                if ((float)data[i * AINet.OutputParameters + 5] < 0) data[i * AINet.OutputParameters + 5] = 0;
        }
#endif

        // update the target
        switch (action)
        {
            case OperativeShootAction shoot:
                data[action.Operative * AINet.OutputParameters] = qNew;
                break;

            case OperativeMoveAction move:
                data[action.Operative * AINet.OutputParameters + 2] = qNew;
                break;

            case OperativeDashAction dash:
                data[action.Operative * AINet.OutputParameters + 5] = qNew;
                break;
        }

        var t = torch.tensor(data);

        _optimizer.zero_grad();
        var loss = _criterion.forward(t, pred);
        loss.backward();
        _optimizer.step();
    }


    public void TrainBatch()
    {
        if (_memory.Count < BatchSize)
            return;

        var samples = _memory.OrderBy(x => Random.Shared.Next()).Take(BatchSize);

        foreach (var sample in samples)
            Train(sample.Item1, sample.Item2, sample.Item3, sample.Item4);

        Console.WriteLine($"Epsilon: {Epsilon():P}, Memory Size: {_memory.Count}, Valid Action Ratio {_validActions.Average():P}");

        _espilonStep++;
    }

    float ComputeReward(MatchState state, IOperativeAction action)
    {
        // invalid action is penalized
        if (!state.IsActionValid(action))
            return -10;

        float reward = 0;

        var position = state.OperativeStates[action.Operative].Position;
        // var target = -1;

        switch (action)
        {
            case OperativeShootAction shoot:
                //target = shoot.Target;
                //reward += 5; // shooting is rewarded
                break;

            case OperativeMoveAction move:
                position = move.Destination;
                break;

            case OperativeDashAction dash:
                position = dash.Destination;
                break;
        }

        // shortest distance to objective
        var operative = state.OperativeStates[action.Operative];

        if (IsOnObjective(state, operative.Type.BaseDiameter, position))
            reward += 10;
#if false
        // get all enemies able to shoot
        var enemies = state.OperativeStates.Where(x => x.Side != operative.Side && x.Status == OperativeStatus.Ready && x.Index != target).ToArray();
        foreach (var enemy in enemies)
        {
            if (state.HasLineOfSight(enemy.Position, position))
            {
                reward -= 5;
                break;
            }
        }
#endif
        return reward;
    }

    bool IsOnObjective(MatchState state, float baseDiameter, Position position)
    {
        foreach (var objective in state.KillZone.Objectives)
        {
            if (Utils.Distance(position, objective.Position) < KillZone.CircleDistance + baseDiameter / 2)
                return true;
        }

        return false;
    }

    public void TrainLast()
    {
        if (_memory.Count == 0)
            return;

        var last = _memory.Last();
        Train(last.Item1, last.Item2, last.Item3, last.Item4);
    }
}