using ClosedXML.Excel;
using LotoEngine.Core.Domain;
using LotoEngine.Core.Games;

namespace LotoEngine.Infrastructure.IO;

public sealed class DrawFileReader
{
    public IReadOnlyList<Draw> Read(string file, IGameDefinition game)
        => Path.GetExtension(file).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? ReadCsv(file, game) : ReadXlsx(file, game);

    private static IReadOnlyList<Draw> ReadCsv(string file, IGameDefinition game)
    {
        var lines = File.ReadAllLines(file).Skip(1);
        return lines.Select(line =>
        {
            var p = line.Split(',');
            var nums = Enumerable.Range(0, game.NumbersPerTicket).Select(i => int.Parse(p[2 + i])).OrderBy(x => x).ToArray();
            var extra = game.HasExtra ? p.Last() : null;
            return new Draw(int.Parse(p[0]), DateTime.Parse(p[1]), nums, extra);
        }).ToList();
    }

    private static IReadOnlyList<Draw> ReadXlsx(string file, IGameDefinition game)
    {
        using var wb = new XLWorkbook(file);
        var ws = wb.Worksheet(1);
        //var rows = ws.RangeUsed().RowsUsed().Skip(1);
        var rows = ws.RangeUsed().RowsUsed();
        return rows.Select(r =>
        {
            var contest = r.Cell(1).GetValue<int>();
            var dateStr = r.Cell(2).GetString();
            if (!DateTime.TryParse(dateStr, out var date))
                throw new FormatException($"Data inválida na célula: '{dateStr}'");
            var nums = Enumerable.Range(0, game.NumbersPerTicket).Select(i => r.Cell(3 + i).GetValue<int>()).OrderBy(x => x).ToArray();
            var extra = game.HasExtra ? r.Cell(3 + game.NumbersPerTicket).GetString() : null;
            return new Draw(contest, date, nums, extra);
            //var contest = r.Cell(1).GetValue<int>();
            //var date = r.Cell(2).GetDateTime();
            //var nums = Enumerable.Range(0, game.NumbersPerTicket).Select(i => r.Cell(3 + i).GetValue<int>()).OrderBy(x => x).ToArray();
            //var extra = game.HasExtra ? r.Cell(3 + game.NumbersPerTicket).GetString() : null;
            //return new Draw(contest, date, nums, extra);
        }).ToList();
    }
}
