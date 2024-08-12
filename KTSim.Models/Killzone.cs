namespace KTSim.Models;

public class KillZone
{
    // size in inches
    public const int OfficialGridWidth = 30;
    public const int OfficialGridHeight = 22;

    // metric size
    public const float GridSquareSize = 2.54f;
    public const float TotalWidth = OfficialGridWidth * GridSquareSize;
    public const float TotalHeight = OfficialGridHeight * GridSquareSize;

    // oversampling factor
    public const int OverSamplingFactor = 2;

    // total oversampled grid size
    public const int GridWidth = OfficialGridWidth * OverSamplingFactor;
    public const int GridHeight = OfficialGridHeight * OverSamplingFactor;

    public IAgent[] Attackers { get { return _attackers.ToArray(); } }
    public IAgent[] Defenders { get { return _attackers.ToArray(); } }

    private List<IAgent> _attackers = [];
    private List<IAgent> _defenders = [];

    public KillZone()
    {
        for (var i = 0; i < 10; i++)
        {
            _attackers.Add(new KommandoBoy(new Position(3 + 4 * i, 3)));
        }

        for (var i = 0; i < 10; i++)
        {
            _defenders.Add(new VeteranTrooper(new Position(3 + 4 * i, OfficialGridHeight - 3)));
        }
    }
}
