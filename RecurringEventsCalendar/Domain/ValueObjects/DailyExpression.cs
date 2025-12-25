namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que corresponde todos os dias.
/// Útil para eventos diários.
/// </summary>
public class DailyExpression : ITemporalExpression
{
    public bool Includes(DateOnly date) => true;
}
