using Microsoft.Extensions.Logging;

namespace KTSim.Models;

public enum OperativeStatus
{
    Ready,
    Active,
    Activated,
    Neutralized,
}

public class OperativeState
{
    public int Index { get; }
    public IOperativeType Type { get; }
    public TeamSide Side { get; }
    public OperativeStatus Status { get; set; }
    public int ActionPoints { get; set; }
    public OperativeActionType PerformedActions { get; set; }
    public Position Position { get; set; }

    public OperativeState(int index, IOperativeType type, TeamSide side, Position position)
    {
        Index = index;
        Type = type;
        Side = side;
        Status = OperativeStatus.Ready;
        PerformedActions = 0;
        ActionPoints = type.ActionPointLimit;
        Position = position;
    }

    public OperativeState(int index, IOperativeType type, TeamSide side, OperativeStatus state, int actionPoint, OperativeActionType performedActions, Position position)
    {
        Index = index;
        Type = type;
        Side = side;
        Status = state;
        ActionPoints = actionPoint;
        PerformedActions = performedActions;
        Position = position;
    }

    public OperativeState Copy()
    {
        return new OperativeState(Index, Type, Side, Status, ActionPoints, PerformedActions, Position);
    }
}

public class MatchState
{
    public const int MaxTurningPoints = 4;

    private ILogger _log;

    public KillZone KillZone { get; }

    public OperativeState[] OperativeStates { get; }

    public int TurningPoint { get; private set; }

    public TeamSide CurrentTurn { get; private set; }

    public TeamSide[] InitiativeRolls { get; private set; }

    public int AttackerScore { get; private set; }

    public int DefenderScore { get; private set; }

    public bool IsFinished => TurningPoint >= MaxTurningPoints;

    public MatchState(KillZone killZone, OperativeState[] operatives)
    {
        _log = Logger.Instance;
        KillZone = killZone;
        OperativeStates = operatives;
        TurningPoint = 0;
        InitiativeRolls = new TeamSide[MaxTurningPoints];
        for (int i = 0; i < MaxTurningPoints; i++)
            InitiativeRolls[i] = InitiativeRoll();
        CurrentTurn = InitiativeRolls[0];
        AttackerScore = 0;
        DefenderScore = 0;
    }

    public MatchState(KillZone killZone, OperativeState[] operatives, TeamSide[] initiativeRolls)
    {
        _log = Logger.Instance;
        KillZone = killZone;
        OperativeStates = operatives;
        TurningPoint = 0;
        InitiativeRolls = initiativeRolls;
        CurrentTurn = InitiativeRolls[0];
        AttackerScore = 0;
        DefenderScore = 0;
    }

    public MatchState(KillZone killZone, OperativeState[] operatives, int turningPoint, TeamSide[] initiativeRolls, TeamSide currentTurn, int attackerScore, int defenderScore)
    {
        _log = Logger.Instance;
        KillZone = killZone;
        OperativeStates = operatives;
        TurningPoint = turningPoint;
        InitiativeRolls = initiativeRolls;
        CurrentTurn = currentTurn;
        AttackerScore = attackerScore;
        DefenderScore = defenderScore;
    }

    public MatchState Copy()
    {
        OperativeState[] operatives = new OperativeState[OperativeStates.Length];
        for (int i = 0; i < OperativeStates.Length; i++)
            operatives[i] = OperativeStates[i].Copy();
        return new MatchState(KillZone, operatives, TurningPoint, InitiativeRolls, CurrentTurn, AttackerScore, DefenderScore);
    }

