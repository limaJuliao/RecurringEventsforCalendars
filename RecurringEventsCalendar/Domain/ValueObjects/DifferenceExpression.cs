namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que exclui (NOT) outra expressão.
/// Exemplo: Todos os dias EXCETO finais de semana.
/// </summary>
public class DifferenceExpression : ITemporalExpression
{
    private readonly ITemporalExpression _included;
    private readonly ITemporalExpression _excluded;

    public DifferenceExpression(ITemporalExpression included, ITemporalExpression excluded)
    {
        _included = included ?? throw new ArgumentNullException(nameof(included));
        _excluded = excluded ?? throw new ArgumentNullException(nameof(excluded));
    }

    public bool Includes(DateOnly date)
    {
        return _included.Includes(date) && !_excluded.Includes(date);
    }
}
