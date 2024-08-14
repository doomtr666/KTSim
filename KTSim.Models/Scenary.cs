namespace KTSim.Models;

[Flags]
public enum TerrainType
{
    Insignificant = 0,
    Light = 1 << 0,
    Heavy = 1 << 1,
    Traversable = 1 << 2,
    VantagePoint = 1 << 3,
    Barricade = Light | Traversable,
}

public interface ITerrain
{
    public TerrainType Type { get; }

    public Position Position { get; }
    public float Rotation { get; }
    public float Width { get; }
    public float Height { get; }
}

public class Barricade : ITerrain
{
    public TerrainType Type => TerrainType.Barricade;

    public Position Position { get; }
    public float Rotation { get; }
    public float Width => KillZone.CircleDistance;
    public float Height => 5f;

    public Barricade(Position position, float rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

public class Terrain : ITerrain
{
    public TerrainType Type { get; }

    public Position Position { get; }
    public float Rotation { get; }
    public float Width { get; }
    public float Height { get; }

    public Terrain(TerrainType type, Position position, float rotation, float width, float height)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
        Width = width;
        Height = height;
    }
}