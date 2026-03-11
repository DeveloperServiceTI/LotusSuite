using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Utils;

namespace LotoEngine.Core.Analysis;

public sealed class RecipeAnalyzer
{
    public RecipeAnalysisResult Analyze(IReadOnlyList<Draw> draws, IGameDefinition game, int last = 21, int innerWindow = 7)
    {
        var last21 = draws.TakeLast(last).ToArray();
        if (last21.Length < 2) throw new ArgumentException("Need at least 2 draws.");
        var transitions = new List<TransitionResult>();
        for (var i = 0; i < last21.Length - 1; i++)
        {
            var a = last21[i].NumbersSorted;
            var b = last21[i + 1].NumbersSorted;
            transitions.Add(new TransitionResult(i + 1, SetOps.Intersect(a, b), SetOps.Except(b, a), SetOps.Except(a, b)));
        }

        var windows = BuildWindows(last21, innerWindow);
        var runs = last21.ToDictionary(x => x.ContestId, x => new RunsProfile(RunsCalculator.Analyze(x.NumbersSorted).MaxRun, RunsCalculator.Analyze(x.NumbersSorted).RunsGe3));
        var medianMaxRun = runs.Values.Select(x => (double)x.MaxRun).OrderBy(x => x).Skip(runs.Count / 2).FirstOrDefault();
        var targetMaxRun = (int)Math.Round(medianMaxRun, MidpointRounding.AwayFromZero);
        var averageRuns = runs.Values.Average(x => (double)x.RunsGe3);
        var targetRuns = (int)Math.Round(averageRuns, MidpointRounding.AwayFromZero);
        var medianRepeats = transitions.Select(t => (double)t.RepeatCount).OrderBy(x => x).Skip(transitions.Count / 2).First();
        var dynamicAnchors = Math.Clamp((int)Math.Round(medianRepeats, MidpointRounding.AwayFromZero), 8, 12);

        var lastDraw = last21.Last();
        var groupCounts = game.Groups.ToDictionary(g => g.Name, g => lastDraw.NumbersSorted.Count(n => n >= g.From && n <= g.To));

        var stats = new List<NumberStats>();
        for (var n = game.RangeMin; n <= game.RangeMax; n++)
        {
            var seen = 0; var stay = 0; var missing = 0; var enter = 0;
            for (var i = 0; i < last21.Length - 1; i++)
            {
                var inA = last21[i].NumbersSorted.Contains(n);
                var inB = last21[i + 1].NumbersSorted.Contains(n);
                if (inA) { seen++; if (inB) stay++; }
                else { missing++; if (inB) enter++; }
            }

            var presence = windows.Count(w => w.Contains(n));
            var hot7 = last21.TakeLast(Math.Min(7, last21.Length)).Count(d => d.NumbersSorted.Contains(n));
            stats.Add(new NumberStats(
                n,
                seen,
                stay,
                seen == 0 ? 0 : (double)stay / seen,
                missing,
                enter,
                missing == 0 ? 0 : (double)enter / missing,
                presence,
                hot7,
                hot7 / 7d,
                hot7 == 0 ? 1 : 0));
        }

        return new RecipeAnalysisResult(transitions, stats, runs, targetMaxRun, targetRuns, dynamicAnchors, groupCounts);
    }

    private static List<HashSet<int>> BuildWindows(IReadOnlyList<Draw> draws, int size)
    {
        var windows = new List<HashSet<int>>();
        if (draws.Count < size) return windows;
        for (var i = 0; i <= draws.Count - size; i++)
        {
            windows.Add(draws.Skip(i).Take(size).SelectMany(x => x.NumbersSorted).ToHashSet());
        }
        return windows;
    }
}
