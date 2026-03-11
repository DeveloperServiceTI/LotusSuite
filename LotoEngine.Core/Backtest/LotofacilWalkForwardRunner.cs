using System.Text.Json;

namespace LotoEngine.Core.Backtest;

public sealed record LotoHistoricInfo(int ID, DateTime Data, HashSet<int> Numeros, bool isGanhadores15Acertos);

/// <summary>
/// Runner de walk-forward (janela 21) para Lotofácil usando histórico já carregado.
/// </summary>
public sealed class LotofacilWalkForwardRunner
{
    public enum StartMode
    {
        FromBeginning,
        LastN,
        FromContestId
    }

    public enum FailPolicy
    {
        Retry,
        FailFast,
        Stop
    }

    public sealed record Config(
        int WindowSize = 21,
        int TicketSize = 15,
        int MinHits = 13,
        int MaxRetries = 2,
        StartMode Start = StartMode.FromBeginning,
        int? LastN = null,
        int? StartContestId = null,
        FailPolicy Policy = FailPolicy.Retry,
        string? ExportJsonPath = null);

    public sealed record TicketEvaluation(HashSet<int> Ticket, int Hits);

    public sealed record RoundResult(
        int RoundIndex,
        List<int> TrainContestIds,
        int TargetContestId,
        DateTime TargetDate,
        List<TicketEvaluation> TicketEvaluations,
        int BestHits,
        HashSet<int>? BestTicket,
        bool Passed,
        int Attempts,
        bool IsWinner15AcertosTarget);

    public sealed record RunResult(
        int TotalRounds,
        int PassedRounds,
        int FailedRounds,
        double PassRate,
        List<RoundResult> Rounds);

    /// <summary>
    /// Executa walk-forward no histórico ordenado por ID (concurso).
    /// </summary>
    /// <param name="historico">Lista de LotoHistoricInfo (ID, Data, Numeros, isGanhadores15Acertos).</param>
    /// <param name="generateTickets">Delegate que recebe treino e ticketSize, retornando cartelas (15+ dezenas).</param>
    /// <param name="config">Configuração do runner.</param>
    public RunResult Run(
        List<LotoHistoricInfo> historico,
        Func<List<LotoHistoricInfo>, int, IEnumerable<HashSet<int>>> generateTickets,
        Config? config = null)
    {
        config ??= new Config();
        if (historico is null || historico.Count == 0)
            throw new ArgumentException("Histórico não pode ser nulo/vazio.", nameof(historico));

        var ordered = historico.OrderBy(h => h.ID).ToList();
        var startIndex = ResolveStartIndex(ordered, config);
        if (startIndex < config.WindowSize)
            startIndex = config.WindowSize;

        var rounds = new List<RoundResult>();
        var stopRequested = false;

        for (var i = startIndex; i < ordered.Count && !stopRequested; i++)
        {
            var treino = ordered.Skip(i - config.WindowSize).Take(config.WindowSize).ToList();
            var alvo = ordered[i];

            var attempt = 0;
            var passed = false;
            var bestHits = -1;
            HashSet<int>? bestTicket = null;
            var evals = new List<TicketEvaluation>();

            while (true)
            {
                attempt++;
                var geradas = (generateTickets(treino, config.TicketSize) ?? Array.Empty<HashSet<int>>())
                    .Where(t => t is not null && t.Count >= config.TicketSize)
                    .ToList();

                var attemptEvals = geradas
                    .Select(t => new TicketEvaluation(new HashSet<int>(t), CountHits(t, alvo.Numeros)))
                    .ToList();

                evals.AddRange(attemptEvals);

                var bestAttempt = attemptEvals
                    .OrderByDescending(x => x.Hits)
                    .FirstOrDefault();

                if (bestAttempt is not null && bestAttempt.Hits > bestHits)
                {
                    bestHits = bestAttempt.Hits;
                    bestTicket = new HashSet<int>(bestAttempt.Ticket);
                }

                passed = bestHits >= config.MinHits;
                if (passed)
                    break;

                if (config.Policy == FailPolicy.FailFast)
                    break;

                if (config.Policy == FailPolicy.Stop)
                {
                    stopRequested = true;
                    break;
                }

                if (config.Policy == FailPolicy.Retry && attempt > config.MaxRetries)
                    break;
            }

            rounds.Add(new RoundResult(
                RoundIndex: rounds.Count + 1,
                TrainContestIds: treino.Select(t => t.ID).ToList(),
                TargetContestId: alvo.ID,
                TargetDate: alvo.Data,
                TicketEvaluations: evals,
                BestHits: Math.Max(bestHits, 0),
                BestTicket: bestTicket,
                Passed: passed,
                Attempts: attempt,
                IsWinner15AcertosTarget: alvo.isGanhadores15Acertos));
        }

        var run = new RunResult(
            TotalRounds: rounds.Count,
            PassedRounds: rounds.Count(r => r.Passed),
            FailedRounds: rounds.Count(r => !r.Passed),
            PassRate: rounds.Count == 0 ? 0 : rounds.Count(r => r.Passed) / (double)rounds.Count,
            Rounds: rounds);

        PrintSummary(run, config);

        if (!string.IsNullOrWhiteSpace(config.ExportJsonPath))
        {
            var folder = Path.GetDirectoryName(config.ExportJsonPath);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            File.WriteAllText(
                config.ExportJsonPath,
                JsonSerializer.Serialize(run, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"JSON exportado em: {config.ExportJsonPath}");
        }

        return run;
    }

    private static int CountHits(HashSet<int> ticket, HashSet<int> target)
        => ticket.Count(target.Contains);

    private static int ResolveStartIndex(List<LotoHistoricInfo> ordered, Config config)
    {
        return config.Start switch
        {
            StartMode.FromBeginning => config.WindowSize,
            StartMode.LastN => Math.Max(0, ordered.Count - (config.LastN ?? ordered.Count)),
            StartMode.FromContestId => Math.Max(0, ordered.FindIndex(x => x.ID >= (config.StartContestId ?? int.MinValue))),
            _ => config.WindowSize
        };
    }

    private static void PrintSummary(RunResult run, Config config)
    {
        Console.WriteLine("================ Walk-Forward Lotofácil (7x21) ================");
        Console.WriteLine($"WindowSize={config.WindowSize} TicketSize={config.TicketSize} MinHits={config.MinHits} MaxRetries={config.MaxRetries}");
        Console.WriteLine($"Rounds={run.TotalRounds} Passed={run.PassedRounds} Failed={run.FailedRounds} PassRate={run.PassRate:P2}");
        Console.WriteLine("---------------------------------------------------------------");

        foreach (var r in run.Rounds)
        {
            Console.WriteLine(
                $"Round {r.RoundIndex:D3} | Target={r.TargetContestId} ({r.TargetDate:yyyy-MM-dd}) | " +
                $"Best={r.BestHits} | Passed={(r.Passed ? "YES" : "NO")} | Attempts={r.Attempts} | " +
                $"Tickets={r.TicketEvaluations.Count}");
        }

        Console.WriteLine("===============================================================");
    }
}
