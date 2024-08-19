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

    private List<DropZone> _dropZones = [];
    private List<Objective> _objectives = [];
    private List<ITerrain> _terrains = [];
    private List<OperativeState> _operatives = [];

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
        var readyAgents = _operatives.Where(a => a.Status == OperativeStatus.Ready && a.Side == SideTurn).OrderBy(a => rand.Next()).ToArray();
        if (readyAgents.Length == 0)
        {
            _log.LogInformation("No agent to activate");
            return null;
        }

        var agent = readyAgents[0];
        var agentIndex = _operatives.IndexOf(agent);

        // randomly select actions
        var possibleActions = Enum.GetValues<OperativeActionType>().OrderBy(a => rand.Next()).Take(agent.Type.ActionPointLimit).ToArray();

        var actions = new List<IOperativeAction>();

        foreach (var possibleAction in possibleActions)
        {
            IOperativeAction selectedAction = null!;

            switch (possibleAction)
            {
                case OperativeActionType.Move:
                    selectedAction = GenerateRandomMoveAction(agent);
                    break;

                case OperativeActionType.Dash:
                    selectedAction = GenerateRandomSprintAction(agent);
                    break;

                case OperativeActionType.Shoot:
                    selectedAction = GenerateRandomShootAction(agent);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            if (selectedAction != null)
                actions.Add(selectedAction);
        }

        var choosenAction = new AIAction(agent, actions);

        _log.LogInformation($"Selected agent: {agent.Type.Name} ({agentIndex})");
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

    OperativeMoveAction GenerateRandomMoveAction(OperativeState agent)
    {
        var destination = new Position();
        do
        {
            destination.X = agent.Position.X + (2 * (float)Random.Shared.NextDouble() - 1) * agent.Type.Movement;
            destination.Y = agent.Position.Y + (2 * (float)Random.Shared.NextDouble() - 1) * agent.Type.Movement;
        } while (!IsMoveValid(agent, destination, agent.Type.Movement));

        return new OperativeMoveAction
        {
            Destination = destination
        };
    }

    OperativeDashAction GenerateRandomSprintAction(OperativeState agent)
    {
        var destination = new Position();
        do
        {
            destination.X = agent.Position.X + (2 * (float)Random.Shared.NextDouble() - 1) * SquareDistance;
            destination.Y = agent.Position.Y + (2 * (float)Random.Shared.NextDouble() - 1) * SquareDistance;
        } while (!IsMoveValid(agent, destination, SquareDistance));

        return new OperativeDashAction
        {
            Destination = destination
        };
    }

    bool IsMoveValid(OperativeState agent, Position destination, float maxDist)
    {
        // must be fully inside the killzone
        if (destination.X + agent.Type.BaseDiameter / 2 > TotalWidth)
            return false;
        if (destination.Y + agent.Type.BaseDiameter / 2 > TotalWidth)
            return false;
        if (destination.X - agent.Type.BaseDiameter / 2 < 0)
            return false;
        if (destination.Y - agent.Type.BaseDiameter / 2 < 0)
            return false;

        // no more than maxdist
        var dist = MathF.Sqrt((destination.X - agent.Position.X) * (destination.X - agent.Position.X) + (destination.Y - agent.Position.Y) * (destination.Y - agent.Position.Y));
        if (dist > maxDist)
            return false;

        // no collision with terrains
        var agentCircle = new Circle(agent.Position, agent.Type.BaseDiameter / 2);
        foreach (var terrain in Terrains)
        {
            if (Utils.Intersects(agentCircle, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        // no collision with other agents
        foreach (var other in _operatives)
        {
            if (other == agent)
                continue;

            if (Utils.Intersects(agentCircle, new Circle(other.Position, other.Type.BaseDiameter / 2)))
                return false;
        }

        return true;
    }

    OperativeShootAction GenerateRandomShootAction(OperativeState agent)
    {
        var enemySide = GetOppositeSide(SideTurn);

        var enemies = _operatives.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (IsTargetValid(agent, enemy))
                return new OperativeShootAction { TargetIndex = _operatives.IndexOf(enemy) };
        }

        return null!;
    }

    bool IsTargetValid(OperativeState agent, OperativeState target)
    {
        // TODO: handle line of sight
        return true;
    }

}
