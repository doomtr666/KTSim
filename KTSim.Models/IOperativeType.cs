namespace KTSim.Models;

public interface IOperativeType
{
    string Name { get; }
    float BaseDiameter { get; }
    float Movement { get; }
    int ActionPointLimit { get; }
    int Defence { get; }
    int Save { get; }
    int Wounds { get; }
}

