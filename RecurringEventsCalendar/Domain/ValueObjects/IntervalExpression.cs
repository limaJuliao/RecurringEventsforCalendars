namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que corresponde a um intervalo de dias.
/// Exemplo: A cada 3 dias, a cada semana (7 dias).
/// </summary>
public class IntervalExpression : ITemporalExpression
{
    private readonly DateOnly _startDate;
    private readonly int _intervalInDays;

    public IntervalExpression(DateOnly startDate, int intervalInDays)
    {
        if (intervalInDays <= 0)
            throw new ArgumentException("O intervalo deve ser maior que zero.", nameof(intervalInDays));

        _startDate = startDate;
        _intervalInDays = intervalInDays;
    }

    public bool Includes(DateOnly date)
    {
        if (date < _startDate)
            return false;

        var daysSinceStart = date.DayNumber - _startDate.DayNumber;
        return daysSinceStart % _intervalInDays == 0;
    }
}
