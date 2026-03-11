using System.CommandLine;
using System.Text.Json;
using LotoEngine.Core.Analysis;
using LotoEngine.Core.Backtest;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;
using LotoEngine.Core.Generation;
using LotoEngine.Core.Validation;
using LotoEngine.Infrastructure.IO;
using LotoEngine.Infrastructure.Reports;

var gameOpt = new Option<string>("--game", () => "lotofacil");
var fileOpt = new Option<string>("--file") { IsRequired = true };
var lastOpt = new Option<int>("--last", () => 21);
var countOpt = new Option<int>("--count", () => 21);
var modeOpt = new Option<string>("--mode", () => "normal");
var startMinOpt = new Option<int?>("--startMin");
var ticketsOpt = new Option<string>("--tickets", () => "output/tickets.json");
var resultOpt = new Option<string>("--result") { IsRequired = true };
var backOpt = new Option<int>("--back", () => 210);
var windowOpt = new Option<int>("--window", () => 21);
var startsOpt = new Option<string>("--starts", () => "1,2,3");
var outputOpt = new Option<string>("--output", () => "output");

var root = new RootCommand("Loto Engine 7x21");

var analyze = new Command("analyze");
analyze.AddOption(gameOpt);
analyze.AddOption(fileOpt);
analyze.AddOption(lastOpt);
analyze.SetHandler((string gameRaw, string file, int last) =>
{
    var game = ParseGame(gameRaw);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(file, def);
    var result = new RecipeAnalyzer().Analyze(draws, def, last);
    Console.WriteLine("Transições R/E/S");
    foreach (var t in result.Transitions) Console.WriteLine($"T{t.Index}: R={t.RepeatCount} E={t.EnterCount} S={t.ExitCount}");
    Console.WriteLine("Top StayRate");
    foreach (var s in result.NumberStats.OrderByDescending(x => x.StayRate).Take(10)) Console.WriteLine($"{s.Number:D2} stay={s.StayRate:F2} presence={s.Presence}");
    Console.WriteLine("Top EnterRate");
    foreach (var s in result.NumberStats.OrderByDescending(x => x.EnterRate).Take(10)) Console.WriteLine($"{s.Number:D2} enter={s.EnterRate:F2} presence={s.Presence}");
    Console.WriteLine($"Runs alvo: MaxRun={result.TargetMaxRun}; RunsGe3={result.TargetRunsGe3}");
}, gameOpt, fileOpt, lastOpt);
root.AddCommand(analyze);

var generate = new Command("generate");
generate.AddOption(gameOpt);
generate.AddOption(fileOpt);
generate.AddOption(lastOpt);
generate.AddOption(countOpt);
generate.AddOption(modeOpt);
generate.AddOption(startMinOpt);
generate.AddOption(outputOpt);
generate.SetHandler((string gameRaw, string file, int last, int count, string mode, int? startMin, string output) =>
{
    var game = ParseGame(gameRaw);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(file, def);
    var analysis = new RecipeAnalyzer().Analyze(draws, def, last);
    var tickets = new TicketGenerator().Generate(draws.TakeLast(last).ToList(), def, analysis, new GenerationOptions(count, mode, startMin));
    var writer = new ReportWriter();
    writer.WriteJson(tickets, Path.Combine(output, "tickets.json"));
    writer.WriteTicketsXlsx(tickets, Path.Combine(output, "tickets.xlsx"));
    Console.WriteLine($"Geradas {tickets.Count} cartelas em {output}");
}, gameOpt, fileOpt, lastOpt, countOpt, modeOpt, startMinOpt, outputOpt);
root.AddCommand(generate);

var validate = new Command("validate");
validate.AddOption(gameOpt);
validate.AddOption(resultOpt);
validate.AddOption(ticketsOpt);
validate.AddOption(outputOpt);
validate.SetHandler((string gameRaw, string resultRaw, string ticketsFile, string output) =>
{
    var game = ParseGame(gameRaw);
    _ = GameDefinitions.For(game);
    var nums = resultRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).OrderBy(x => x).ToArray();
    var draw = new Draw(0, DateTime.Today, nums, null);
    var tickets = JsonSerializer.Deserialize<List<Ticket>>(File.ReadAllText(ticketsFile)) ?? new List<Ticket>();
    var results = new TicketValidator().ValidateMany(tickets, draw);
    var threshold = game switch { GameType.Lotofacil => 11, GameType.MegaSena => 4, _ => 5 };
    new ReportWriter().WriteValidationXlsx(results, draw, threshold, Path.Combine(output, "validation.xlsx"));
    Console.WriteLine($"Best hit: {results.DefaultIfEmpty(new ValidationResult(new Ticket(0, game, Array.Empty<int>(), null, "", ""), 0, false)).Max(x => x.Hits)}");
}, gameOpt, resultOpt, ticketsOpt, outputOpt);
root.AddCommand(validate);

