using Microsoft.Extensions.Logging;

namespace KTSim.Models;


public enum Side
{
    Attacker,
    Defender,
}

public class MatchRunnerBase
{
    public KillZone KillZone { get; protected set; } = null!;
    public List<OperativeState> InitialOperativeStates { get; protected set; } = [];
    public List<OperativeState> CurrentOperativeStates { get; } = [];

    public MatchRunnerBase()
    {
    }

    public MatchRunnerBase(KillZone killZone, List<OperativeState> initialOperativeStates)
    {
        KillZone = killZone;
        InitialOperativeStates = initialOperativeStates;
    }

    public virtual void Reset()
    {
        CurrentOperativeStates.Clear();
        if (InitialOperativeStates == null)
            return;
        foreach (var operativeState in InitialOperativeStates)
            CurrentOperativeStates.Add(operativeState.Copy());
    }

    protected void ApplyAction(IOperativeAction action)
    {
        switch (action)
        {
            case OperativeMoveAction moveAction:
                CurrentOperativeStates[moveAction.Operative].Position = moveAction.Destination;
                break;

            case OperativeDashAction dashAction:
                CurrentOperativeStates[dashAction.Operative].Position = dashAction.Destination;
                break;

            case OperativeShootAction shootAction:
                CurrentOperativeStates[shootAction.Target].Status = OperativeStatus.Neutralized;
                break;

            default:
                throw new InvalidOperationException();
        }
    }

}

public class MatchPlayer : MatchRunnerBase
{
    Match _match;

    public MatchPlayer(Match match)
        : base(match.KillZone, match.InitialOperativeStates)
    {
        _match = match;
        Reset();
    }

    public IEnumerable<IOperativeAction> NextStep()
    {
        foreach (var action in _match.PlayedActions)
        {
            yield return action;
            ApplyAction(action);
        }
    }

}

public class MatchTrainer : MatchRunnerBase
{
    ILogger<MatchTrainer> _log;

    public uint TurningPoint { get; private set; }

    public Side SideTurn { get; private set; }

    public int GameCount { get; private set; } = 0;

