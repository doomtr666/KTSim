namespace KTSim.Models;

public enum Side
{
    Attacker,
    Defender,
}

public class DropZone
{
    public Position Position { get; }
    public float Width { get; }
    public float Height { get; }

    public Side Side { get; }

    public DropZone(Position position, float width, float height, Side side)
    {
        Position = position;
        Width = width;
        Height = height;
        Side = side;
    }
}