var backtest = new Command("backtest");
backtest.AddOption(gameOpt);
backtest.AddOption(fileOpt);
backtest.AddOption(backOpt);
backtest.AddOption(windowOpt);
backtest.AddOption(startsOpt);
backtest.AddOption(outputOpt);
backtest.SetHandler((string gameRaw, string file, int back, int window, string startsRaw, string output) =>
{
    var game = ParseGame(gameRaw);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(file, def);
    var threshold = game switch { GameType.Lotofacil => 11, GameType.MegaSena => 4, _ => 5 };
    var starts = startsRaw.Split(',').Select(int.Parse).ToArray();
    var run = new WalkForwardBacktestEngine().Run(draws, def, back, window, starts, threshold);
    new ReportWriter().WriteJson(run, Path.Combine(output, "backtest.json"));
    Console.WriteLine($"Backtest passos={run.Steps.Count} P(best>={threshold})={run.Summary.ProbThresholdBestOf3:F3}");
}, gameOpt, fileOpt, backOpt, windowOpt, startsOpt, outputOpt);
root.AddCommand(backtest);

var wfTicketCount = new Option<int>("--ticketCount", () => 6);
var wfMinHits = new Option<int>("--minHits", () => 13);
var wfMaxRetries = new Option<int>("--maxRetries", () => 2);
var wfStartMode = new Option<string>("--startMode", () => "FromBeginning");
var wfLastN = new Option<int?>("--lastN");
var wfStartContestId = new Option<int?>("--startContestId");
var wfPolicy = new Option<string>("--policy", () => "Retry");

var walkForwardLotofacil = new Command("walkforward-lotofacil");
walkForwardLotofacil.AddOption(fileOpt);
walkForwardLotofacil.AddOption(outputOpt);
walkForwardLotofacil.AddOption(windowOpt);
walkForwardLotofacil.AddOption(wfTicketCount);
walkForwardLotofacil.AddOption(wfMinHits);
walkForwardLotofacil.AddOption(wfMaxRetries);
walkForwardLotofacil.AddOption(wfStartMode);
walkForwardLotofacil.AddOption(wfLastN);
walkForwardLotofacil.AddOption(wfStartContestId);
walkForwardLotofacil.AddOption(wfPolicy);
walkForwardLotofacil.SetHandler(ctx =>
{
    var file = ctx.ParseResult.GetValueForOption(fileOpt)!;
    var output = ctx.ParseResult.GetValueForOption(outputOpt)!;
    var window = ctx.ParseResult.GetValueForOption(windowOpt);
    var ticketCount = ctx.ParseResult.GetValueForOption(wfTicketCount);
    var minHits = ctx.ParseResult.GetValueForOption(wfMinHits);
    var maxRetries = ctx.ParseResult.GetValueForOption(wfMaxRetries);
    var startMode = ctx.ParseResult.GetValueForOption(wfStartMode)!;
    var lastN = ctx.ParseResult.GetValueForOption(wfLastN);
    var startContestId = ctx.ParseResult.GetValueForOption(wfStartContestId);
    var policy = ctx.ParseResult.GetValueForOption(wfPolicy)!;

    var def = new LotofacilDefinition();
    var draws = new DrawFileReader().Read(file, def).OrderBy(d => d.ContestId).ToList();

    var historico = draws
        .Select(d => new LotoHistoricInfo(d.ContestId, d.Date, d.NumbersSorted.ToHashSet(), false))
        .ToList();

    var runner = new LotofacilWalkForwardRunner();
    var start = Enum.Parse<LotofacilWalkForwardRunner.StartMode>(startMode, true);
    var failPolicy = Enum.Parse<LotofacilWalkForwardRunner.FailPolicy>(policy, true);

    var config = new LotofacilWalkForwardRunner.Config(
        WindowSize: window,
        TicketSize: 15,
        MinHits: minHits,
        MaxRetries: maxRetries,
        Start: start,
        LastN: lastN,
        StartContestId: startContestId,
        Policy: failPolicy,
        ExportJsonPath: Path.Combine(output, "walkforward-lotofacil.json"));

    IEnumerable<HashSet<int>> Generator(List<LotoHistoricInfo> treino, int ticketSize)
    {
        var last = treino.Last().Numeros;
        var all = treino.SelectMany(t => t.Numeros)
            .GroupBy(n => n)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        var tickets = new List<HashSet<int>>();
        for (var i = 0; i < ticketCount; i++)
        {
            var t = new HashSet<int>(last.Take(Math.Min(10, ticketSize)));
            foreach (var n in all.Skip(i).Concat(all.Take(i)))
            {
                if (t.Count >= ticketSize) break;
                t.Add(n);
            }
            while (t.Count < ticketSize)
            {
                var next = Enumerable.Range(1, 25).First(n => !t.Contains(n));
                t.Add(next);
            }
            tickets.Add(t);
        }
        return tickets;
    }

    var run = runner.Run(historico, Generator, config);
    Console.WriteLine($"WalkForward concluído. Rounds={run.TotalRounds} PassRate={run.PassRate:P2}");
});
root.AddCommand(walkForwardLotofacil);

return await root.InvokeAsync(args);

static GameType ParseGame(string raw) => raw.ToLowerInvariant() switch
{
    "lotofacil" => GameType.Lotofacil,
    "megasena" or "mega-sena" => GameType.MegaSena,
    "diadesorte" or "dia-de-sorte" => GameType.DiaDeSorte,
    _ => throw new ArgumentException($"Jogo inválido: {raw}")
};
