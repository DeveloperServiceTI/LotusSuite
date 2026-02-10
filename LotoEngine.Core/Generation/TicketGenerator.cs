using LotoEngine.Core.Analysis;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Utils;

namespace LotoEngine.Core.Generation;

public sealed class TicketGenerator
{
    public IReadOnlyList<Ticket> Generate(IReadOnlyList<Draw> history, IGameDefinition game, RecipeAnalysisResult analysis, GenerationOptions options, double alpha = 0.6, double beta = 0.4)
    {
        var last = history.Last();
        var stats = analysis.NumberStats;
        var stayRank = last.NumbersSorted
            .Select(n => (n, score: alpha * stats.First(s => s.Number == n).StayRate + beta * stats.First(s => s.Number == n).Hot7Norm, presence: stats.First(s => s.Number == n).Presence))
            .OrderByDescending(x => x.score).ThenByDescending(x => x.presence).Select(x => x.n).ToList();

        var enterRank = Enumerable.Range(game.RangeMin, game.RangeMax - game.RangeMin + 1)
            .Except(last.NumbersSorted)
            .Select(n => (n, score: alpha * stats.First(s => s.Number == n).EnterRate + beta * stats.First(s => s.Number == n).Cold7Bonus, presence: stats.First(s => s.Number == n).Presence))
            .OrderByDescending(x => x.score).ThenByDescending(x => x.presence).Select(x => x.n).ToList();

        var anchors = options.Mode.ToUpperInvariant() switch
        {
            "GRUDA" => Math.Max(analysis.DynamicAnchorsK, game.NumbersPerTicket - 4),
            "RODIZIO" => Math.Max(game.NumbersPerTicket - 7, analysis.DynamicAnchorsK - 2),
            _ => Math.Min(game.NumbersPerTicket - 2, analysis.DynamicAnchorsK)
        };

        var tickets = new List<Ticket>();
        for (var i = 0; i < options.Count; i++)
        {
            var startMin = options.StartMin ?? 1;
            var forbidden = startMin switch { 2 => new[] { 1 }, 3 => new[] { 1, 2 }, _ => Array.Empty<int>() };
            var pool = new HashSet<int>(Enumerable.Range(game.RangeMin, game.RangeMax - game.RangeMin + 1).Except(forbidden));
            var picked = new SortedSet<int>();
            picked.Add(startMin);

            foreach (var n in stayRank.Where(pool.Contains))
            {
                if (picked.Count >= anchors) break;
                picked.Add(n);
            }
            foreach (var n in enterRank.Where(pool.Contains))
            {
                if (picked.Count >= game.NumbersPerTicket) break;
                picked.Add(n);
            }
            foreach (var n in stayRank.Where(pool.Contains))
            {
                if (picked.Count >= game.NumbersPerTicket) break;
                picked.Add(n);
            }

            EnforceGroups(game, analysis.LastGroupCounts, options.Mode, picked);
            ApplyRunsFilter(analysis, game, picked);

            while (picked.Count < game.NumbersPerTicket)
                picked.Add(pool.Except(picked).First());

            var month = game.HasExtra ? game.ExtraDomain![i % game.ExtraDomain.Count] : null;
            tickets.Add(new Ticket(DateTime.UtcNow.Ticks + i, game.Game, picked.OrderBy(x => x).ToArray(), month, options.Mode.ToUpperInvariant(), $"anchors={anchors};start={startMin};dynGroup={options.GroupPolicyDynamic}"));
        }
        return tickets;
    }

    private static void ApplyRunsFilter(RecipeAnalysisResult analysis, IGameDefinition game, SortedSet<int> picked)
    {
        while (RunsCalculator.Analyze(picked.ToArray()).MaxRun > analysis.TargetMaxRun + 1)
        {
            var largest = picked.Max;
            picked.Remove(largest);
            var candidate = Enumerable.Range(game.RangeMin, game.RangeMax - game.RangeMin + 1).First(n => !picked.Contains(n));
            picked.Add(candidate);
        }
    }

    private static void EnforceGroups(IGameDefinition game, IReadOnlyDictionary<string, int> lastGroupCounts, string mode, SortedSet<int> picked)
    {
        if (!mode.Equals("NORMAL", StringComparison.OrdinalIgnoreCase) && !mode.Equals("RODIZIO", StringComparison.OrdinalIgnoreCase)) return;
        var g5 = game.Groups.Last();
        var g5Count = lastGroupCounts[g5.Name];
        var currentG5 = picked.Count(n => n >= g5.From && n <= g5.To);
        if (mode.Equals("NORMAL", StringComparison.OrdinalIgnoreCase))
        {
            var cap = g5Count >= 5 ? 3 : g5Count == 4 ? 4 : int.MaxValue;
            while (currentG5 > cap)
            {
                var rem = picked.Last(n => n >= g5.From && n <= g5.To);
                picked.Remove(rem);
                picked.Add(Enumerable.Range(game.RangeMin, game.RangeMax - game.RangeMin + 1).First(n => !picked.Contains(n) && (n < g5.From || n > g5.To)));
                currentG5--;
            }
        }
        else
        {
            var min = g5Count <= 2 ? 3 : 0;
            while (currentG5 < min)
            {
                var add = Enumerable.Range(g5.From, g5.To - g5.From + 1).First(n => !picked.Contains(n));
                picked.Add(add);
                var rem = picked.First(n => n < g5.From || n > g5.To);
                picked.Remove(rem);
                currentG5++;
            }
        }
    }
}
