namespace RecurringEventsCalendar.Domain.Entities;

/// <summary>
/// Representa uma exceção (exclusão) a um evento recorrente em uma data específica.
/// Parte da solução "Going Further" do Martin Fowler para tratar exceções.
/// </summary>
public class EventException
{
    public Guid Id { get; private set; }
    public Guid RecurringEventId { get; private set; }
    public DateOnly ExceptionDate { get; private set; }
    public string Reason { get; private set; }

    public EventException(Guid recurringEventId, DateOnly exceptionDate, string reason = "")
    {
        Id = Guid.NewGuid();
        RecurringEventId = recurringEventId;
        ExceptionDate = exceptionDate;
        Reason = reason ?? string.Empty;
    }
}
