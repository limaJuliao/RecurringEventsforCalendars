using RecurringEventsCalendar.Domain.Entities;

namespace RecurringEventsCalendar.Application.Services;

/// <summary>
/// DTO para representar um evento no calendário.
/// Separa a camada de domínio da apresentação (Clean Architecture).
/// </summary>
public record CalendarEventDto(
    Guid Id,
    string Title,
    string Description,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsRecurring,
    bool IsException = false
);
