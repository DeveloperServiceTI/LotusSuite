using LotoEngine.Core.Domain;

namespace LotoEngine.Core.Validation;

public sealed class TicketValidator
{
    public ValidationResult Validate(Ticket ticket, Draw draw)
    {
        var hits = ticket.NumbersSorted.Intersect(draw.NumbersSorted).Count();
        var monthHit = string.Equals(ticket.Extra, draw.Extra, StringComparison.OrdinalIgnoreCase);
        return new ValidationResult(ticket, hits, monthHit);
    }

    public IReadOnlyList<ValidationResult> ValidateMany(IEnumerable<Ticket> tickets, Draw draw) => tickets.Select(t => Validate(t, draw)).ToList();
}
