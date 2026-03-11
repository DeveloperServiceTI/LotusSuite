using FluentAssertions;
using LotoEngine.Core.Analysis;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Utils;
using Xunit;

namespace LotoEngine.Tests;

public class RecipeTests
{
    [Fact]
    public void SetOps_ShouldCompute_RES()
    {
        SetOps.Intersect(new[] { 1, 2, 3 }, new[] { 2, 3, 4 }).Should().Equal(2, 3);
        SetOps.Except(new[] { 2, 3, 4 }, new[] { 1, 2, 3 }).Should().Equal(4);
        SetOps.Except(new[] { 1, 2, 3 }, new[] { 2, 3, 4 }).Should().Equal(1);
    }

    [Fact]
    public void RunsCalculator_ShouldComputeMetrics()
    {
        var (max, ge3) = RunsCalculator.Analyze(new[] { 1, 2, 3, 7, 8, 9, 10, 15 });
        max.Should().Be(4);
        ge3.Should().Be(2);
    }

    [Fact]
    public void Analyzer_ShouldComputeStayEnterPresence()
    {
        var def = new LotofacilDefinition();
        var draws = new List<Draw>();
        for (var i = 0; i < 21; i++)
            draws.Add(new Draw(i + 1, DateTime.Today.AddDays(i), Enumerable.Range(1 + (i % 2), 15).ToArray()));

        var res = new RecipeAnalyzer().Analyze(draws, def, 21);
        res.Transitions.Should().HaveCount(20);
        res.NumberStats.Should().Contain(x => x.Number == 2 && x.StayRate >= 0);
        res.NumberStats.Should().Contain(x => x.Presence >= 0);
    }
}
