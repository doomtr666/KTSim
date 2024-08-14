namespace KTSim.Models;

public class KillZone
{
    // size in inches
    public const int OfficialGridWidth = 30;
    public const int OfficialGridHeight = 22;

    // metric size
    public const float GridSquareSize = 25.4f;
    public const float TotalWidth = OfficialGridWidth * GridSquareSize;
    public const float TotalHeight = OfficialGridHeight * GridSquareSize;

    public const float CenterX = TotalWidth / 2;
    public const float CenterY = TotalHeight / 2;

    // oversampling factor
    public const int OverSamplingFactor = 2;

    // total oversampled grid size
    public const int GridWidth = OfficialGridWidth * OverSamplingFactor;
    public const int GridHeight = OfficialGridHeight * OverSamplingFactor;

    // KT distances
    public const float TriangleDistance = 1.0f * GridSquareSize;
    public const float CircleDistance = 2.0f * GridSquareSize;
    public const float SquareDistance = 3.0f * GridSquareSize;
    public const float PentagonDistance = 6.0f * GridSquareSize;

    public DropZone[] DropZones { get { return _dropZones.ToArray(); } }
    public Objective[] Objectives { get { return _objectives.ToArray(); } }
    public ITerrain[] Terrains { get { return _terrains.ToArray(); } }
    public IAgent[] Agents { get { return _agents.ToArray(); } }

    private List<DropZone> _dropZones = [];
    private List<Objective> _objectives = [];
    private List<ITerrain> _terrains = [];
    private List<IAgent> _agents = [];

    public KillZone()
    {
        // add drop zones
        _dropZones.Add(new DropZone(new Position(0, 0), TotalWidth, SquareDistance, Side.Attacker));
        _dropZones.Add(new DropZone(new Position(0, TotalHeight - SquareDistance), TotalWidth, SquareDistance, Side.Defender));

        // add objectives
        _objectives.Add(new Objective(new Position(CenterX + PentagonDistance, CenterY + CircleDistance)));
        _objectives.Add(new Objective(new Position(CenterX - PentagonDistance, CenterY - CircleDistance)));
        _objectives.Add(new Objective(new Position(CenterX, PentagonDistance)));
        _objectives.Add(new Objective(new Position(CenterX, TotalHeight - PentagonDistance)));
        _objectives.Add(new Objective(new Position(SquareDistance, CenterY + CircleDistance)));
        _objectives.Add(new Objective(new Position(TotalWidth - SquareDistance, CenterY - CircleDistance)));

        // add attackers
        for (var i = 0; i < 10; i++)
            _agents.Add(new KommandoBoy(new Position(30 + 40 * i, 30), Side.Attacker));

        // add defenders
        for (var i = 0; i < 10; i++)
            _agents.Add(new VeteranTrooper(new Position(TotalWidth - 30 - 40 * i, TotalHeight - 30), Side.Defender));
    }
}