    public MatchTrainer()
    {
        // logger
        using var factory = LoggerFactory.Create(builder => builder
            .AddFilter("KilZone", LogLevel.Debug)
            .AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            }));

        _log = factory.CreateLogger<MatchTrainer>();

        // create KillZone
        KillZone = new KillZone();

        // create Operatives
        var operatives = new List<OperativeState>();

        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, attacker, Side.Attacker, OperativeStatus.Ready, new Position(30 + 40 * i, 30));
            operatives.Add(operativeState);
        }

        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, defender, Side.Defender, OperativeStatus.Ready, new Position(KillZone.TotalWidth - 30 - 40 * i, KillZone.TotalHeight - 30));
            operatives.Add(operativeState);
        }

        InitialOperativeStates = operatives;

        Reset();
    }

    public override void Reset()
    {
        TurningPoint = 0;
        SideTurn = InitiativeRoll();

        base.Reset();

        _log.LogInformation($"Match {GameCount} started, {SideTurn} have the initiative");
    }

    public IEnumerable<IOperativeAction> NextStep()
    {
        while (true)
        {
            // end of turning point / game check
            var readyOperatives = CurrentOperativeStates.Where(a => a.Status == OperativeStatus.Ready).ToArray();
            if (readyOperatives.Length == 0)
            {
                TurningPoint++;
                SideTurn = InitiativeRoll();

                if (TurningPoint >= 4)
                {
                    _log.LogInformation($"Match {GameCount} ended");
                    GameCount++;
                    Reset();
                }
                else
                {
                    foreach (var operative in CurrentOperativeStates)
                    {
                        if (operative.Status != OperativeStatus.Neutralized)
                            operative.Status = OperativeStatus.Ready;
                    }

                    _log.LogInformation($"Turning Point {TurningPoint}, {SideTurn} have the initiative");
                }
            }

            // Get Next Action
            var actions = NextRandomAction();

            // Execute Action
            foreach (var action in actions)
            {
                yield return action;

                ApplyAction(action);
            }

            // Update Operative States
            if (actions.Count > 0)
                CurrentOperativeStates[actions[0].Operative].Status = OperativeStatus.Activated;

            // Next Side
            SideTurn = GetOppositeSide(SideTurn);
        }
    }

    public Side InitiativeRoll()
    {
        var values = Enum.GetValues(typeof(Side));
        var side = (Side)values.GetValue(Random.Shared.Next(values.Length))!;

        _log.LogInformation($"{side} have the initiative");

        return side;
    }

    public List<IOperativeAction> NextRandomAction()
    {
        var rand = Random.Shared;

        // randomly select a ready operative
        var readyOperatives = CurrentOperativeStates.Where(a => a.Status == OperativeStatus.Ready && a.Side == SideTurn).OrderBy(a => rand.Next()).ToArray();
        if (readyOperatives.Length == 0)
        {
            _log.LogInformation("No operative to activate");
            return [];
        }

        var operative = readyOperatives[0];
        var operativeIndex = operative.Index;

        // randomly select actions
        var possibleActions = Enum.GetValues<OperativeActionType>().OrderBy(a => rand.Next()).Take(operative.Type.ActionPointLimit).ToArray();

        var actions = new List<IOperativeAction>();

        var position = operative.Position;

        foreach (var possibleAction in possibleActions)
        {
            IOperativeAction selectedAction = null!;

            switch (possibleAction)
            {
                case OperativeActionType.Move:
                    selectedAction = GenerateRandomMoveAction(operative, ref position);
                    break;

                case OperativeActionType.Dash:
                    selectedAction = GenerateRandomDashAction(operative, ref position);
                    break;

                case OperativeActionType.Shoot:
                    selectedAction = GenerateRandomShootAction(operative, ref position);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            if (selectedAction != null)
                actions.Add(selectedAction);
        }

        _log.LogInformation($"Selected operative: {operative.Type.Name} ({operativeIndex})");
        foreach (var action in actions)
            _log.LogInformation($"Choosen Action: {action}");

        return actions;
    }

    Side GetOppositeSide(Side side)
    {
        return side switch
        {
            Side.Attacker => Side.Defender,
            Side.Defender => Side.Attacker,
            _ => throw new ArgumentException("Invalid side", nameof(side)),
        };
    }

    void GenerateRandomMovePosition(OperativeState operative, float maxDist, ref Position position)
    {
        const int maxTries = 100;
        int guard = 0;

        var destination = new Position();
        do
        {
            destination.X = position.X + (2 * (float)Random.Shared.NextDouble() - 1) * maxDist;
            destination.Y = position.Y + (2 * (float)Random.Shared.NextDouble() - 1) * maxDist;
            guard++;
        } while (!IsMoveValid(operative, position, destination, maxDist) && guard < maxTries);

        if (guard >= maxTries)
        {
            destination = operative.Position;
            _log.LogWarning("No valid move found");
        }

        position = destination;
    }

    bool IsMoveValid(OperativeState operative, Position source, Position destination, float maxDist)
    {
        // must be fully inside the killzone
        if (destination.X + (operative.Type.BaseDiameter / 2) >= KillZone.TotalWidth)
            return false;
        if (destination.Y + (operative.Type.BaseDiameter / 2) >= KillZone.TotalHeight)
            return false;
        if (destination.X - (operative.Type.BaseDiameter / 2) < 0)
            return false;
        if (destination.Y - (operative.Type.BaseDiameter / 2) < 0)
            return false;

        // no more than maxdist
        if (Utils.Distance(source, destination) > maxDist)
            return false;

        var operativeCircle = new Circle(destination, operative.Type.BaseDiameter / 2);

        // no collision with other operatives
        foreach (var other in CurrentOperativeStates)
        {
            if (other.Index == operative.Index)
                continue;
            if (other.Status == OperativeStatus.Neutralized)
                continue;

            if (Utils.Intersects(operativeCircle, new Circle(other.Position, other.Type.BaseDiameter / 2)))
                return false;
        }

        // no collision with terrains
        foreach (var terrain in KillZone.Terrains)
        {
            if (Utils.Intersects(operativeCircle, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        return true;
    }

    OperativeMoveAction GenerateRandomMoveAction(OperativeState operative, ref Position position)
    {
        GenerateRandomMovePosition(operative, operative.Type.Movement, ref position);

        return new OperativeMoveAction(operative.Index, position);
    }

    OperativeDashAction GenerateRandomDashAction(OperativeState operative, ref Position position)
    {
        GenerateRandomMovePosition(operative, KillZone.SquareDistance, ref position);

        return new OperativeDashAction(operative.Index, position);
    }

    OperativeShootAction GenerateRandomShootAction(OperativeState operative, ref Position position)
    {
        var enemySide = GetOppositeSide(operative.Side);

        var enemies = CurrentOperativeStates.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (IsTargetValid(position, enemy.Position))
                return new OperativeShootAction(operative.Index, enemy.Index);
        }

        _log.LogWarning("No valid target found");
        return null!;
    }

    bool IsTargetValid(Position source, Position target)
    {
        var segment = new Segment(source, target);

        // no collision with terrains
        foreach (var terrain in KillZone.Terrains)
        {
            //if (!terrain.Type.HasFlag(TerrainType.Heavy))
            //    continue;

            if (Utils.Intersects(segment, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        // TODO: no collision with other operatives
        return true;
    }

}
