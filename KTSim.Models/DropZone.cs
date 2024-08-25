namespace KTSim.Models;

public class DropZone
{
    public Position Position { get; }
    public float Width { get; }
    public float Height { get; }

    public TeamSide Side { get; }

    public DropZone(Position position, float width, float height, TeamSide side)
    {
        Position = position;
        Width = width;
        Height = height;
        Side = side;
    }
}
