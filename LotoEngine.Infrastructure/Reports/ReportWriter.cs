using System.Text.Json;
using ClosedXML.Excel;
using LotoEngine.Core.Domain;

namespace LotoEngine.Infrastructure.Reports;

public sealed class ReportWriter
{
    public void WriteJson<T>(T data, string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void WriteTicketsXlsx(IReadOnlyList<Ticket> tickets, string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Tickets");
        ws.Cell(1, 1).Value = "TicketId"; ws.Cell(1, 2).Value = "Tag"; ws.Cell(1, 3).Value = "Numbers"; ws.Cell(1, 4).Value = "Extra"; ws.Cell(1, 5).Value = "Comment";
        for (var i = 0; i < tickets.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = tickets[i].TicketId;
            ws.Cell(i + 2, 2).Value = tickets[i].Tag;
            ws.Cell(i + 2, 3).Value = string.Join(' ', tickets[i].NumbersSorted.Select(x => x.ToString("D2")));
            ws.Cell(i + 2, 4).Value = tickets[i].Extra;
            ws.Cell(i + 2, 5).Value = tickets[i].Comment;
        }
        wb.SaveAs(file);
    }

    public void WriteValidationXlsx(IReadOnlyList<ValidationResult> results, Draw draw, int threshold, string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Validação");
        ws.Cell(1, 1).Value = "Resultado";
        ws.Cell(1, 2).Value = string.Join(' ', draw.NumbersSorted.Select(n => n.ToString("D2")));
        ws.Cell(1, 3).Value = draw.Extra;
        ws.Cell(3, 1).Value = "TicketId"; ws.Cell(3, 2).Value = "Tag"; ws.Cell(3, 3).Value = "Números"; ws.Cell(3, 4).Value = "Acertos"; ws.Cell(3, 5).Value = $">={threshold}"; ws.Cell(3, 6).Value = "Mês OK"; ws.Cell(3, 7).Value = "Comentário";
        for (var i = 0; i < results.Count; i++)
        {
            ws.Cell(i + 4, 1).Value = results[i].Ticket.TicketId;
            ws.Cell(i + 4, 2).Value = results[i].Ticket.Tag;
            ws.Cell(i + 4, 3).Value = string.Join(' ', results[i].Ticket.NumbersSorted.Select(n => n.ToString("D2")));
            ws.Cell(i + 4, 4).Value = results[i].Hits;
            ws.Cell(i + 4, 5).FormulaA1 = $"D{i + 4}>={threshold}";
            ws.Cell(i + 4, 6).Value = results[i].MonthHit;
            ws.Cell(i + 4, 7).Value = results[i].Ticket.Comment;
        }
        wb.SaveAs(file);
    }
}
