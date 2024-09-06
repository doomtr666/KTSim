namespace KTSim.Models;

using System.ComponentModel;
using TorchSharp;

/* Tensors structure
    * 
    * AIState
    *  - OperativeStates
    *      - 0 PositionX
    *      - 1 PositionY
    *      - 2 Ready
    *      - 3 Activated
    *      - 4 Neutralized
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
    public const int InputParameters = 5;
    public const int OutputParameters = 8;

    const int InputSize = InputParameters * 20;
    const int OutputSize = OutputParameters * 10;
    const int HidenSize = 1024;

    torch.nn.Module<torch.Tensor, torch.Tensor> _inputLayer;
    torch.nn.Module<torch.Tensor, torch.Tensor> _outputLayer;

    public AINet() : base(nameof(AINet))
    {
        _inputLayer = torch.nn.Linear(InputSize, HidenSize);
        _outputLayer = torch.nn.Linear(HidenSize, OutputSize);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return _outputLayer.forward(_inputLayer.forward(input).relu());
    }
}


public class AITrainer
{
    private TeamSide Side { get; }

    public AINet Net { get; }

    const int MemorySize = 10000;
    const int BatchSize = 32;

    List<(MatchState, IOperativeAction[], MatchState, float)> _memory = [];

    public AITrainer(AINet net, TeamSide side)
    {
        Net = net;
        Side = side;
    }

    torch.Tensor StateToTensor(MatchState state)
    {
        var flatState = new float[state.OperativeStates.Length * AINet.InputParameters];
        for (int i = 0; i < state.OperativeStates.Length; i++)
        {
            flatState[i * AINet.InputParameters] = state.OperativeStates[i].Position.X / KillZone.TotalWidth;
            flatState[i * AINet.InputParameters + 1] = state.OperativeStates[i].Position.Y / KillZone.TotalHeight;
            flatState[i * AINet.InputParameters + 2] = state.OperativeStates[i].Status == OperativeStatus.Ready ? 1 : 0;
            flatState[i * AINet.InputParameters + 3] = state.OperativeStates[i].Status == OperativeStatus.Activated ? 1 : 0;
            flatState[i * AINet.InputParameters + 4] = state.OperativeStates[i].Status == OperativeStatus.Neutralized ? 1 : 0;
        }
        return torch.tensor(flatState);
    }

    torch.Tensor ActionToTensor(MatchState state, IOperativeAction[] actions)
    {
        var offset = Side == TeamSide.Attacker ? 0 : 10;
        var flatAction = new float[AINet.OutputParameters * 10];

        foreach (var action in actions)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i + offset != action.Operative)
                    continue;

                switch (action)
                {
                    case OperativeShootAction shoot:
                        flatAction[i * AINet.OutputParameters + 0] = ComputeReward(state, shoot);
                        flatAction[i * AINet.OutputParameters + 1] = shoot.Target / 20.0f;
                        break;

                    case OperativeMoveAction move:
                        flatAction[i * AINet.OutputParameters + 2] = ComputeReward(state, move);
                        flatAction[i * AINet.OutputParameters + 3] = move.Destination.X / KillZone.TotalWidth;
                        flatAction[i * AINet.OutputParameters + 4] = move.Destination.Y / KillZone.TotalHeight;
                        break;

                    case OperativeDashAction dash:
                        flatAction[i * AINet.OutputParameters + 5] = ComputeReward(state, dash);
                        flatAction[i * AINet.OutputParameters + 6] = dash.Destination.X / KillZone.TotalWidth;
                        flatAction[i * AINet.OutputParameters + 7] = dash.Destination.Y / KillZone.TotalHeight;
                        break;
                }
            }

        }

        return torch.tensor(flatAction);
    }

    IOperativeAction[] TensorToActions(MatchState state, torch.Tensor tensor)
    {
        var offset = Side == TeamSide.Attacker ? 0 : 10;

        // extract all data form each operative
        var tensorActions = new List<(float, (float, IOperativeAction)[])>();

        for (var i = 0; i < 10; i++)
        {

            var shootReward = (float)tensor[i * AINet.OutputParameters];
            var shootAction = new OperativeShootAction(i + offset, (int)(tensor[i * AINet.OutputParameters + 1] * 20));

            var moveReward = (float)tensor[i * AINet.OutputParameters + 2];
            var moveAction = new OperativeMoveAction(i + offset,
                new Position((int)(tensor[i * AINet.OutputParameters + 3] * KillZone.TotalWidth), (int)(tensor[i * AINet.OutputParameters + 4] * KillZone.TotalHeight)));

            var dashReward = (float)tensor[i * AINet.OutputParameters + 5];
            var dashAction = new OperativeDashAction(i + offset,
                new Position((int)(tensor[i * AINet.OutputParameters + 6] * KillZone.TotalWidth), (int)(tensor[i * AINet.OutputParameters + 7] * KillZone.TotalHeight)));

            var totalReward = shootReward + moveReward + dashReward;

            tensorActions.Add(
            (totalReward, new (float, IOperativeAction)[]{
                (shootReward, shootAction),
                (moveReward, moveAction),
                (dashReward, dashAction)
            }));
        }

        var bestOperativeActions = tensorActions.OrderByDescending(x => x.Item1).First().Item2;

        return bestOperativeActions.OrderByDescending(x => x.Item1).Select(x => x.Item2).ToArray();
    }

    public IOperativeAction[] GetActions(MatchState state)
    {
        var stateTensor = StateToTensor(state);
        var pred = Net.forward(stateTensor);
        return TensorToActions(state, pred);
    }

    public void Remember(MatchState state, IOperativeAction[] actions, MatchState nextState, float reward)
    {
        if (_memory.Count > MemorySize)
            _memory.RemoveAt(0);

        _memory.Add((state, actions, nextState, reward));
    }

    void Train(MatchState state, IOperativeAction[] actions, MatchState nextState, float reward)
    {

        var stateTensor = StateToTensor(state);
        var actionTensor = ActionToTensor(state, actions);
        var nextStateTensor = StateToTensor(nextState);

        var pred = Net.forward(stateTensor);

        var target = pred.clone();
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
            {
                return true;
            }
        }

        return false;
    }
}