# 📋 Quick Reference - Guia Rápido

Referência rápida para desenvolvedores que precisam entender ou estender o projeto.

## 🚀 Início Rápido (5 minutos)

### Executar Aplicação
```bash
cd RecurringEventsCalendar
dotnet run
```

### Criar Seu Primeiro Evento Recorrente
```csharp
// 1. Setup
var repository = new InMemoryEventRepository();
var service = new CalendarService(repository);

// 2. Criar regra: toda segunda-feira
var everyMonday = new DayOfWeekExpression(DayOfWeek.Monday);

// 3. Criar evento
var meeting = new RecurringEvent(
    title: "Daily Stand-up",
    description: "Reunião diária",
    startTime: new TimeOnly(9, 0),
    endTime: new TimeOnly(9, 15),
    recurrenceRule: everyMonday
);

repository.AddRecurringEvent(meeting);

// 4. Consultar eventos
var events = service.GetEventsForDate(new DateOnly(2025, 1, 6));
```

---

## 📐 Temporal Expressions - Cheat Sheet

### Expressões Básicas

```csharp
// Dia da semana específico
new DayOfWeekExpression(DayOfWeek.Friday)

// Múltiplos dias da semana
new DayOfWeekExpression(DayOfWeek.Monday, DayOfWeek.Wednesday)

// Dia do mês
new DayOfMonthExpression(1, 15) // Dias 1 e 15

// Todos os dias
new DailyExpression()

// A cada N dias
new IntervalExpression(startDate, intervalInDays: 3)
```

### Expressões Compostas

```csharp
// Segunda OU Quarta (OR)
new UnionExpression(
    new DayOfWeekExpression(DayOfWeek.Monday),
    new DayOfWeekExpression(DayOfWeek.Wednesday)
)

// Segunda E dia 1 do mês (AND)
new IntersectionExpression(
    new DayOfWeekExpression(DayOfWeek.Monday),
    new DayOfMonthExpression(1)
)

// Todos os dias EXCETO finais de semana (NOT)
new DifferenceExpression(
    new DailyExpression(),
    new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday)
)
```

---

## 🎯 CalendarService - API Essencial

### Consultar Eventos
```csharp
// Um dia específico
var events = service.GetEventsForDate(new DateOnly(2025, 1, 15));

// Intervalo de datas
var events = service.GetEventsForDateRange(
    startDate: new DateOnly(2025, 1, 1),
    endDate: new DateOnly(2025, 1, 31)
);
```

### Mover Instância
```csharp
service.MoveRecurringEventInstance(
    recurringEventId: eventId,
    originalDate: new DateOnly(2025, 1, 17),  // Data original
    newDate: new DateOnly(2025, 1, 16),       // Nova data
    newStartTime: new TimeOnly(15, 0),        // Opcional
    newEndTime: new TimeOnly(16, 0)           // Opcional
);
```

### Cancelar Instância
```csharp
service.CancelRecurringEventInstance(
    recurringEventId: eventId,
    date: new DateOnly(2025, 1, 24),
    reason: "Feriado"
);
```

---

## 🏗️ Estrutura de Classes - Mapa Mental

```
EventBase (abstract)
├── RecurringEvent (usa ITemporalExpression)
└── OneTimeEvent (data fixa)

ITemporalExpression (interface)
├── DayOfWeekExpression
├── DayOfMonthExpression
├── DailyExpression
├── IntervalExpression
├── UnionExpression (composição)
├── IntersectionExpression (composição)
└── DifferenceExpression (composição)

EventException (exclusão)
└── RecurringEventId + ExceptionDate

IEventRepository (interface)
└── InMemoryEventRepository (implementação)

CalendarService
└── Orquestra eventos + exceções
```

---

## 🔧 Casos de Uso Comuns

### Caso 1: Dias Úteis (Segunda a Sexta)
```csharp
var allDays = new DailyExpression();
var weekends = new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday);
var weekdays = new DifferenceExpression(allDays, weekends);

var dailyReport = new RecurringEvent(
    "Relatório Diário",
    "Enviar relatório",
    new TimeOnly(18, 0),
    new TimeOnly(18, 30),
    weekdays
);
```

### Caso 2: Primeiro e Último Dia Útil do Mês
```csharp
// Primeiro dia útil
var firstBusinessDay = new IntersectionExpression(
    new DayOfMonthExpression(1, 2, 3), // Primeiros 3 dias
    new DifferenceExpression(
        new DailyExpression(),
        new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday)
    )
);
```

### Caso 3: Quinzenalmente
```csharp
var everyTwoWeeks = new IntervalExpression(
    startDate: new DateOnly(2025, 1, 6), // Segunda-feira
    intervalInDays: 14
);

var meeting = new RecurringEvent(
    "Reunião Quinzenal",
    "",
    new TimeOnly(14, 0),
    new TimeOnly(15, 0),
    new IntersectionExpression(
        everyTwoWeeks,
        new DayOfWeekExpression(DayOfWeek.Monday) // Garantir segunda-feira
    )
);
```

### Caso 4: Horário de Verão/Inverno
```csharp
// Evento normal
var summerEvent = new RecurringEvent(...);
repository.AddRecurringEvent(summerEvent);

// Ajustar horário em data específica (mudança de horário)
service.MoveRecurringEventInstance(
    summerEvent.Id,
    originalDate: new DateOnly(2025, 3, 30), // Início horário de verão
    newDate: new DateOnly(2025, 3, 30),      // Mesma data
    newStartTime: new TimeOnly(13, 0),       // -1 hora
    newEndTime: new TimeOnly(14, 0)
);
```

