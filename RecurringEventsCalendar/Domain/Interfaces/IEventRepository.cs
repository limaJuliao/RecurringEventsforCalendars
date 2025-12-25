using RecurringEventsCalendar.Domain.Entities;

namespace RecurringEventsCalendar.Domain.Interfaces;

/// <summary>
/// Interface para repositório de eventos seguindo Dependency Inversion Principle (SOLID).
/// </summary>
public interface IEventRepository
{
    void AddRecurringEvent(RecurringEvent recurringEvent);
    void AddOneTimeEvent(OneTimeEvent oneTimeEvent);
    void AddException(EventException exception);
    
    IEnumerable<RecurringEvent> GetAllRecurringEvents();
    IEnumerable<OneTimeEvent> GetAllOneTimeEvents();
    IEnumerable<EventException> GetExceptionsForEvent(Guid recurringEventId);
    IEnumerable<EventException> GetExceptionsForDate(DateOnly date);
}
