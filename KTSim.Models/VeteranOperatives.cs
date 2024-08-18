namespace KTSim.Models;

public class VeteranTrooperOperative : IOperativeType
{
    public string Name => "Veteran Trooper";

    public float BaseDiameter => 25.0f;

    public float Movement => 3 * KillZone.CircleDistance;

    public int ActionPointLimit => 2;

    public int Defence => 3;

    public int Save => 5;

    public int Wounds => 7;

    public VeteranTrooperOperative()
    {
    }
}