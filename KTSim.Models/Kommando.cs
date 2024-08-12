namespace KTSim.Models;

public class KommandoBoy : IAgent
{
    public string Name => "Kommando Boy";

    public float BaseDiameter => 3.2f;

    public Position Position { get; set; }

    public KommandoBoy(Position position)
    {
        Position = position;
    }
}