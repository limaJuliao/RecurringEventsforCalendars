namespace RecurringEventsCalendar.Domain.Entities;

/// <summary>
/// Evento único que ocorre em uma data específica.
/// Usado para substituições quando uma instância de evento recorrente é movida.
/// </summary>
public class OneTimeEvent : EventBase
{
    public DateOnly Date { get; private set; }

    public OneTimeEvent(
        string title,
        string description,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
        : base(title, description, startTime, endTime)
    {
        Date = date;
    }

    public override bool OccursOn(DateOnly date) => Date == date;
}
