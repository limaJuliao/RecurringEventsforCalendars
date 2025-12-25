namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que corresponde a dias específicos do mês.
/// Exemplo: Dia 1 e 15 de cada mês.
/// </summary>
public class DayOfMonthExpression : ITemporalExpression
{
    private readonly HashSet<int> _daysOfMonth;

    public DayOfMonthExpression(params int[] daysOfMonth)
    {
        if (daysOfMonth == null || daysOfMonth.Length == 0)
            throw new ArgumentException("Pelo menos um dia do mês deve ser especificado.");

        if (daysOfMonth.Any(d => d < 1 || d > 31))
            throw new ArgumentException("Os dias do mês devem estar entre 1 e 31.");

        _daysOfMonth = new HashSet<int>(daysOfMonth);
    }

    public bool Includes(DateOnly date)
    {
        return _daysOfMonth.Contains(date.Day);
    }
}
