using RecurringEventsCalendar.Application.Services;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.ValueObjects;
using RecurringEventsCalendar.Infrastructure.Repositories;

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("  RECURRING EVENTS FOR CALENDARS - Padrão Martin Fowler");
Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

// Setup - Dependency Injection manual (em produção usaria DI Container)
var repository = new InMemoryEventRepository();
var calendarService = new CalendarService(repository);

// ═══════════════════════════════════════════════════════════════
// CENÁRIO 1: Evento Semanal Simples
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("📅 CENÁRIO 1: Reunião Semanal às Sextas-feiras");
Console.WriteLine("─────────────────────────────────────────────────────────────\n");

var everyFriday = new DayOfWeekExpression(DayOfWeek.Friday);
var weeklyMeeting = new RecurringEvent(
    "Reunião de Equipe",
    "Reunião semanal de alinhamento",
    new TimeOnly(14, 0),
    new TimeOnly(15, 0),
    everyFriday,
    startDate: new DateOnly(2025, 1, 1)
);

repository.AddRecurringEvent(weeklyMeeting);

Console.WriteLine("✓ Evento criado: Reunião de Equipe (todas as sextas-feiras)\n");

// Consultar próximas semanas
var janDates = new[] { 
    new DateOnly(2025, 1, 9),  // Quinta
    new DateOnly(2025, 1, 10), // Sexta
    new DateOnly(2025, 1, 11), // Sábado
    new DateOnly(2025, 1, 17), // Sexta
};

foreach (var date in janDates)
{
    var events = calendarService.GetEventsForDate(date);
    Console.WriteLine($"{date:dd/MM/yyyy (dddd)}: {(events.Any() ? "✓ " + events.First().Title : "Sem eventos")}");
}

// ═══════════════════════════════════════════════════════════════
// CENÁRIO 2: Aplicando Exceções (Going Further)
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("\n\n📅 CENÁRIO 2: Movendo uma Instância Específica");
Console.WriteLine("─────────────────────────────────────────────────────────────\n");

var moveDate = new DateOnly(2025, 1, 17); // Sexta, 17 de Janeiro
Console.WriteLine($"⚠ A reunião de {moveDate:dd/MM/yyyy (dddd)} será movida para quinta-feira");
Console.WriteLine("  (Implementando o padrão 'Going Further' do Martin Fowler)\n");

// Mover instância específica para quinta-feira
calendarService.MoveRecurringEventInstance(
    weeklyMeeting.Id,
    originalDate: new DateOnly(2025, 1, 17), // Sexta
    newDate: new DateOnly(2025, 1, 16),      // Quinta
    newStartTime: new TimeOnly(15, 0),
    newEndTime: new TimeOnly(16, 0)
);

Console.WriteLine("✓ Exceção criada: Sexta-feira, 17/01 excluída da regra");
Console.WriteLine("✓ Evento substituto criado: Quinta-feira, 16/01\n");

// Verificar resultado
var checkDates = new[] {
    new DateOnly(2025, 1, 16), // Quinta (nova data)
    new DateOnly(2025, 1, 17), // Sexta (data original - deve estar vazia)
    new DateOnly(2025, 1, 24), // Sexta seguinte (não afetada)
};

foreach (var date in checkDates)
{
    var events = calendarService.GetEventsForDate(date);
    if (events.Any())
    {
        var evt = events.First();
        var type = evt.IsRecurring ? "Recorrente" : "Remarcado";
        Console.WriteLine($"{date:dd/MM/yyyy (dddd)}: ✓ {evt.Title} ({type}) - {evt.StartTime:HH:mm}");
    }
    else
    {
        Console.WriteLine($"{date:dd/MM/yyyy (dddd)}: Sem eventos");
    }
}

// ═══════════════════════════════════════════════════════════════
// CENÁRIO 3: Cancelamento de Instância
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("\n\n📅 CENÁRIO 3: Cancelando uma Instância Específica");
Console.WriteLine("─────────────────────────────────────────────────────────────\n");

var cancelDate = new DateOnly(2025, 1, 24);
Console.WriteLine($"⚠ Cancelando reunião de {cancelDate:dd/MM/yyyy (dddd)}\n");

calendarService.CancelRecurringEventInstance(
    weeklyMeeting.Id,
    cancelDate,
    "Feriado"
);

Console.WriteLine("✓ Exceção criada: instância cancelada\n");

var feb = calendarService.GetEventsForDateRange(
    new DateOnly(2025, 1, 24),
    new DateOnly(2025, 1, 31)
);

