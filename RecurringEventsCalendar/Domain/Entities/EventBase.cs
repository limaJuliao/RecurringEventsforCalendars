namespace RecurringEventsCalendar.Domain.Entities;

/// <summary>
/// Classe base para todos os eventos. Seguindo SRP (Single Responsibility Principle).
/// </summary>
public abstract class EventBase
{
    public Guid Id { get; protected set; }
    public string Title { get; protected set; }
    public string Description { get; protected set; }
    public TimeOnly StartTime { get; protected set; }
    public TimeOnly EndTime { get; protected set; }

    protected EventBase(string title, string description, TimeOnly startTime, TimeOnly endTime)
    {
        Id = Guid.NewGuid();
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? string.Empty;
        StartTime = startTime;
        EndTime = endTime;

        if (endTime <= startTime)
            throw new ArgumentException("O horário de término deve ser posterior ao de início.");
    }

    /// <summary>
    /// Determina se este evento ocorre na data especificada.
    /// </summary>
    public abstract bool OccursOn(DateOnly date);
}
