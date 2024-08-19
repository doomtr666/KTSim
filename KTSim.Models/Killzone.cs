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
    public OperativeState[] Agents { get { return _agents.ToArray(); } }

    private List<DropZone> _dropZones = [];
    private List<Objective> _objectives = [];
    private List<ITerrain> _terrains = [];
    private List<OperativeState> _agents = [];
    private List<OperativeStatus> _agentStates = [];

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

        _agents.Clear();
        // add attackers
        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var agentState = new OperativeState(attacker, Side.Attacker, OperativeStatus.Ready, new Position(30 + 40 * i, 30));
            _agents.Add(agentState);
        }

        // add defenders
        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var agentState = new OperativeState(defender, Side.Defender, OperativeStatus.Ready, new Position(TotalWidth - 30 - 40 * i, TotalHeight - 30));
            _agents.Add(agentState);
        }

        TurningPoint = 0;
        SideTurn = InitiativeRoll();

        _log.LogInformation($"Match {GameCount} started, {SideTurn} have the initiative");
    }

    public void NextStep()
    {
        // turning point check
        var readyAgents = _agents.Where(a => a.Status == OperativeStatus.Ready).ToArray();
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
                foreach (var agent in _agents)
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
        var readyAgents = _agents.Where(a => a.Status == OperativeStatus.Ready && a.Side == SideTurn).OrderBy(a => rand.Next()).ToArray();
        if (readyAgents.Length == 0)
            return null;

        var agent = readyAgents[0];
        var agentIndex = _agents.IndexOf(agent);

        _log.LogInformation($"Selected agent: {agent.Type.Name} ({agentIndex})");


        // randomly select actions
        var possibleActions = Enum.GetValues<OperativeActionType>();
        var actions = possibleActions.OrderBy(a => rand.Next()).Take(2).ToArray();

        for (var i = 0; i < actions.Length; i++)
        {
            _log.LogInformation($"Action {i}: {actions[i]}");
        }

        return new AIAction(agent, []);
    }

    public Side GetOppositeSide(Side side)
    {
        return side switch
        {
            Side.Attacker => Side.Defender,
            Side.Defender => Side.Attacker,
            _ => throw new ArgumentException("Invalid side", nameof(side)),
        };
    }

    public OperativeMoveAction? GenerateRandomMoveAction(OperativeState agent)
    {
        return new OperativeMoveAction
        {
            MoveX = (float)Random.Shared.NextDouble(),
            MoveY = (float)Random.Shared.NextDouble()
        };
    }

    public OperativeSprintAction? GenerateRandomSprintAction(OperativeState agent)
    {
        return new OperativeSprintAction
        {
            MoveX = (float)Random.Shared.NextDouble(),
            MoveY = (float)Random.Shared.NextDouble()
        };
    }

    public OperativeShootAction? GenerateRandomShootAction(OperativeState agent)
    {
        var enemies = GetOppositeSide(SideTurn);

        var enemyAgents = _agents.Where(a => a.Side == enemies && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();
        if (enemyAgents.Length == 0)
            return null;

        return new OperativeShootAction
        {
            TargetIndex = _agents.IndexOf(enemyAgents[0])
        };
    }

}