    public MatchState ApplyAction(IOperativeAction action)
    {
        if (action == null)
        {
            CurrentTurn = CurrentTurn.GetOppositeSide();
            return this;
        }

        if (!IsActionValid(action))
            throw new InvalidOperationException();

        OperativeStates[action.Operative].Status = OperativeStatus.Active;

        switch (action)
        {
            case OperativeMoveAction moveAction:
                OperativeStates[moveAction.Operative].Position = moveAction.Destination;
                OperativeStates[moveAction.Operative].PerformedActions |= OperativeActionType.Move;
                break;

            case OperativeDashAction dashAction:
                OperativeStates[dashAction.Operative].Position = dashAction.Destination;
                OperativeStates[dashAction.Operative].PerformedActions |= OperativeActionType.Dash;
                break;

            case OperativeShootAction shootAction:
                OperativeStates[shootAction.Target].Status = OperativeStatus.Neutralized;
                OperativeStates[shootAction.Operative].PerformedActions |= OperativeActionType.Shoot;
                break;

            default:
                throw new InvalidOperationException();
        }

        OperativeStates[action.Operative].ActionPoints--;

        if (OperativeStates[action.Operative].ActionPoints == 0)
        {
            OperativeStates[action.Operative].Status = OperativeStatus.Activated;

            if (TurningPointFinished())
            {
                // score
                CountCapturedObjectives(out var attackerTurnScore, out var defenderTurnScore);
                AttackerScore += attackerTurnScore;
                DefenderScore += defenderTurnScore;

                TurningPoint++;
                if (IsFinished)
                    return this;
                ReadyOperatives();
                CurrentTurn = InitiativeRolls[TurningPoint];
            }
            else
            {
                CurrentTurn = CurrentTurn.GetOppositeSide();
            }
        }

        return this;
    }

    public MatchState ReadyOperatives()
    {
        foreach (var operative in OperativeStates)
            if (operative.Status == OperativeStatus.Activated)
            {
                operative.Status = OperativeStatus.Ready;
                operative.ActionPoints = operative.Type.ActionPointLimit;
                operative.PerformedActions = 0;
            }
        return this;
    }

    public bool TurningPointFinished()
    {
        return OperativeStates.Where(a => a.Status == OperativeStatus.Ready || a.Status == OperativeStatus.Active).Count() == 0;
    }


    public OperativeState SelectOperative()
    {
        // if an operative is active, it must be selected
        var activeOperative = OperativeStates.Where(a => a.Side == CurrentTurn && a.Status == OperativeStatus.Active).FirstOrDefault();
        if (activeOperative != null)
            return activeOperative;

        // otherwise, select random ready operative 
        return OperativeStates.Where(a => a.Side == CurrentTurn && a.Status == OperativeStatus.Ready).OrderBy(a => Random.Shared.Next()).FirstOrDefault()!;
    }

    public IOperativeAction GenerateAction()
    {
        var operative = SelectOperative();
        if (operative == null)
            return null!;

        // by order of priority
        if (!operative.PerformedActions.HasFlag(OperativeActionType.Shoot))
        {
            var action = GenerateShootAction(operative);
            if (action != null)
                return action;
        }

        if (!operative.PerformedActions.HasFlag(OperativeActionType.Move))
        {
            var action = GenerateMoveAction(operative);
            if (action != null)
                return action;
        }

        if (!operative.PerformedActions.HasFlag(OperativeActionType.Dash))
        {
            var action = GenerateDashAction(operative);
            if (action != null)
                return action;
        }

        return null!;
    }

    public OperativeMoveAction GenerateMoveAction(OperativeState operative)
    {
        var position = new Position();
        GenerateMovePosition(operative, operative.Type.Movement, ref position);
        return new OperativeMoveAction(operative.Index, position);
    }

    public OperativeDashAction GenerateDashAction(OperativeState operative)
    {

        var position = new Position();
        GenerateMovePosition(operative, KillZone.SquareDistance, ref position);
        return new OperativeDashAction(operative.Index, position);
    }

    public OperativeShootAction GenerateShootAction(OperativeState operative)
    {
        var enemySide = operative.Side.GetOppositeSide();

        var enemies = OperativeStates.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (HasLineOfSight(operative.Position, enemy.Position))
                return new OperativeShootAction(operative.Index, enemy.Index);
        }

