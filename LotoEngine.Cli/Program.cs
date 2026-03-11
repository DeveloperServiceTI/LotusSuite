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

return await root.InvokeAsync(args);

static GameType ParseGame(string raw) => raw.ToLowerInvariant() switch
{
    "lotofacil" => GameType.Lotofacil,
    "megasena" or "mega-sena" => GameType.MegaSena,
    "diadesorte" or "dia-de-sorte" => GameType.DiaDeSorte,
    _ => throw new ArgumentException($"Jogo inválido: {raw}")
};
