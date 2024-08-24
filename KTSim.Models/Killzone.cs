namespace KTSim.Models;

public class KillZone
{
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

    private List<DropZone> _dropZones = [];
    private List<Objective> _objectives = [];
    private List<ITerrain> _terrains = [];

    public KillZone()
    {
        Reset();
    }

    public void Reset()
    {
        // add drop zones
        _dropZones.Clear();
        _dropZones.Add(new DropZone(new Position(0, 0), TotalWidth, SquareDistance, TurnSide.Attacker));
        _dropZones.Add(new DropZone(new Position(0, TotalHeight - SquareDistance), TotalWidth, SquareDistance, TurnSide.Defender));

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

    }


}
