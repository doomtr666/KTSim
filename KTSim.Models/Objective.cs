namespace KTSim.Models;

public class Objective
{
    public Position Position { get; set; }

    public const float Radius = KillZone.CircleDistance;

    public Objective(Position position)
    {
        Position = position;
    }
}