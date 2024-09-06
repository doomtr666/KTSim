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

    AINet _aiNet;
    AITrainer _aiTrainer;

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

        // create AI
        _aiNet = new AINet();
        _aiTrainer = new AITrainer(_aiNet, TeamSide.Attacker);
    }

    public Match GenerateMatch()
    {
        var sw = new Stopwatch();
        sw.Start();
        var matchState = _initialState.Copy();

        var playedActions = new List<IOperativeAction>();

        while (!matchState.IsFinished)
        {
            var action = matchState.GenerateAction();
            playedActions.Add(action);
            matchState.ApplyAction(action);
        }
        sw.Stop();

        _log.LogInformation($"Match {GameCount} finished ({sw.Elapsed.TotalMilliseconds} ms), Attacker: {matchState.AttackerScore}, Defender: {matchState.DefenderScore}");

        return new Match(_killZone, _initialState.Copy().OperativeStates, playedActions.ToArray(), matchState.InitiativeRolls, matchState.AttackerScore, matchState.DefenderScore);
    }
}
