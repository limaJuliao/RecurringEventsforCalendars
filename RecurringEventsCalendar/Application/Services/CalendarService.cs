using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;

namespace RecurringEventsCalendar.Application.Services;

/// <summary>
/// Serviço de aplicação que implementa o algoritmo do Martin Fowler
/// para consultar eventos considerando regras de recorrência e exceções.
/// 
/// Algoritmo "Going Further":
/// 1. Busca eventos recorrentes que correspondem à data
/// 2. Verifica se há exceções (exclusões) para essa data
/// 3. Adiciona eventos únicos (substituições) marcados para essa data
/// </summary>
public class CalendarService
{
    private readonly IEventRepository _repository;

    public CalendarService(IEventRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Obtém todos os eventos que ocorrem em uma data específica,
    /// aplicando as regras de recorrência e exceções.
    /// </summary>
    public IEnumerable<CalendarEventDto> GetEventsForDate(DateOnly date)
    {
        var events = new List<CalendarEventDto>();

        // Passo 1: Obter exceções (exclusões) para esta data
        var exceptionsForDate = _repository.GetExceptionsForDate(date).ToHashSet();

        // Passo 2: Buscar eventos recorrentes que correspondem à regra
        var recurringEvents = _repository.GetAllRecurringEvents()
            .Where(e => e.OccursOn(date));

        foreach (var recurringEvent in recurringEvents)
        {
            // Passo 3: Verificar se há exceção para este evento nesta data
            var hasException = exceptionsForDate.Any(ex => ex.RecurringEventId == recurringEvent.Id);

            if (!hasException)
            {
                events.Add(new CalendarEventDto(
                    recurringEvent.Id,
                    recurringEvent.Title,
                    recurringEvent.Description,
                    date,
                    recurringEvent.StartTime,
                    recurringEvent.EndTime,
                    IsRecurring: true
                ));
            }
        }

        // Passo 4: Adicionar eventos únicos (substituições) para esta data
        var oneTimeEvents = _repository.GetAllOneTimeEvents()
            .Where(e => e.OccursOn(date));

        foreach (var oneTimeEvent in oneTimeEvents)
        {
            events.Add(new CalendarEventDto(
                oneTimeEvent.Id,
                oneTimeEvent.Title,
                oneTimeEvent.Description,
                date,
                oneTimeEvent.StartTime,
                oneTimeEvent.EndTime,
                IsRecurring: false
            ));
        }

        return events.OrderBy(e => e.StartTime);
    }

    /// <summary>
    /// Obtém eventos para um intervalo de datas.
    /// </summary>
    public IEnumerable<CalendarEventDto> GetEventsForDateRange(DateOnly startDate, DateOnly endDate)
    {
        var events = new List<CalendarEventDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            events.AddRange(GetEventsForDate(date));
        }

        return events;
    }

    /// <summary>
    /// Move uma instância específica de um evento recorrente para outra data.
    /// Implementa o padrão "Going Further": cria uma exceção + evento de substituição.
    /// </summary>
    public void MoveRecurringEventInstance(
        Guid recurringEventId,
        DateOnly originalDate,
        DateOnly newDate,
        TimeOnly? newStartTime = null,
        TimeOnly? newEndTime = null)
    {
        var recurringEvent = _repository.GetAllRecurringEvents()
            .FirstOrDefault(e => e.Id == recurringEventId)
            ?? throw new ArgumentException("Evento recorrente não encontrado.");

        if (!recurringEvent.OccursOn(originalDate))
            throw new ArgumentException("O evento recorrente não ocorre na data original especificada.");

        // 1. Criar exceção para a data original (exclusão)
        var exception = new EventException(
            recurringEventId,
            originalDate,
            $"Movido para {newDate:dd/MM/yyyy}"
        );
        _repository.AddException(exception);

        // 2. Criar evento único na nova data (substituição)
        var movedEvent = new OneTimeEvent(
            recurringEvent.Title,
            recurringEvent.Description + " (Remarcado)",
            newDate,
            newStartTime ?? recurringEvent.StartTime,
            newEndTime ?? recurringEvent.EndTime
        );
        _repository.AddOneTimeEvent(movedEvent);
    }

    /// <summary>
    /// Cancela uma instância específica de um evento recorrente.
    /// </summary>
    public void CancelRecurringEventInstance(Guid recurringEventId, DateOnly date, string reason = "")
    {
        var recurringEvent = _repository.GetAllRecurringEvents()
            .FirstOrDefault(e => e.Id == recurringEventId)
            ?? throw new ArgumentException("Evento recorrente não encontrado.");

        if (!recurringEvent.OccursOn(date))
            throw new ArgumentException("O evento recorrente não ocorre na data especificada.");

        var exception = new EventException(recurringEventId, date, reason);
        _repository.AddException(exception);
    }
}
