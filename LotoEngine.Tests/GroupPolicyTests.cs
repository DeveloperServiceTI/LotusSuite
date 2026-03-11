using FluentAssertions;
using LotoEngine.Core.Analysis;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Generation;

namespace LotoEngine.Tests;

public class GroupPolicyTests
{
    [Fact]
    public void NormalMode_ShouldCapG5_WhenLastWasStrong()
    {
        var def = new LotofacilDefinition();
        var draws = Enumerable.Range(1, 21)
            .Select(i => new Draw(i, DateTime.Today.AddDays(i), new[] { 1,2,3,4,5,6,7,8,9,10,11,20,21,22,23 }))
            .ToList();
        var analysis = new RecipeAnalyzer().Analyze(draws, def, 21);
        var ticket = new TicketGenerator().Generate(draws, def, analysis, new GenerationOptions(1, "NORMAL", 1)).Single();
        ticket.NumbersSorted.Count(n => n >= 20).Should().BeLessOrEqualTo(4);
    }
}
