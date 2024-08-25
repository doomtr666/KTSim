using Microsoft.Extensions.Logging;

namespace KTSim.Models;

public class MatchTrainer
{
    ILogger<MatchTrainer> _log;

    KillZone _killZone;
    MatchState _initialState;

    public int GameCount { get; private set; } = 0;

    AINet _aiNet;
    AITrainer _aiTrainer;

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
        _killZone = new KillZone();

        // create Operatives
        var operatives = new List<OperativeState>();

        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, attacker, TeamSide.Attacker, OperativeStatus.Ready, new Position(30 + 40 * i, 30));
            operatives.Add(operativeState);
        }

        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, defender, TeamSide.Defender, OperativeStatus.Ready, new Position(KillZone.TotalWidth - 30 - 40 * i, KillZone.TotalHeight - 30));
            operatives.Add(operativeState);
        }

        _initialState = new MatchState(_killZone, operatives.ToArray());

        // create AI
        _aiNet = new AINet();
        _aiTrainer = new AITrainer(_aiNet);
    }

    public Match GenerateMatch()
    {
        _log.LogInformation($"Match {GameCount} started");

        var currentState = _initialState.Copy();

        var playedActions = new List<IOperativeAction>();

        var attackerScore = 0;
        var defenderScore = 0;

        for (var turningPoint = 1; turningPoint <= 4; turningPoint++)
        {
            // determine initiative
            var sideTurn = InitiativeRoll();

            // prepare operatives
            currentState.ReadyOperatives();

            // turning point starts
            _log.LogInformation($"Turning Point {turningPoint}, {sideTurn} have the initiative");

            while (!currentState.TurningPointFinished())
            {
                // select random operative
                var operative = currentState.SelectRandomOperative(sideTurn);

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
                            var action = currentState.GenerateRandomShootAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                currentState.ApplyAction(action);
                                shoot = true;
                                continue;
                            }
                        }

                        if (!move)
                        {
                            var action = currentState.GenerateRandomMoveAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                currentState.ApplyAction(action);
                                move = true;
                                continue;
                            }
                        }

                        if (!dash)
                        {
                            var action = currentState.GenerateRandomDashAction(operative);

                            if (action != null)
                            {
                                _log.LogInformation($"Choosen Action {i}: {action}");
                                playedActions.Add(action);
                                currentState.ApplyAction(action);
                                dash = true;
                                continue;
                            }
                        }

                        _log.LogWarning($"No valid action found for operative {operative.Index}");
                    }

                    operative.Status = OperativeStatus.Activated;
                }

                _aiTrainer.Train(currentState, playedActions.ToArray(), currentState, 0);


                // apply action
                sideTurn = sideTurn.GetOppositeSide();
            }

            // compute score
            var attackerTurnScore = 0;
            var defenderTurnScore = 0;
            foreach (var objective in _killZone.Objectives)
            {
                var attackerCount = currentState.OperativeStates.Where(a => a.Side == TeamSide.Attacker && a.Status != OperativeStatus.Neutralized
                    && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();
                var defenderCount = currentState.OperativeStates.Where(a => a.Side == TeamSide.Defender && a.Status != OperativeStatus.Neutralized
                    && Utils.Distance(a.Position, objective.Position) <= Objective.Radius + a.Type.BaseDiameter / 2).Count();

                if (attackerCount == 0 && defenderCount == 0)
                    continue;

                if (attackerCount > defenderCount)
                    attackerTurnScore++;

                if (attackerCount < defenderCount)
                    defenderTurnScore++;
            }

            attackerScore += attackerTurnScore;
            defenderScore += defenderTurnScore;
            _log.LogInformation($"*** End of Turning poin {turningPoint}, Score: Attacker {attackerTurnScore}/{attackerScore} - Defender {defenderTurnScore}/{defenderScore}");

        }

        GameCount++;

        return new Match(_killZone, _initialState.OperativeStates.ToList(), playedActions, attackerScore, defenderScore);
    }


    public TeamSide InitiativeRoll()
    {
        var values = Enum.GetValues(typeof(TeamSide));
        var side = (TeamSide)values.GetValue(Random.Shared.Next(values.Length))!;

        return side;
    }
}
