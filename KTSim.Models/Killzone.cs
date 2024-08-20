using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace KTSim.Models;

public class KillZone
{
    ILogger<KillZone> _log;

    // size in inches
    public const int GridWidth = 30;
    public const int GridHeight = 22;

    // metric size
    public const float GridStep = 25.4f;
    public const float TotalWidth = GridWidth * GridStep;
    public const float TotalHeight = GridHeight * GridStep;

    public const float CenterX = TotalWidth / 2;
    public const float CenterY = TotalHeight / 2;

    // KT distances
    public const float TriangleDistance = 1.0f * GridStep;
    public const float CircleDistance = 2.0f * GridStep;
    public const float SquareDistance = 3.0f * GridStep;
    public const float PentagonDistance = 6.0f * GridStep;

    public DropZone[] DropZones { get { return _dropZones.ToArray(); } }
    public Objective[] Objectives { get { return _objectives.ToArray(); } }
    public ITerrain[] Terrains { get { return _terrains.ToArray(); } }
    public OperativeState[] Operatives { get { return _operatives.ToArray(); } }
    public Segment[] Lines { get { return _lines.ToArray(); } }

    private List<DropZone> _dropZones = [];
    private List<Objective> _objectives = [];
    private List<ITerrain> _terrains = [];
    private List<OperativeState> _operatives = [];

    private List<Segment> _lines = [];

    public uint TurningPoint { get; set; }

    public Side SideTurn { get; set; }

    public int GameCount { get; set; } = 0;

    public KillZone()
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

        _log = factory.CreateLogger<KillZone>();

