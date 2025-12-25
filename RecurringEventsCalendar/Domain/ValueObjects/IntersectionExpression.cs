namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Expressão temporal que faz a interseção (AND) de múltiplas expressões.
/// Exemplo: Segunda-feira E dia 1 do mês (apenas segundas-feiras que caem no dia 1).
/// </summary>
public class IntersectionExpression : ITemporalExpression
{
    private readonly IEnumerable<ITemporalExpression> _expressions;

    public IntersectionExpression(params ITemporalExpression[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("Pelo menos uma expressão deve ser fornecida.");

        _expressions = expressions;
    }

    public bool Includes(DateOnly date)
    {
        return _expressions.All(expr => expr.Includes(date));
    }
}
