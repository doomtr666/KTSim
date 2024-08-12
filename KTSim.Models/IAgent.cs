namespace KTSim.Models;

public interface IAgent
{
    string Name { get; }
    float BaseDiameter { get; }
    Position Position { get; }
}