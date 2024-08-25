namespace KTSim.Models;

using TorchSharp;

/* Tensors structure
    * 
    * AIState
    *  - OperativeStates
    *      - PositionX
    *      - PositionY
    *      - Ready
    *      - Activated
    *      - Neutralized
    * 
    * AIAction
    *  - OperativeActions
    *      - Priority
    *      - ShootPriority
    *      - ShootX
    *      - ShootY
    *      - MovePriority
    *      - MoveX
    *      - MoveY
    *      - DashPriority
    *      - DashX
    *      - DashY
*/

public class AINet : torch.nn.Module<torch.Tensor, torch.Tensor>
{
    const int InputSize = 5 * 20;
    const int OutputSize = 10 * 10;
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
    public AINet Net { get; }

    public AITrainer(AINet net)
    {
        Net = net;
    }

    torch.Tensor StateToTensor(OperativeState[] state)
    {
        var flatState = new float[state.Length * 5];
        for (int i = 0; i < state.Length; i++)
        {
            flatState[i * 5] = state[i].Position.X / KillZone.TotalWidth;
            flatState[i * 5 + 1] = state[i].Position.Y / KillZone.TotalHeight;
            flatState[i * 5 + 2] = state[i].Status == OperativeStatus.Ready ? 1 : 0;
            flatState[i * 5 + 3] = state[i].Status == OperativeStatus.Activated ? 1 : 0;
            flatState[i * 5 + 4] = state[i].Status == OperativeStatus.Neutralized ? 1 : 0;
        }

        return torch.tensor(flatState);
    }

    torch.Tensor ActionToTensor(OperativeState[] state, IOperativeAction[] actions)
    {
        var flatAction = new float[10 * 10];

        float priority = 1.0f;
        foreach (var action in actions)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i != action.Operative)
                    continue;

                flatAction[i * 10] = 1;

                switch (action)
                {
                    case OperativeShootAction shoot:
                        flatAction[i * 10 + 1] = priority;
                        flatAction[i * 10 + 2] = state[shoot.Target].Position.X / KillZone.TotalWidth;
                        flatAction[i * 10 + 3] = state[shoot.Target].Position.Y / KillZone.TotalHeight;
                        break;

                    case OperativeMoveAction move:
                        flatAction[i * 10 + 4] = priority;
                        flatAction[i * 10 + 5] = move.Destination.X / KillZone.TotalWidth;
                        flatAction[i * 10 + 6] = move.Destination.Y / KillZone.TotalHeight;
                        break;

                    case OperativeDashAction dash:
                        flatAction[i * 10 + 7] = priority;
                        flatAction[i * 10 + 8] = dash.Destination.X / KillZone.TotalWidth;
                        flatAction[i * 10 + 9] = dash.Destination.Y / KillZone.TotalHeight;
                        break;
                }
            }

            priority -= 0.25f;
        }

        return torch.tensor(flatAction);
    }


    public void Train(MatchState state, IOperativeAction[] actions, MatchState nextState, float reward)
    {
#if false
        
        var stateTensor = StateToTensor(state);
        var actionTensor = ActionToTensor(state, action);
        var nextStateTensor = StateToTensor(nextState);

        var pred = Net.forward(stateTensor);

        var target = pred.clone();

#endif

    }

}