        Reset();
    }

    public void Reset()
    {
        _lines.Clear();

        // add drop zones
        _dropZones.Clear();
        _dropZones.Add(new DropZone(new Position(0, 0), TotalWidth, SquareDistance, Side.Attacker));
        _dropZones.Add(new DropZone(new Position(0, TotalHeight - SquareDistance), TotalWidth, SquareDistance, Side.Defender));

        // add objectives
        _objectives.Clear();
        _objectives.Add(new Objective(new Position(CenterX + PentagonDistance, CenterY + CircleDistance)));
        _objectives.Add(new Objective(new Position(CenterX - PentagonDistance, CenterY - CircleDistance)));
        _objectives.Add(new Objective(new Position(CenterX, PentagonDistance)));
        _objectives.Add(new Objective(new Position(CenterX, TotalHeight - PentagonDistance)));
        _objectives.Add(new Objective(new Position(SquareDistance, CenterY + CircleDistance)));
        _objectives.Add(new Objective(new Position(TotalWidth - SquareDistance, CenterY - CircleDistance)));

        // add terrains
        _terrains.Clear();
        _terrains.Add(new Terrain(TerrainType.Heavy, new Position(CenterX, CenterY), 0, 3 * CircleDistance, 2 * CircleDistance));
        _terrains.Add(new Terrain(TerrainType.Heavy, new Position(SquareDistance + TriangleDistance, PentagonDistance), 0, 2 * CircleDistance, 2 * CircleDistance));
        _terrains.Add(new Terrain(TerrainType.Heavy, new Position(TotalWidth - TriangleDistance - SquareDistance, TotalHeight - PentagonDistance), 0, 2 * CircleDistance, 2 * CircleDistance));
        _terrains.Add(new Terrain(TerrainType.Heavy, new Position(TotalWidth - TriangleDistance - SquareDistance, PentagonDistance - TriangleDistance), 0, 2 * CircleDistance, CircleDistance));
        _terrains.Add(new Terrain(TerrainType.Heavy, new Position(SquareDistance + TriangleDistance, TotalHeight - PentagonDistance + TriangleDistance), 0, 2 * CircleDistance, CircleDistance));
        _terrains.Add(new Terrain(TerrainType.Light | TerrainType.Traversable, new Position(CenterX + PentagonDistance, CenterY - 4.5f * GridStep), 0, TriangleDistance, PentagonDistance));
        _terrains.Add(new Terrain(TerrainType.Light | TerrainType.Traversable, new Position(CenterX - PentagonDistance, CenterY + 4.5f * GridStep), 0, TriangleDistance, PentagonDistance));
        _terrains.Add(new Terrain(TerrainType.Light | TerrainType.Traversable, new Position(CenterX - 5 * GridStep, CenterY - 5f * GridStep), 0, 4 * GridStep, TriangleDistance));
        _terrains.Add(new Terrain(TerrainType.Light | TerrainType.Traversable, new Position(CenterX + 5 * GridStep, CenterY + 5f * GridStep), 0, 4 * GridStep, TriangleDistance));

        _operatives.Clear();
        // add attackers
        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var agentState = new OperativeState(attacker, Side.Attacker, OperativeStatus.Ready, new Position(30 + 40 * i, 30));
            _operatives.Add(agentState);
        }

        // add defenders
        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var agentState = new OperativeState(defender, Side.Defender, OperativeStatus.Ready, new Position(TotalWidth - 30 - 40 * i, TotalHeight - 30));
            _operatives.Add(agentState);
        }

        TurningPoint = 0;
        SideTurn = InitiativeRoll();

        _log.LogInformation($"Match {GameCount} started, {SideTurn} have the initiative");
    }

    public void NextStep()
    {
        // turning point check
        var readyAgents = _operatives.Where(a => a.Status == OperativeStatus.Ready).ToArray();
        if (readyAgents.Length == 0)
        {
            TurningPoint++;
            SideTurn = InitiativeRoll();

            if (TurningPoint >= 4)
            {
                GameCount++;
                Reset();

            }
            else
            {
                foreach (var agent in _operatives)
                {
                    if (agent.Status != OperativeStatus.Neutralized)
                        agent.Status = OperativeStatus.Ready;
                }

                _log.LogInformation($"Turning Point {TurningPoint}, {SideTurn} have the initiative");
            }
        }

        // Get Next Action
        var action = NextRandomAction();

        if (action != null)
        {
            // Execute Action
            foreach (var operativeAction in action.Actions)
            {
                switch (operativeAction)
                {
                    case OperativeMoveAction moveAction:
                        action.Operative.Position = moveAction.Destination;
                        break;

                    case OperativeDashAction dashAction:
                        action.Operative.Position = dashAction.Destination;
                        break;

                    case OperativeShootAction shootAction:
                        var target = _operatives[shootAction.TargetIndex];
                        target.Status = OperativeStatus.Neutralized;
                        _lines.Add(new Segment(action.Operative.Position, target.Position));
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            // Update Agent States
            action.Operative.Status = OperativeStatus.Activated;
        }

        // Next Side
        SideTurn = GetOppositeSide(SideTurn);
    }

    public Side InitiativeRoll()
    {
        var values = Enum.GetValues(typeof(Side));
        var side = (Side)values.GetValue(Random.Shared.Next(values.Length))!;

        _log.LogInformation($"{side} have the initiative");

        return side;
    }

    public AIAction? NextRandomAction()
    {
        var rand = Random.Shared;

        // randomly select a ready agent
        var readyOperatives = _operatives.Where(a => a.Status == OperativeStatus.Ready && a.Side == SideTurn).OrderBy(a => rand.Next()).ToArray();
        if (readyOperatives.Length == 0)
        {
            _log.LogInformation("No agent to activate");
            return null;
        }

        var operative = readyOperatives[0];
        var operativeIndex = _operatives.IndexOf(operative);

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

        var choosenAction = new AIAction(operative, actions);

        _log.LogInformation($"Selected agent: {operative.Type.Name} ({operativeIndex})");
        foreach (var action in actions)
            _log.LogInformation($"Choosen Action: {action}");

        return choosenAction;
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
        if (destination.X + (operative.Type.BaseDiameter / 2) >= TotalWidth)
            return false;
        if (destination.Y + (operative.Type.BaseDiameter / 2) >= TotalHeight)
            return false;
        if (destination.X - (operative.Type.BaseDiameter / 2) < 0)
            return false;
        if (destination.Y - (operative.Type.BaseDiameter / 2) < 0)
            return false;

        // no more than maxdist
        if (Utils.Distance(source, destination) > maxDist)
            return false;

        var operativeCircle = new Circle(destination, operative.Type.BaseDiameter / 2);

        // no collision with other agents
        foreach (var other in _operatives)
        {
            if (other == operative)
                continue;
            if (other.Status == OperativeStatus.Neutralized)
                continue;

            if (Utils.Intersects(operativeCircle, new Circle(other.Position, other.Type.BaseDiameter / 2)))
                return false;
        }

        // no collision with terrains
        foreach (var terrain in Terrains)
        {
            if (Utils.Intersects(operativeCircle, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        return true;
    }

    OperativeMoveAction GenerateRandomMoveAction(OperativeState operative, ref Position position)
    {
        GenerateRandomMovePosition(operative, operative.Type.Movement, ref position);

        return new OperativeMoveAction
        {
            Destination = position
        };
    }

    OperativeDashAction GenerateRandomDashAction(OperativeState agent, ref Position position)
    {
        GenerateRandomMovePosition(agent, SquareDistance, ref position);

        return new OperativeDashAction
        {
            Destination = position
        };
    }

    OperativeShootAction GenerateRandomShootAction(OperativeState operative, ref Position position)
    {
        var enemySide = GetOppositeSide(operative.Side);

        var enemies = _operatives.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (IsTargetValid(position, enemy.Position))
                return new OperativeShootAction { TargetIndex = _operatives.IndexOf(enemy) };
        }

        _log.LogWarning("No valid target found");
        return null!;
    }

    bool IsTargetValid(Position source, Position target)
    {
        var segment = new Segment(source, target);

        // no collision with terrains
        foreach (var terrain in Terrains)
        {
            //if (!terrain.Type.HasFlag(TerrainType.Heavy))
            //    continue;

            if (Utils.Intersects(segment, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        // TODO: no collision with other agents

        return true;
    }

}
