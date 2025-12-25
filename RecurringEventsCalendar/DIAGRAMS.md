# Diagramas e Exemplos Visuais

## 🎨 Arquitetura em Camadas

```
┌─────────────────────────────────────────────────────────────┐
│                     Program.cs (Console)                     │
│                    Ponto de Entrada                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Application Layer (Casos de Uso)                │
│  ┌─────────────────────────────────────────────────────┐    │
│  │         CalendarService (Orquestração)              │    │
│  │  - GetEventsForDate()                               │    │
│  │  - MoveRecurringEventInstance()                     │    │
│  │  - CancelRecurringEventInstance()                   │    │
│  └─────────────────────────────────────────────────────┘    │
└────────────────────────┬────────────────────────────────────┘
                         │ Depende de ↓
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer (Core)                       │
│  ┌──────────────────┐  ┌─────────────────────────────────┐ │
│  │    Entities      │  │     Value Objects               │ │
│  │  - EventBase     │  │  - ITemporalExpression          │ │
│  │  - RecurringEvent│  │  - DayOfWeekExpression          │ │
│  │  - OneTimeEvent  │  │  - DailyExpression              │ │
│  │  - EventException│  │  - IntersectionExpression       │ │
│  └──────────────────┘  └─────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Interfaces (Contratos)                     │  │
│  │  - IEventRepository (DIP - Inversão de Dependência)  │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         ▲ Implementa
                         │
┌─────────────────────────────────────────────────────────────┐
│           Infrastructure Layer (Implementações)              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │        InMemoryEventRepository                      │    │
│  │  (Poderia ser: SqlEventRepository,                  │    │
│  │   MongoEventRepository, etc.)                       │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Fluxo: Consultar Eventos com Exceções

```
Usuario solicita: "Eventos de 17/01/2025"
    │
    ▼
┌─────────────────────────────────────────────┐
│   CalendarService.GetEventsForDate()        │
└────────┬────────────────────────────────────┘
         │
         ├─► [1] Buscar Exceções para 17/01
         │        Repository.GetExceptionsForDate(17/01)
         │        Resultado: [EventException: RecurringEventId=ABC]
         │
         ├─► [2] Buscar Eventos Recorrentes
         │        Repository.GetAllRecurringEvents()
         │        Para cada evento:
         │          ├─► OccursOn(17/01)?
         │          │     RecurringEvent.RecurrenceRule.Includes(17/01)
         │          │     DayOfWeekExpression(Friday).Includes(17/01)
         │          │     Resultado: true (é sexta)
         │          │
         │          └─► Tem exceção? Sim (ID=ABC)
         │                ❌ NÃO incluir este evento
         │
         └─► [3] Buscar Eventos Únicos (OneTimeEvent)
                  Repository.GetAllOneTimeEvents()
                  Para cada evento:
                    └─► OccursOn(17/01)?
                          OneTimeEvent.Date == 17/01
                          ✅ Incluir evento
         │
         ▼
    Retorna lista final:
    [OneTimeEvent "Reunião Remarcada" - 16/01]
```

## 📐 Hierarquia de Classes

```
                    EventBase (abstract)
                         │
          ┌──────────────┴──────────────┐
          │                             │
    RecurringEvent               OneTimeEvent
    │                                   │
    ├─ Id: Guid                        ├─ Date: DateOnly
    ├─ Title: string                   └─ (herda campos base)
    ├─ RecurrenceRule: ITemporalExpression
    └─ OccursOn(date): bool
         └─> Delega para RecurrenceRule.Includes(date)


           ITemporalExpression (interface)
                     │
        ┌────────────┼────────────┐
        │            │            │
 DayOfWeekExpression │     DailyExpression
 DayOfMonthExpression│     IntervalExpression
        │            │
        │     Composições:
        │     ├─ UnionExpression (OR)
        │     ├─ IntersectionExpression (AND)
        │     └─ DifferenceExpression (NOT)
```

## 🎯 Exemplo: Movendo uma Instância

### Situação Inicial
```
Janeiro 2025
─────────────────────────────────
Seg Ter Qua Qui Sex Sab Dom
              1   2   3   4   5
 6   7   8   9  10🟢 11  12
13  14  15  16  17🟢 18  19
20  21  22  23  24🟢 25  26
27  28  29  30  31🟢

🟢 = Reunião de Equipe (RecurringEvent)
Regra: DayOfWeekExpression(Friday)
```

### Ação: Mover instância de 17/01 para 16/01
```csharp
calendarService.MoveRecurringEventInstance(
    recurringEventId: meetingId,
    originalDate: 17/01,
    newDate: 16/01
);
```

### Situação Final
```
Janeiro 2025
─────────────────────────────────
Seg Ter Qua Qui Sex Sab Dom
              1   2   3   4   5
 6   7   8   9  10🟢 11  12
13  14  15  16🔵 17❌ 18  19
20  21  22  23  24🟢 25  26
27  28  29  30  31🟢

🟢 = Reunião (RecurringEvent - Regra)
🔵 = Reunião Remarcada (OneTimeEvent - Substituição)
❌ = Exceção (EventException - Exclusão)

