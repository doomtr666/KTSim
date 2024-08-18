namespace KTSim.Models;

public class KommandoBoyOperative : IOperativeType
{
    public string Name => "Kommando Boy";

    public float BaseDiameter => 32.0f;

    public float Movement => 3 * KillZone.CircleDistance;

    public int ActionPointLimit => 2;

    public int Defence => 3;

    public int Save => 5;

    public int Wounds => 10;

    public KommandoBoyOperative()
    {
    }
}