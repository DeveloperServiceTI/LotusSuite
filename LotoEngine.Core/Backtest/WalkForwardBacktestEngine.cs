using LotoEngine.Core.Analysis;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Generation;
using LotoEngine.Core.Validation;

namespace LotoEngine.Core.Backtest;

public sealed class WalkForwardBacktestEngine
{
    public (IReadOnlyList<BacktestStepResult> Steps, BacktestSummary Summary) Run(IReadOnlyList<Draw> draws, IGameDefinition game, int back = 210, int window = 21, int[]? starts = null, int threshold = 11)
    {
        starts ??= new[] { 1, 2, 3 };
        var series = draws.TakeLast(back).ToList();
        var steps = new List<BacktestStepResult>();
        var analyzer = new RecipeAnalyzer();
        var generator = new TicketGenerator();
        var validator = new TicketValidator();

        for (var i = window; i < series.Count; i++)
        {
            var hist21 = series.Skip(i - window).Take(window).ToList();
            var result = new Dictionary<int, ValidationResult>();
            foreach (var start in starts)
            {
                var analysis = analyzer.Analyze(hist21, game, window);
                var ticket = generator.Generate(hist21, game, analysis, new GenerationOptions(1, "NORMAL", start)).Single();
                result[start] = validator.Validate(ticket, series[i]);
            }
            steps.Add(new BacktestStepResult(series[i].ContestId, result, result.Values.Max(x => x.Hits)));
        }

        double Avg(int start) => steps.Average(s => s.StartResults[start].Hits);
        double Prob(int start) => steps.Count == 0 ? 0 : steps.Count(s => s.StartResults[start].Hits >= threshold) / (double)steps.Count;
        var summary = new BacktestSummary(
            starts.Contains(1) ? Avg(1) : 0,
            starts.Contains(2) ? Avg(2) : 0,
            starts.Contains(3) ? Avg(3) : 0,
            starts.Contains(1) ? Prob(1) : 0,
            starts.Contains(2) ? Prob(2) : 0,
            starts.Contains(3) ? Prob(3) : 0,
            steps.Count == 0 ? 0 : steps.Count(s => s.BestOf3 >= threshold) / (double)steps.Count);

        return (steps, summary);
    }
}
