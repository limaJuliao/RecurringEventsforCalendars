namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que faz a união (OR) de múltiplas expressões.
/// Exemplo: Segunda-feira OU Quarta-feira.
/// </summary>
public class UnionExpression : ITemporalExpression
{
    private readonly IEnumerable<ITemporalExpression> _expressions;

    public UnionExpression(params ITemporalExpression[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("Pelo menos uma expressão deve ser fornecida.");

        _expressions = expressions;
    }

    public bool Includes(DateOnly date)
    {
        return _expressions.Any(expr => expr.Includes(date));
    }
}
