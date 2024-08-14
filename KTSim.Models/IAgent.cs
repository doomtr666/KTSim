namespace KTSim.Models;

public enum OrderType
{
    Conceal,
    Engage,
}

public interface IAgent
{
    string Name { get; }
    float BaseDiameter { get; }
    Position Position { get; }
    float Movement { get; }
    Side Side { get; }
}