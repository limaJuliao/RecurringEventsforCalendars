namespace RecurringEventsCalendar.Domain.ValueObjects;

/// <summary>
/// Interface que representa uma expressão temporal - o padrão core do Martin Fowler.
/// Permite verificar se uma data específica "corresponde" à regra de recorrência.
/// </summary>
public interface ITemporalExpression
{
    /// <summary>
    /// Verifica se a data fornecida corresponde a esta expressão temporal.
    /// </summary>
    bool Includes(DateOnly date);
}
