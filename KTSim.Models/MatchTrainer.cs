using Microsoft.Extensions.Logging;
using Tensorboard;

namespace KTSim.Models;

public enum TurnSide
{
    Attacker,
    Defender,
}

public class MatchTrainer : MatchRunnerBase
{
    ILogger<MatchTrainer> _log;

    public int GameCount { get; private set; } = 0;

    public MatchTrainer()
    {
        // logger
        using var factory = LoggerFactory.Create(builder => builder
            .AddFilter("KilZone", LogLevel.Debug)
            .AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            }));

        _log = factory.CreateLogger<MatchTrainer>();

        // create KillZone
        KillZone = new KillZone();

        // create Operatives
        var operatives = new List<OperativeState>();

        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, attacker, TurnSide.Attacker, OperativeStatus.Ready, new Position(30 + 40 * i, 30));
            operatives.Add(operativeState);
        }

        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, defender, TurnSide.Defender, OperativeStatus.Ready, new Position(KillZone.TotalWidth - 30 - 40 * i, KillZone.TotalHeight - 30));
            operatives.Add(operativeState);
        }

        InitialOperativeStates = operatives;

        Reset();
    }

    public Match GenerateMatch()
    {
        Reset();

        _log.LogInformation($"Match {GameCount} started");

        var playedActions = new List<IOperativeAction>();

        var attackerScore = 0;
        var defenderScore = 0;

        for (var turningPoint = 1; turningPoint <= 4; turningPoint++)
        {
            // determine initiative
            var sideTurn = InitiativeRoll();

            // prepare operatives
            foreach (var operative in CurrentOperativeStates)
            {
                if (operative.Status != OperativeStatus.Neutralized)
                    operative.Status = OperativeStatus.Ready;
            }

            // turning point starts
            _log.LogInformation($"Turning Point {turningPoint}, {sideTurn} have the initiative");

            while (CurrentOperativeStates.Where(a => a.Status == OperativeStatus.Ready).Count() > 0)
            {
                // select random operative
                var operative = CurrentOperativeStates.Where(a => a.Status == OperativeStatus.Ready && a.Side == sideTurn).OrderBy(a => Random.Shared.Next()).FirstOrDefault();

                if (operative != null)
                {
                    _log.LogInformation($"Selected operative: {operative.Type.Name} ({operative.Index})");

                    bool shoot = false;
                    bool move = false;
                    bool dash = false;

                    for (var i = 0; i < operative.Type.ActionPointLimit; i++)
                    {
                        // by order of priority
                        if (!shoot)
                        {
                            var action = GenerateRandomShootAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                ApplyAction(action);
                                shoot = true;
                                continue;
                            }
                        }

                        if (!move)
                        {
                            var action = GenerateRandomMoveAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                ApplyAction(action);
                                move = true;
                                continue;
                            }
                        }

                        if (!dash)
                        {
                            var action = GenerateRandomDashAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                ApplyAction(action);
                                dash = true;
                                continue;
                            }
                        }

                        _log.LogWarning($"No valid action found for operative {operative.Index}");
                    }

                    operative.Status = OperativeStatus.Activated;
                }

                // apply action
                sideTurn = GetOppositeSide(sideTurn);
            }

            // compute score
            foreach (var objective in KillZone.Objectives)
            {
                var attackerCount = CurrentOperativeStates.Where(a => a.Side == TurnSide.Attacker && a.Status != OperativeStatus.Neutralized 
                    && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();
                var defenderCount = CurrentOperativeStates.Where(a => a.Side == TurnSide.Defender && a.Status != OperativeStatus.Neutralized 
                    && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();

                if (attackerCount == 0 && defenderCount == 0)
                    continue;

                if (attackerCount > defenderCount)
                    attackerScore++;

                if (attackerCount < defenderCount)
                    defenderScore++;
            }

            _log.LogInformation($"**** End of Turning poin {turningPoint}, Score: Attacker {attackerScore} - Defender {defenderScore}");

        }

        GameCount++;

        return new Match(KillZone, InitialOperativeStates, playedActions, attackerScore, defenderScore);
    }


    public TurnSide InitiativeRoll()
    {
        var values = Enum.GetValues(typeof(TurnSide));
        var side = (TurnSide)values.GetValue(Random.Shared.Next(values.Length))!;

        return side;
    }


    TurnSide GetOppositeSide(TurnSide side)
    {
        return side switch
        {
            TurnSide.Attacker => TurnSide.Defender,
            TurnSide.Defender => TurnSide.Attacker,
            _ => throw new ArgumentException("Invalid side", nameof(side)),
        };
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
            _log.LogWarning("No valid move found");
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
        foreach (var other in CurrentOperativeStates)
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

    OperativeMoveAction GenerateRandomMoveAction(OperativeState operative)
    {
        var position = new Position();
        GenerateRandomMovePosition(operative, operative.Type.Movement, ref position);
        return new OperativeMoveAction(operative.Index, position);
    }

    OperativeDashAction GenerateRandomDashAction(OperativeState operative)
    {

        var position = new Position();
        GenerateRandomMovePosition(operative, KillZone.SquareDistance, ref position);
        return new OperativeDashAction(operative.Index, position);
    }

    OperativeShootAction GenerateRandomShootAction(OperativeState operative)
    {
        var enemySide = GetOppositeSide(operative.Side);

        var enemies = CurrentOperativeStates.Where(a => a.Side == enemySide && a.Status != OperativeStatus.Neutralized).OrderBy(a => Random.Shared.Next()).ToArray();

        foreach (var enemy in enemies)
        {
            if (IsTargetValid(operative.Position, enemy.Position))
                return new OperativeShootAction(operative.Index, enemy.Index);
        }

        return null!;
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
