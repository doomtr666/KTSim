namespace KTSim.Models;

using TorchSharp;
using TorchSharp.Modules;

/* Tensors structure
    * 
    * AIState
    *  - OperativeStates
    *      - 0 PositionX
    *      - 1 PositionY
    *      - 2 HasMoved
    *      - 3 HasDashed
    *      - 4 HasShot
    *      - 5 Ready
    *      - 6 Active
    *      - 7 Activated
    *      - 8 Neutralized
    * 
    * AIAction
    *  - OperativeActions
    *      - 0 ShootReward 
    *      - 1 Target
    *      - 2 MoveReward
    *      - 3 MoveX
    *      - 4 MoveY
    *      - 5 DashReward
    *      - 6 DashX
    *      - 7 DashY
*/

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
    private TeamSide Side { get; }

    public AINet Net { get; }

    const int MemorySize = 100000;
    const int BatchSize = 32;
    const float LearningRate = 0.001f;
    const float Gamma = 0.9f;
    const float MaxEpsilon = 0.9f;
    const float MinEpsilon = 0.01f;
    const float EpsilonDecay = -0.01f;

    List<(MatchState, IOperativeAction, MatchState, float)> _memory = [];

    torch.optim.Optimizer _optimizer;
    MSELoss _criterion;

    public AITrainer(TeamSide side)
    {
        Net = new AINet();
        Side = side;
        _optimizer = torch.optim.Adam(Net.parameters(), LearningRate);
        _criterion = torch.nn.MSELoss();
    }

    torch.Tensor StateToTensor(MatchState state)
    {
        var flatState = new float[state.OperativeStates.Length * AINet.InputParameters];
        for (int i = 0; i < state.OperativeStates.Length; i++)
        {
            flatState[i * AINet.InputParameters] = state.OperativeStates[i].Position.X / KillZone.TotalWidth;
            flatState[i * AINet.InputParameters + 1] = state.OperativeStates[i].Position.Y / KillZone.TotalHeight;
            flatState[i * AINet.InputParameters + 2] = state.OperativeStates[i].PerformedActions.HasFlag(OperativeActionType.Move) ? 1 : 0;
            flatState[i * AINet.InputParameters + 3] = state.OperativeStates[i].PerformedActions.HasFlag(OperativeActionType.Dash) ? 1 : 0;
            flatState[i * AINet.InputParameters + 4] = state.OperativeStates[i].PerformedActions.HasFlag(OperativeActionType.Shoot) ? 1 : 0;
            flatState[i * AINet.InputParameters + 5] = state.OperativeStates[i].Status == OperativeStatus.Ready ? 1 : 0;
            flatState[i * AINet.InputParameters + 6] = state.OperativeStates[i].Status == OperativeStatus.Active ? 1 : 0;
            flatState[i * AINet.InputParameters + 7] = state.OperativeStates[i].Status == OperativeStatus.Activated ? 1 : 0;
            flatState[i * AINet.InputParameters + 8] = state.OperativeStates[i].Status == OperativeStatus.Neutralized ? 1 : 0;
        }
        return torch.tensor(flatState);
    }

    (float, IOperativeAction) TensorToActions(MatchState state, torch.Tensor tensor)
    {
        // extract all data form each operative
        var actions = new List<(float, IOperativeAction)>();

        for (var i = 0; i < 20; i++)
        {

            var shootReward = (float)tensor[i * AINet.OutputParameters];
            var shootAction = new OperativeShootAction(i, (int)(tensor[i * AINet.OutputParameters + 1] * 20));
            actions.Add((shootReward, shootAction));

            var moveReward = (float)tensor[i * AINet.OutputParameters + 2];
            var moveAction = new OperativeMoveAction(i,
                new Position((int)(tensor[i * AINet.OutputParameters + 3] * KillZone.TotalWidth), (int)(tensor[i * AINet.OutputParameters + 4] * KillZone.TotalHeight)));
            actions.Add((moveReward, moveAction));

            var dashReward = (float)tensor[i * AINet.OutputParameters + 5];
            var dashAction = new OperativeDashAction(i,
                new Position((int)(tensor[i * AINet.OutputParameters + 6] * KillZone.TotalWidth), (int)(tensor[i * AINet.OutputParameters + 7] * KillZone.TotalHeight)));
            actions.Add((dashReward, dashAction));
        }

        // sort by reward
        actions = actions.OrderByDescending(x => x.Item1).ToList();

        // take the first valid action
        foreach (var action in actions)
        {
            if (state.IsActionValid(action.Item2))
                return (action.Item1, action.Item2);
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
            action = GetActions(matchState);

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

    static int _step = 0;

    float Epsilon()
    {
        var epsilon = MinEpsilon + (MaxEpsilon - MinEpsilon) * MathF.Exp(EpsilonDecay * _step);
        _step++;
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

        if (nextState.CurrentTurn != Side)
            maxNextReward = -maxNextReward;

        if (nextAction == null)
            maxNextReward = -10;

        var qNew = reward + Gamma * maxNextReward;

#if false
        // penalize invalid actions
        for (var i = 0; i < 20; i++)
        {
            var shootAction = new OperativeShootAction(i, (int)(target[i * AINet.OutputParameters + 1] * 20));
            if (!state.IsActionValid(shootAction))
                target[i * AINet.OutputParameters] = -10;

            var moveAction = new OperativeMoveAction(i,
                new Position((int)(target[i * AINet.OutputParameters + 3] * KillZone.TotalWidth), (int)(target[i * AINet.OutputParameters + 4] * KillZone.TotalHeight)));
            if (!state.IsActionValid(moveAction))
                target[i * AINet.OutputParameters] = -10;

            var dashAction = new OperativeDashAction(i,
                new Position((int)(target[i * AINet.OutputParameters + 6] * KillZone.TotalWidth), (int)(target[i * AINet.OutputParameters + 7] * KillZone.TotalHeight)));
            if (!state.IsActionValid(dashAction))
                target[i * AINet.OutputParameters] = -10;
        }
#endif

        // update the target
        switch (action)
        {
            case OperativeShootAction shoot:
                target[action.Operative * AINet.OutputParameters] = qNew;
                break;

            case OperativeMoveAction move:
                target[action.Operative * AINet.OutputParameters + 2] = qNew;
                break;

            case OperativeDashAction dash:
                target[action.Operative * AINet.OutputParameters + 5] = qNew;
                break;
        }

        target[action.Operative * AINet.OutputParameters] = qNew;

        _optimizer.zero_grad();
        var loss = _criterion.forward(target, pred);
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
    }

    float ComputeReward(MatchState state, IOperativeAction action)
    {
        // invalid action is penalized
        if (!state.IsActionValid(action))
            return -10;

        switch (action)
        {
            case OperativeShootAction shoot:
                return 2; // shooting is rewarded

            case OperativeMoveAction move: // reward if destination is on objective
                return IsOnObjective(state, state.OperativeStates[move.Operative].Type.BaseDiameter, move.Destination) ? 5 : 0;

            case OperativeDashAction dash: // reward if destination is on objective
                return IsOnObjective(state, state.OperativeStates[dash.Operative].Type.BaseDiameter, dash.Destination) ? 5 : 0;

            default: // should not happen
                return 0;
        }
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