namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que corresponde a dias específicos da semana.
/// Exemplo: Todas as segundas e quartas-feiras.
/// </summary>
public class DayOfWeekExpression : ITemporalExpression
{
    private readonly HashSet<DayOfWeek> _daysOfWeek;

    public DayOfWeekExpression(params DayOfWeek[] daysOfWeek)
    {
        if (daysOfWeek == null || daysOfWeek.Length == 0)
            throw new ArgumentException("Pelo menos um dia da semana deve ser especificado.");

        _daysOfWeek = new HashSet<DayOfWeek>(daysOfWeek);
    }

    public bool Includes(DateOnly date)
    {
        return _daysOfWeek.Contains(date.DayOfWeek);
    }
}