---

## 🎨 Padrões de Design Identificados

### Strategy Pattern
`ITemporalExpression` permite trocar algoritmos de recorrência dinamicamente.

### Composite Pattern
`UnionExpression`, `IntersectionExpression` compõem múltiplas expressões.

### Repository Pattern
`IEventRepository` abstrai persistência de dados.

### Service Layer Pattern
`CalendarService` orquestra lógica de negócio.

### DTO Pattern
`CalendarEventDto` separa domínio de apresentação.

---

## ⚡ Performance Tips

### 1. Cache de Expressões Frequentes
```csharp
// Em vez de criar toda vez
var weekdays = new DifferenceExpression(...); // Reutilizar instância
```

### 2. Limitar Range de Consultas
```csharp
// ✅ Bom: Consultar apenas período necessário
service.GetEventsForDateRange(today, today.AddDays(30));

// ❌ Ruim: Consultar anos
service.GetEventsForDateRange(today, today.AddYears(5));
```

### 3. Índices no Banco (quando migrar para SQL)
```sql
CREATE INDEX IX_EventExceptions_Date ON EventExceptions(ExceptionDate);
CREATE INDEX IX_EventExceptions_EventId ON EventExceptions(RecurringEventId);
```

---

## 🐛 Troubleshooting

### Problema: Evento não aparece na data esperada
**Solução:**
```csharp
// Verificar se regra inclui a data
var includes = event.RecurrenceRule.Includes(targetDate);
Console.WriteLine($"Rule includes date: {includes}");

// Verificar se há exceção
var exceptions = repository.GetExceptionsForDate(targetDate);
Console.WriteLine($"Exceptions: {exceptions.Count()}");

// Verificar range do evento
if (event.StartDate.HasValue && targetDate < event.StartDate.Value)
    Console.WriteLine("Data antes do início do evento");
```

### Problema: ArgumentException ao criar evento
**Causas comuns:**
- `endTime <= startTime` - Horário de término antes do início
- `endDate < startDate` - Data de término antes do início
- Expressão temporal nula
- Intervalo <= 0 em `IntervalExpression`

### Problema: Evento movido não aparece na nova data
**Verificar:**
```csharp
// A instância original foi criada uma exceção?
var exceptions = repository.GetExceptionsForEvent(eventId);

// O OneTimeEvent foi criado?
var oneTimeEvents = repository.GetAllOneTimeEvents()
    .Where(e => e.Date == newDate);
```

---

## 📊 Métricas e Monitoramento

### Contar Ocorrências
```csharp
int CountOccurrences(RecurringEvent evt, DateOnly start, DateOnly end)
{
    int count = 0;
    for (var d = start; d <= end; d = d.AddDays(1))
        if (evt.OccursOn(d)) count++;
    return count;
}
```

### Calcular Carga de Trabalho
```csharp
TimeSpan CalculateTotalTime(DateOnly start, DateOnly end)
{
    var events = service.GetEventsForDateRange(start, end);
    return TimeSpan.FromMinutes(
        events.Sum(e => (e.EndTime - e.StartTime).TotalMinutes)
    );
}
```

---

## 🔐 Validações Importantes

### Antes de Criar Evento
```csharp
if (endTime <= startTime)
    throw new ArgumentException("Horário inválido");

if (startDate.HasValue && endDate.HasValue && endDate < startDate)
    throw new ArgumentException("Range inválido");

if (recurrenceRule == null)
    throw new ArgumentNullException(nameof(recurrenceRule));
```

### Antes de Mover/Cancelar
```csharp
var evt = repository.GetAllRecurringEvents()
    .FirstOrDefault(e => e.Id == eventId)
    ?? throw new InvalidOperationException("Evento não encontrado");

if (!evt.OccursOn(originalDate))
    throw new InvalidOperationException("Evento não ocorre nesta data");
```

---

## 🎓 Conceitos-Chave para Explicar em Code Review

1. **Temporal Expression**: Objeto que representa uma regra de recorrência
2. **Exceção**: Exclusão de uma instância específica da regra
3. **Substituição**: Evento único criado quando instância é movida
4. **Lazy Evaluation**: Datas não são pré-calculadas, são avaliadas on-demand
5. **Composição**: Expressões podem ser combinadas (Union, Intersection, Difference)

---

## 📚 Links Úteis

- **README Principal**: [README.md](README.md)
- **Diagramas Visuais**: [DIAGRAMS.md](DIAGRAMS.md)
- **Exemplos Avançados**: [ADVANCED_EXAMPLES.md](ADVANCED_EXAMPLES.md)
- **Guia de Banco de Dados**: [DATABASE_GUIDE.md](DATABASE_GUIDE.md)
- **Guia de Testes**: [TESTING_GUIDE.md](TESTING_GUIDE.md)

---

## ✅ Checklist de Implementação

Ao adicionar nova funcionalidade:

- [ ] Criar Temporal Expression (se necessário)
- [ ] Adicionar testes unitários
- [ ] Documentar no código
- [ ] Atualizar README se for feature pública
- [ ] Considerar impacto em exceções
- [ ] Validar edge cases (datas limites, null values)
- [ ] Testar performance com muitos eventos

---

**Este guia cobre 80% dos casos de uso. Para casos específicos, consulte a documentação completa!** 📖
