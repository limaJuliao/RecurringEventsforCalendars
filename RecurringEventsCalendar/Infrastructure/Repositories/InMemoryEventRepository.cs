using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;

namespace RecurringEventsCalendar.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de eventos.
/// Em produção, seria substituído por uma implementação com banco de dados.
/// Seguindo Dependency Inversion Principle (SOLID).
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly List<RecurringEvent> _recurringEvents = new();
    private readonly List<OneTimeEvent> _oneTimeEvents = new();
    private readonly List<EventException> _exceptions = new();

    public void AddRecurringEvent(RecurringEvent recurringEvent)
    {
        ArgumentNullException.ThrowIfNull(recurringEvent);
        _recurringEvents.Add(recurringEvent);
    }

    public void AddOneTimeEvent(OneTimeEvent oneTimeEvent)
    {
        ArgumentNullException.ThrowIfNull(oneTimeEvent);
        _oneTimeEvents.Add(oneTimeEvent);
    }

    public void AddException(EventException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exceptions.Add(exception);
    }

    public IEnumerable<RecurringEvent> GetAllRecurringEvents()
    {
        return _recurringEvents.AsReadOnly();
    }

    public IEnumerable<OneTimeEvent> GetAllOneTimeEvents()
    {
        return _oneTimeEvents.AsReadOnly();
    }

    public IEnumerable<EventException> GetExceptionsForEvent(Guid recurringEventId)
    {
        return _exceptions.Where(e => e.RecurringEventId == recurringEventId);
    }

    public IEnumerable<EventException> GetExceptionsForDate(DateOnly date)
    {
        return _exceptions.Where(e => e.ExceptionDate == date);
    }
}
