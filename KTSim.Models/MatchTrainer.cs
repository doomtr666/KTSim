using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KTSim.Models;


public static class Logger
{
    public static ILogger Instance { get; }

    static Logger()
    {
        using var factory = LoggerFactory.Create(builder => builder
            .AddFilter("Logger", LogLevel.Debug)
            .AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            }));

        Instance = factory.CreateLogger("Logger");
    }
}

public class MatchTrainer
{
    ILogger _log;

    KillZone _killZone;
    MatchState _initialState;

    public int GameCount { get; private set; } = 0;

    AITrainer _attackerAI;
    //AITrainer _defenderAI;

    private MovingAverage _attackerWinRate = new MovingAverage(100);
    private MovingAverage _defenderWinRate = new MovingAverage(100);

    public MatchTrainer()
    {
        _log = Logger.Instance;

        // create KillZone
        _killZone = new KillZone();

        // create Operatives
        var operatives = new List<OperativeState>();

        var attacker = new KommandoBoyOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, attacker, TeamSide.Attacker, new Position(30 + 40 * i, 30));
            operatives.Add(operativeState);
        }

        var defender = new VeteranTrooperOperative();
        for (var i = 0; i < 10; i++)
        {
            var operativeState = new OperativeState(operatives.Count, defender, TeamSide.Defender, new Position(KillZone.TotalWidth - 30 - 40 * i, KillZone.TotalHeight - 30));
            operatives.Add(operativeState);
        }

        _initialState = new MatchState(_killZone, operatives.ToArray());

        // create AIs
        _attackerAI = new AITrainer(TeamSide.Attacker);
        //_defenderAI = new AITrainer(TeamSide.Defender);
    }

    public Match GenerateMatch()
    {
        var sw = new Stopwatch();
        sw.Start();
        var matchState = _initialState.Copy();

        var playedActions = new List<IOperativeAction>();

        while (!matchState.IsFinished)
        {
            IOperativeAction action = null!;

            if (matchState.CurrentTurn == TeamSide.Attacker)
            {
                action = _attackerAI.GenerateAction(matchState);
                //_attackerAI.TrainLast();
            }
            else
            {
                //action = _defenderAI.GenerateAction(matchState);
                //_defenderAI.TrainLast();
                action = matchState.GenerateAction();
            }

            playedActions.Add(action);
            matchState.ApplyAction(action);
        }


        _attackerAI.TrainBatch();
        //_defenderAI.TrainBatch();

        sw.Stop();

        GameCount++;
        _attackerWinRate.Add(matchState.AttackerScore > matchState.DefenderScore ? 1 : 0);
        _defenderWinRate.Add(matchState.AttackerScore < matchState.DefenderScore ? 1 : 0);

        _log.LogInformation($"Match {GameCount} finished ({sw.Elapsed.TotalMilliseconds} ms), Attacker: {matchState.AttackerScore}, Defender: {matchState.DefenderScore}, Attacker Win Rate: {_attackerWinRate.Average():P} Defender Win Rate: {_defenderWinRate.Average():P}");


        return new Match(_killZone, _initialState.Copy().OperativeStates, playedActions.ToArray(), matchState.InitiativeRolls, matchState.AttackerScore, matchState.DefenderScore);
    }
}
