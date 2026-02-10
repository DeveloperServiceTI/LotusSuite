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
var fileOpt = new Option<string>("--file");
var lastOpt = new Option<int>("--last", () => 21);
var countOpt = new Option<int>("--count", () => 21);
var modeOpt = new Option<string>("--mode", () => "normal");
var startMinOpt = new Option<int?>("--startMin");
var ticketsOpt = new Option<string>("--tickets", () => "output/tickets.json");
var resultOpt = new Option<string>("--result");
var backOpt = new Option<int>("--back", () => 210);
var windowOpt = new Option<int>("--window", () => 21);
var startsOpt = new Option<string>("--starts", () => "1,2,3");
var outputOpt = new Option<string>("--output", () => "output");

var root = new RootCommand("Loto Engine 7x21");

root.AddCommand(new Command("analyze")
{
    gameOpt, fileOpt, lastOpt
}.WithHandler(async ctx =>
{
    var game = ParseGame(gameOpt.GetValue(ctx.ParseResult)!);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(fileOpt.GetValue(ctx.ParseResult)!, def);
    var result = new RecipeAnalyzer().Analyze(draws, def, lastOpt.GetValue(ctx.ParseResult));
    Console.WriteLine("Transições R/E/S");
    foreach (var t in result.Transitions) Console.WriteLine($"T{t.Index}: R={t.RepeatCount} E={t.EnterCount} S={t.ExitCount}");
    Console.WriteLine("Top StayRate");
    foreach (var s in result.NumberStats.OrderByDescending(x => x.StayRate).Take(10)) Console.WriteLine($"{s.Number:D2} stay={s.StayRate:F2} presence={s.Presence}");
    Console.WriteLine("Top EnterRate");
    foreach (var s in result.NumberStats.OrderByDescending(x => x.EnterRate).Take(10)) Console.WriteLine($"{s.Number:D2} enter={s.EnterRate:F2} presence={s.Presence}");
    Console.WriteLine($"Runs alvo: MaxRun={result.TargetMaxRun}; RunsGe3={result.TargetRunsGe3}");
    await Task.CompletedTask;
}));

root.AddCommand(new Command("generate")
{
    gameOpt, fileOpt, lastOpt, countOpt, modeOpt, startMinOpt, outputOpt
}.WithHandler(async ctx =>
{
    var game = ParseGame(gameOpt.GetValue(ctx.ParseResult)!);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(fileOpt.GetValue(ctx.ParseResult)!, def);
    var analysis = new RecipeAnalyzer().Analyze(draws, def, lastOpt.GetValue(ctx.ParseResult));
    var tickets = new TicketGenerator().Generate(draws.TakeLast(lastOpt.GetValue(ctx.ParseResult)).ToList(), def, analysis, new GenerationOptions(countOpt.GetValue(ctx.ParseResult), modeOpt.GetValue(ctx.ParseResult)!, startMinOpt.GetValue(ctx.ParseResult)));
    var writer = new ReportWriter();
    var folder = outputOpt.GetValue(ctx.ParseResult)!;
    writer.WriteJson(tickets, Path.Combine(folder, "tickets.json"));
    writer.WriteTicketsXlsx(tickets, Path.Combine(folder, "tickets.xlsx"));
    Console.WriteLine($"Geradas {tickets.Count} cartelas em {folder}");
    await Task.CompletedTask;
}));

root.AddCommand(new Command("validate")
{
    gameOpt, resultOpt, ticketsOpt, outputOpt
}.WithHandler(async ctx =>
{
    var game = ParseGame(gameOpt.GetValue(ctx.ParseResult)!);
    var def = GameDefinitions.For(game);
    var nums = resultOpt.GetValue(ctx.ParseResult)!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).OrderBy(x => x).ToArray();
    var draw = new Draw(0, DateTime.Today, nums, null);
    var tickets = JsonSerializer.Deserialize<List<Ticket>>(File.ReadAllText(ticketsOpt.GetValue(ctx.ParseResult)!))!;
    var results = new TicketValidator().ValidateMany(tickets, draw);
    var threshold = game switch { GameType.Lotofacil => 11, GameType.MegaSena => 4, _ => 5 };
    new ReportWriter().WriteValidationXlsx(results, draw, threshold, Path.Combine(outputOpt.GetValue(ctx.ParseResult)!, "validation.xlsx"));
    Console.WriteLine($"Best hit: {results.Max(x => x.Hits)}");
    await Task.CompletedTask;
}));

root.AddCommand(new Command("backtest")
{
    gameOpt, fileOpt, backOpt, windowOpt, startsOpt, outputOpt
}.WithHandler(async ctx =>
{
    var game = ParseGame(gameOpt.GetValue(ctx.ParseResult)!);
    var def = GameDefinitions.For(game);
    var draws = new DrawFileReader().Read(fileOpt.GetValue(ctx.ParseResult)!, def);
    var threshold = game switch { GameType.Lotofacil => 11, GameType.MegaSena => 4, _ => 5 };
    var starts = startsOpt.GetValue(ctx.ParseResult)!.Split(',').Select(int.Parse).ToArray();
    var run = new WalkForwardBacktestEngine().Run(draws, def, backOpt.GetValue(ctx.ParseResult), windowOpt.GetValue(ctx.ParseResult), starts, threshold);
    var folder = outputOpt.GetValue(ctx.ParseResult)!;
    new ReportWriter().WriteJson(run, Path.Combine(folder, "backtest.json"));
    Console.WriteLine($"Backtest passos={run.Steps.Count} P(best>={threshold})={run.Summary.ProbThresholdBestOf3:F3}");
    await Task.CompletedTask;
}));

return await root.InvokeAsync(args);

static GameType ParseGame(string raw) => raw.ToLowerInvariant() switch
{
    "lotofacil" => GameType.Lotofacil,
    "megasena" or "mega-sena" => GameType.MegaSena,
    "diadesorte" or "dia-de-sorte" => GameType.DiaDeSorte,
    _ => throw new ArgumentException($"Jogo inválido: {raw}")
};
