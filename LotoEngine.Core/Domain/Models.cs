namespace LotoEngine.Core.Domain;

public enum GameType { Lotofacil, MegaSena, DiaDeSorte }

public sealed record Draw(int ContestId, DateTime Date, int[] NumbersSorted, string? Extra = null);

public sealed record Ticket(
    long TicketId,
    GameType Game,
    int[] NumbersSorted,
    string? Extra,
    string Tag,
    string Comment);

public sealed record GroupRange(string Name, int From, int To);

public sealed record TransitionResult(int Index, IReadOnlyCollection<int> Repeats, IReadOnlyCollection<int> Enters, IReadOnlyCollection<int> Exits)
{
    public int RepeatCount => Repeats.Count;
    public int EnterCount => Enters.Count;
    public int ExitCount => Exits.Count;
}

public sealed record NumberStats(int Number, int Seen, int Stay, double StayRate, int Missing, int Enter, double EnterRate, int Presence, int Hot7, double Hot7Norm, int Cold7Bonus);

public sealed record RunsProfile(int MaxRun, int RunsGe3);

public sealed record RecipeAnalysisResult(
    IReadOnlyList<TransitionResult> Transitions,
    IReadOnlyList<NumberStats> NumberStats,
    IReadOnlyDictionary<int, RunsProfile> RunsPerContest,
    int TargetMaxRun,
    int TargetRunsGe3,
    int DynamicAnchorsK,
    IReadOnlyDictionary<string, int> LastGroupCounts);

public sealed record GenerationOptions(int Count, string Mode, int? StartMin, bool GroupPolicyDynamic = true, bool AvoidHistoryDuplicates = false);

public sealed record ValidationResult(Ticket Ticket, int Hits, bool MonthHit);

public sealed record BacktestStepResult(int ContestId, Dictionary<int, ValidationResult> StartResults, int BestOf3);

public sealed record BacktestSummary(double AvgStart1, double AvgStart2, double AvgStart3, double ProbThresholdStart1, double ProbThresholdStart2, double ProbThresholdStart3, double ProbThresholdBestOf3);
