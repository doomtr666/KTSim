namespace KTSim.Models;

public class VeteranTrooper : IAgent
{
    public string Name => "Veteran Trooper";

    public float BaseDiameter => 2.5f;

    public Position Position { get; set; }

    public VeteranTrooper(Position position)
    {
        Position = position;
    }
}