Banco de dados:
┌──────────────────────────────────────────────┐
│ RecurringEvents                              │
│ - ID: ABC                                    │
│ - Title: "Reunião de Equipe"                 │
│ - Rule: DayOfWeekExpression(Friday)          │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ EventExceptions                              │
│ - RecurringEventId: ABC                      │
│ - ExceptionDate: 17/01/2025                  │
│ - Reason: "Movido para 16/01/2025"           │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ OneTimeEvents                                │
│ - ID: XYZ                                    │
│ - Title: "Reunião de Equipe (Remarcado)"    │
│ - Date: 16/01/2025                           │
└──────────────────────────────────────────────┘
```

## 🧮 Comparação: Com vs Sem Padrão

### ❌ SEM o Padrão (Abordagem Ingênua)
```
Reunião Semanal por 1 ano = 52 registros

Events Table:
┌────┬─────────────────────┬────────────┐
│ ID │ Title               │ Date       │
├────┼─────────────────────┼────────────┤
│  1 │ Reunião de Equipe   │ 03/01/2025 │
│  2 │ Reunião de Equipe   │ 10/01/2025 │
│  3 │ Reunião de Equipe   │ 17/01/2025 │
│  4 │ Reunião de Equipe   │ 24/01/2025 │
│  5 │ Reunião de Equipe   │ 31/01/2025 │
│... │ ...                 │ ...        │
│ 52 │ Reunião de Equipe   │ 27/12/2025 │
└────┴─────────────────────┴────────────┘

Problemas:
- Alterar horário = UPDATE em 52 linhas
- Adicionar participante = UPDATE em 52 linhas
- Evento "para sempre" = IMPOSSÍVEL
- Performance degrada com tempo
```

### ✅ COM o Padrão (Temporal Expressions)
```
Reunião Semanal por 1 ano = 1 registro + regra

RecurringEvents Table:
┌────┬─────────────────────┬──────────────────────┐
│ ID │ Title               │ RecurrenceRule       │
├────┼─────────────────────┼──────────────────────┤
│ 1  │ Reunião de Equipe   │ DayOfWeek: Friday    │
└────┴─────────────────────┴──────────────────────┘

EventExceptions Table (quando necessário):
┌────┬─────────────┬────────────┬──────────────┐
│ ID │ EventId     │ Date       │ Reason       │
├────┼─────────────┼────────────┼──────────────┤
│ 1  │ 1           │ 17/01/2025 │ Movido       │
└────┴─────────────┴────────────┴──────────────┘

Benefícios:
- Alterar horário = UPDATE em 1 linha
- Evento "infinito" = POSSÍVEL
- Performance constante
- Exceções pontuais não afetam regra
```

## 🔢 Expressões Compostas - Exemplos

### Exemplo 1: Dias Úteis
```
Regra: Todos os dias EXCETO finais de semana

DifferenceExpression(
    included: DailyExpression(),
    excluded: DayOfWeekExpression(Saturday, Sunday)
)

Janeiro 2025:
Mo Tu We Th Fr Sa Su
       1  2  3  4  5   → ✅✅✅❌❌
 6  7  8  9 10 11 12   → ✅✅✅✅✅❌❌
13 14 15 16 17 18 19   → ✅✅✅✅✅❌❌
```

### Exemplo 2: Segunda E Primeiro do Mês
```
Regra: Apenas segundas-feiras que caem no dia 1

IntersectionExpression(
    DayOfWeekExpression(Monday),
    DayOfMonthExpression(1)
)

2025:
- 01/01 (Qua) ❌
- 01/02 (Sáb) ❌
- 01/03 (Sáb) ❌
- 01/04 (Ter) ❌
- 01/05 (Qui) ❌
- 01/06 (Dom) ❌
- 01/07 (Ter) ❌
- 01/08 (Sex) ❌
- 01/09 (SEG) ✅ ← Corresponde!
```

### Exemplo 3: Segundas OU Quartas
```
Regra: Toda segunda-feira OU quarta-feira

UnionExpression(
    DayOfWeekExpression(Monday),
    DayOfWeekExpression(Wednesday)
)

Janeiro 2025:
Mo Tu We Th Fr Sa Su
      ✅  2 ✅  4  5
✅  7  8 ✅ 10 11 12
✅ 14 15 ✅ 17 18 19
✅ 21 22 ✅ 24 25 26
✅ 28 29 ✅ 31
```

## 🎯 SOLID em Ação

### Single Responsibility Principle
```
❌ Classe com múltiplas responsabilidades:
class Event {
    CalculateNextOccurrence()
    SaveToDatabase()
    SendEmailReminder()
    ValidatePermissions()
}

✅ Responsabilidades separadas:
class RecurringEvent {
    OccursOn(date) // Apenas lógica de negócio
}
class EventRepository {
    Save(event) // Apenas persistência
}
class EmailService {
    SendReminder(event) // Apenas notificação
}
```

### Open/Closed Principle
```
✅ Extensível sem modificação:

// Nova expressão temporal sem alterar código existente
class BusinessDaysExpression : ITemporalExpression {
    public bool Includes(DateOnly date) {
        return date.DayOfWeek != Saturday 
            && date.DayOfWeek != Sunday
            && !IsHoliday(date);
    }
}

// Usa em qualquer RecurringEvent
var event = new RecurringEvent(
    title: "Daily Report",
    recurrenceRule: new BusinessDaysExpression()
);
```

---

**Estes diagramas ilustram visualmente a elegância do padrão Martin Fowler** 🎨