foreach (var evt in feb)
{
    Console.WriteLine($"{evt.Date:dd/MM/yyyy (dddd)}: {evt.Title}");
}
Console.WriteLine($"{cancelDate:dd/MM/yyyy (dddd)}: ✗ Cancelado");

// ═══════════════════════════════════════════════════════════════
// CENÁRIO 4: Expressões Temporais Complexas
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("\n\n📅 CENÁRIO 4: Eventos com Regras Complexas");
Console.WriteLine("─────────────────────────────────────────────────────────────\n");

// Evento: Toda segunda E quarta-feira (união)
var mondaysAndWednesdays = new UnionExpression(
    new DayOfWeekExpression(DayOfWeek.Monday),
    new DayOfWeekExpression(DayOfWeek.Wednesday)
);

var gymClass = new RecurringEvent(
    "Aula de Ginástica",
    "Treino funcional",
    new TimeOnly(6, 30),
    new TimeOnly(7, 30),
    mondaysAndWednesdays,
    startDate: new DateOnly(2025, 2, 1)
);

repository.AddRecurringEvent(gymClass);
Console.WriteLine("✓ Aula de Ginástica: Segundas E Quartas-feiras\n");

// Evento: Dias úteis (segunda a sexta, exceto finais de semana)
var allDays = new DailyExpression();
var weekends = new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday);
var weekdays = new DifferenceExpression(allDays, weekends);

var standupMeeting = new RecurringEvent(
    "Daily Stand-up",
    "Reunião diária da equipe",
    new TimeOnly(9, 0),
    new TimeOnly(9, 15),
    weekdays,
    startDate: new DateOnly(2025, 2, 3)
);

repository.AddRecurringEvent(standupMeeting);
Console.WriteLine("✓ Daily Stand-up: Todos os dias EXCETO finais de semana\n");

// Evento: Primeiro dia de cada mês
var firstDayOfMonth = new DayOfMonthExpression(1);
var monthlyReport = new RecurringEvent(
    "Relatório Mensal",
    "Entrega de relatório de performance",
    new TimeOnly(17, 0),
    new TimeOnly(18, 0),
    firstDayOfMonth,
    startDate: new DateOnly(2025, 2, 1)
);

repository.AddRecurringEvent(monthlyReport);
Console.WriteLine("✓ Relatório Mensal: Dia 1 de cada mês\n");

// Verificar uma semana
Console.WriteLine("Eventos na primeira semana de Fevereiro/2025:\n");
var firstWeekFeb = calendarService.GetEventsForDateRange(
    new DateOnly(2025, 2, 3),
    new DateOnly(2025, 2, 7)
);

foreach (var evt in firstWeekFeb.OrderBy(e => e.Date).ThenBy(e => e.StartTime))
{
    Console.WriteLine($"{evt.Date:dd/MM (ddd)} {evt.StartTime:HH:mm} - {evt.Title}");
}

// ═══════════════════════════════════════════════════════════════
// CENÁRIO 5: Evento a cada N dias
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("\n\n📅 CENÁRIO 5: Evento a Cada 3 Dias");
Console.WriteLine("─────────────────────────────────────────────────────────────\n");

var everyThreeDays = new IntervalExpression(new DateOnly(2025, 2, 1), 3);
var waterPlants = new RecurringEvent(
    "Regar Plantas",
    "Lembrete de regar as plantas",
    new TimeOnly(18, 0),
    new TimeOnly(18, 15),
    everyThreeDays
);

repository.AddRecurringEvent(waterPlants);
Console.WriteLine("✓ Regar Plantas: A cada 3 dias a partir de 01/02/2025\n");

var febDates = Enumerable.Range(1, 10)
    .Select(d => new DateOnly(2025, 2, d));

foreach (var date in febDates)
{
    var events = calendarService.GetEventsForDate(date)
        .Where(e => e.Title == "Regar Plantas");
    
    if (events.Any())
        Console.WriteLine($"{date:dd/MM (ddd)}: ✓ Regar Plantas");
}

// ═══════════════════════════════════════════════════════════════
// Resumo dos Benefícios
// ═══════════════════════════════════════════════════════════════
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════════");
Console.WriteLine("  BENEFÍCIOS DO PADRÃO MARTIN FOWLER");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("\n✓ Performance: Não gera milhares de registros no banco");
Console.WriteLine("✓ Flexibilidade: Permite eventos infinitos com exceções pontuais");
Console.WriteLine("✓ Manutenção: Alterar regra afeta todos os eventos futuros");
Console.WriteLine("✓ Clean Code: Separação clara entre regras e instâncias");
Console.WriteLine("✓ SOLID: Interfaces, DDD e responsabilidades bem definidas");
Console.WriteLine("\n═══════════════════════════════════════════════════════════════\n");
