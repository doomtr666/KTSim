namespace KTSim.Models;

public enum OperativeStatus
{
    Ready,
    Activated,
    Neutralized,
}

public class OperativeState
{
    public int Index { get; }
    public IOperativeType Type { get; }
    public TeamSide Side { get; }
    public OperativeStatus Status { get; set; }
    public Position Position { get; set; }

    public OperativeState(int index, IOperativeType type, TeamSide side, OperativeStatus state, Position position)
    {
        Index = index;
        Type = type;
        Side = side;
        Status = state;
        Position = position;
    }

    public OperativeState Copy()
    {
        return new OperativeState(Index, Type, Side, Status, Position);
    }
}

public class MatchState
{
    public KillZone KillZone { get; }

    public OperativeState[] OperativeStates { get; }

    public MatchState(KillZone killZone, OperativeState[] operatives)
    {
        KillZone = killZone;
        OperativeStates = operatives;
    }

    public MatchState Copy()
    {
        OperativeState[] operatives = new OperativeState[OperativeStates.Length];
        for (int i = 0; i < OperativeStates.Length; i++)
            operatives[i] = OperativeStates[i].Copy();
        return new MatchState(KillZone, operatives);
    }

    public MatchState ApplyAction(IOperativeAction action)
    {
        switch (action)
        {
            case OperativeMoveAction moveAction:
                OperativeStates[moveAction.Operative].Position = moveAction.Destination;
                break;

            case OperativeDashAction dashAction:
                OperativeStates[dashAction.Operative].Position = dashAction.Destination;
                break;

            case OperativeShootAction shootAction:
                OperativeStates[shootAction.Target].Status = OperativeStatus.Neutralized;
                break;

            default:
                throw new InvalidOperationException();
        }

        return this;
    }

    public MatchState ReadyOperatives()
    {
        foreach (var operative in OperativeStates)
            if (operative.Status == OperativeStatus.Activated)
                operative.Status = OperativeStatus.Ready;

        return this;
    }

    public bool TurningPointFinished()
    {
        return OperativeStates.Where(a => a.Status == OperativeStatus.Ready).Count() == 0;
    }

    public OperativeState SelectRandomOperative(TeamSide side)
    {
        return OperativeStates.Where(a => a.Side == side && a.Status == OperativeStatus.Ready).OrderBy(a => Random.Shared.Next()).FirstOrDefault()!;
    }

    public OperativeMoveAction GenerateRandomMoveAction(OperativeState operative)
    {
        var position = new Position();
        GenerateRandomMovePosition(operative, operative.Type.Movement, ref position);
        return new OperativeMoveAction(operative.Index, position);
    }

    public OperativeDashAction GenerateRandomDashAction(OperativeState operative)
    {

        var position = new Position();
        GenerateRandomMovePosition(operative, KillZone.SquareDistance, ref position);
        return new OperativeDashAction(operative.Index, position);
    }

    public OperativeShootAction GenerateRandomShootAction(OperativeState operative)
    {
        var enemySide = operative.Side.GetOppositeSide();

        var enemies = OperativeStates.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (IsTargetValid(operative.Position, enemy.Position))
                return new OperativeShootAction(operative.Index, enemy.Index);
        }

        return null!;
    }

    void GenerateRandomMovePosition(OperativeState operative, float maxDist, ref Position destination)
    {
        const int maxTries = 100;
        int guard = 0;

        destination = new Position();
        do
        {
            destination.X = operative.Position.X + (2 * (float)Random.Shared.NextDouble() - 1) * maxDist;
            destination.Y = operative.Position.Y + (2 * (float)Random.Shared.NextDouble() - 1) * maxDist;
            guard++;
        } while (!IsMoveValid(operative, destination, maxDist) && guard < maxTries);

        if (guard >= maxTries)
        {
            destination = operative.Position;
        }
    }

    bool IsMoveValid(OperativeState operative, Position destination, float maxDist)
    {
        // must be fully inside the killzone
        if (destination.X + (operative.Type.BaseDiameter / 2) >= KillZone.TotalWidth)
            return false;
        if (destination.Y + (operative.Type.BaseDiameter / 2) >= KillZone.TotalHeight)
            return false;
        if (destination.X - (operative.Type.BaseDiameter / 2) < 0)
            return false;
        if (destination.Y - (operative.Type.BaseDiameter / 2) < 0)
            return false;

        // no more than maxdist
        if (Utils.Distance(operative.Position, destination) > maxDist)
            return false;

        var operativeCircle = new Circle(destination, operative.Type.BaseDiameter / 2);

        // no collision with other operatives
        foreach (var other in OperativeStates)
        {
            if (other.Index == operative.Index)
                continue;
            if (other.Status == OperativeStatus.Neutralized)
                continue;

            if (Utils.Intersects(operativeCircle, new Circle(other.Position, other.Type.BaseDiameter / 2)))
                return false;
        }

        // no collision with terrains
        foreach (var terrain in KillZone.Terrains)
        {
            if (Utils.Intersects(operativeCircle, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        return true;
    }

    bool IsTargetValid(Position source, Position target)
    {
        var segment = new Segment(source, target);

        // no collision with terrains
        foreach (var terrain in KillZone.Terrains)
        {
            //if (!terrain.Type.HasFlag(TerrainType.Heavy))
            //    continue;

            if (Utils.Intersects(segment, new Rectangle(terrain.Position, terrain.Width, terrain.Height)))
                return false;
        }

        // TODO: no collision with other operatives
        return true;
    }

}