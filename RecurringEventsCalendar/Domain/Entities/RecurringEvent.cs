using RecurringEventsCalendar.Domain.ValueObjects;

namespace RecurringEventsCalendar.Domain.Entities;

/// <summary>
/// Evento recorrente - usa uma Temporal Expression para definir quando ocorre.
/// Implementa o padrão do Martin Fowler: não armazena datas, armazena REGRAS.
/// </summary>
public class RecurringEvent : EventBase
{
    public ITemporalExpression RecurrenceRule { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public RecurringEvent(
        string title,
        string description,
        TimeOnly startTime,
        TimeOnly endTime,
        ITemporalExpression recurrenceRule,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
        : base(title, description, startTime, endTime)
    {
        RecurrenceRule = recurrenceRule ?? throw new ArgumentNullException(nameof(recurrenceRule));
        StartDate = startDate;
        EndDate = endDate;

        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            throw new ArgumentException("A data de término deve ser posterior à data de início.");
    }

    public override bool OccursOn(DateOnly date)
    {
        // Verifica se está dentro do período válido
        if (StartDate.HasValue && date < StartDate.Value)
            return false;

        if (EndDate.HasValue && date > EndDate.Value)
            return false;

        // Delega a verificação à regra de recorrência
        return RecurrenceRule.Includes(date);
    }
}