        return null!;
    }

    public void CountCapturedObjectives(out int attackerTurnScore, out int defenderTurnScore)
    {
        attackerTurnScore = 0;
        defenderTurnScore = 0;
        foreach (var objective in KillZone.Objectives)
        {
            var attackerCount = OperativeStates.Where(a => a.Side == TeamSide.Attacker && a.Status != OperativeStatus.Neutralized
                && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();
            var defenderCount = OperativeStates.Where(a => a.Side == TeamSide.Defender && a.Status != OperativeStatus.Neutralized
                && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();

            if (attackerCount == 0 && defenderCount == 0)
                continue;

            if (attackerCount > defenderCount)
                attackerTurnScore++;

            if (attackerCount < defenderCount)
                defenderTurnScore++;
        }
    }

    void GenerateMovePosition(OperativeState operative, float maxDist, ref Position destination)
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

    public bool HasLineOfSight(Position source, Position target)
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

    public bool IsActionValid(IOperativeAction action)
    {
        // check if the match is finished
        if (IsFinished)
            return false;

        // check if the operative exists
        if (action.Operative < 0 || action.Operative >= OperativeStates.Length)
        {
            //_log.LogError($"Invalid Operative {action.Operative}");
            return false;
        }

        var activeOperative = OperativeStates.Where(a => a.Side == CurrentTurn && a.Status == OperativeStatus.Active).FirstOrDefault();
        if (activeOperative != null)
        {
            // if an operative is active, it must be selected
            if (activeOperative.Index != action.Operative)
            {
                //_log.LogError($"Operative {activeOperative.Index} is active ({action.Operative})");
                return false;
            }
        }
        else
        {
            // otherwise, operative must be ready
            if (OperativeStates[action.Operative].Status != OperativeStatus.Ready)
            {
                //_log.LogError($"Operative {action.Operative} is not ready");
                return false;
            }
        }

        // check if the operative is on the correct side
        if (OperativeStates[action.Operative].Side != CurrentTurn)
        {
            //_log.LogError($"Operative {action.Operative} is on the wrong side");
            return false;
        }

        // check if the operative has action points
        if (OperativeStates[action.Operative].ActionPoints <= 0)
        {
            _log.LogError($"Operative {action.Operative} has no action points");
            return false;
        }

        switch (action)
        {
            case OperativeMoveAction moveAction:
                return IsMoveValid(OperativeStates[moveAction.Operative], moveAction.Destination, OperativeStates[moveAction.Operative].Type.Movement);

            case OperativeDashAction dashAction:
                return IsMoveValid(OperativeStates[dashAction.Operative], dashAction.Destination, KillZone.SquareDistance);

            case OperativeShootAction shootAction:
                // check if the target exists
                if (shootAction.Target < 0 || shootAction.Target >= OperativeStates.Length)
                {
                    //_log.LogError($"Invalid Target {shootAction.Target}");
                    return false;
                }

                // check if the target is on the correct side
                if (OperativeStates[shootAction.Target].Side == OperativeStates[shootAction.Operative].Side)
                {
                    //_log.LogError($"Target {shootAction.Target} is on the wrong side");
                    return false;
                }

                // check if the target is not neutralized
                if (OperativeStates[shootAction.Target].Status == OperativeStatus.Neutralized)
                {
                    //_log.LogError($"Target {shootAction.Target} is already neutralized");
                    return false;
                }

                // check if the target is visible
                if (!HasLineOfSight(OperativeStates[shootAction.Operative].Position, OperativeStates[shootAction.Target].Position))
                {
                    //_log.LogError($"Target {shootAction.Target} is not visible");
                    return false;
                }

                return true;
        }

        // should never reach here
        //_log.LogError($"Invalid action {action}");
        return false;
    }

    public TeamSide InitiativeRoll()
    {
        var values = Enum.GetValues(typeof(TeamSide));
        var side = (TeamSide)values.GetValue(Random.Shared.Next(values.Length))!;

        return side;
    }

}