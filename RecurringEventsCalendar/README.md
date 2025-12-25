# Recurring Events for Calendars - Padrão Martin Fowler

Aplicação console em **.NET 10** demonstrando o padrão de design **"Recurring Events for Calendars"** proposto por Martin Fowler, implementando SOLID, DDD, Clean Code e OOP.

## 📋 O Problema

A maioria dos sistemas de calendário comete o erro de criar **milhares de registros** no banco de dados para cada repetição de um evento recorrente. Se você tem uma reunião semanal por 1 ano, isso gera 52 registros desnecessários.

## 💡 A Solução: Temporal Expressions

Em vez de armazenar **datas**, armazenamos **regras**. Um evento recorrente contém uma expressão temporal que determina quando ele ocorre.

### Conceito Chave
```csharp
// ❌ Errado: 52 registros no banco
foreach(var week in year)
    database.Insert(new Event { Date = week.Friday });

// ✅ Correto: 1 regra
var event = new RecurringEvent(
    "Reunião Semanal",
    recurrenceRule: new DayOfWeekExpression(DayOfWeek.Friday)
);
```

## 🏗️ Arquitetura

Estrutura seguindo **DDD** e **Clean Architecture**:

```
RecurringEventsCalendar/
├── Domain/                          # Núcleo do negócio
│   ├── Entities/                    # Entidades do domínio
│   │   ├── EventBase.cs            # Classe base abstrata (OCP)
│   │   ├── RecurringEvent.cs       # Evento com regra de recorrência
│   │   ├── OneTimeEvent.cs         # Evento único (substituições)
│   │   └── EventException.cs       # Exceções/exclusões
│   ├── ValueObjects/                # Expressões temporais (imutáveis)
│   │   ├── ITemporalExpression.cs  # Interface base (LSP)
│   │   ├── DayOfWeekExpression.cs  # Dias da semana
│   │   ├── DayOfMonthExpression.cs # Dias do mês
│   │   ├── DailyExpression.cs      # Todos os dias
│   │   ├── IntervalExpression.cs   # A cada N dias
│   │   ├── UnionExpression.cs      # União (OR)
│   │   ├── IntersectionExpression.cs # Interseção (AND)
│   │   └── DifferenceExpression.cs # Diferença (NOT)
│   └── Interfaces/                  # Contratos (DIP)
│       └── IEventRepository.cs
├── Application/                     # Casos de uso
│   └── Services/
│       ├── CalendarService.cs      # Orquestra regras e exceções
│       └── CalendarEventDto.cs     # DTO para apresentação
├── Infrastructure/                  # Implementações técnicas
│   └── Repositories/
│       └── InMemoryEventRepository.cs
└── Program.cs                       # Ponto de entrada
```

## 🎯 Princípios SOLID Aplicados

### Single Responsibility Principle (SRP)
- `EventBase`: Responsável apenas por dados básicos do evento
- `RecurringEvent`: Responsável por aplicar regra de recorrência
- `CalendarService`: Responsável por orquestrar consultas com exceções

### Open/Closed Principle (OCP)
- Novas expressões temporais podem ser adicionadas sem modificar código existente
- Sistema extensível via composição de expressões

### Liskov Substitution Principle (LSP)
- Qualquer `ITemporalExpression` pode ser usada em `RecurringEvent`
- `EventBase` permite polimorfismo entre `RecurringEvent` e `OneTimeEvent`

### Interface Segregation Principle (ISP)
- `ITemporalExpression`: Interface mínima com apenas `Includes(date)`
- `IEventRepository`: Interface focada em operações de repositório

### Dependency Inversion Principle (DIP)
- `CalendarService` depende de `IEventRepository` (abstração)
- Domain não depende de Infrastructure

## 🔥 Funcionalidades Implementadas

### 1. Expressões Temporais Básicas
```csharp
// Todas as sextas-feiras
var everyFriday = new DayOfWeekExpression(DayOfWeek.Friday);

// Dia 1 de cada mês
var firstDay = new DayOfMonthExpression(1);

// A cada 3 dias
var everyThreeDays = new IntervalExpression(startDate, 3);
```

### 2. Expressões Compostas
```csharp
// Segundas OU Quartas
var mondaysAndWednesdays = new UnionExpression(
    new DayOfWeekExpression(DayOfWeek.Monday),
    new DayOfWeekExpression(DayOfWeek.Wednesday)
);

// Todos os dias EXCETO finais de semana
var weekdays = new DifferenceExpression(
    new DailyExpression(),
    new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday)
);
```

### 3. Exceções (Going Further) - O Diferencial

#### Problema
> "A reunião é sempre à sexta, mas **nesta semana** será na quinta-feira."

#### Solução
O padrão implementa 3 componentes:

1. **Regra (Padrão)**: Continua gerando datas normais
2. **Exclusão**: Registra que a regra deve ser ignorada em data específica
3. **Substituição**: Evento único na nova data

```csharp
// Mover instância de Sexta (17/01) para Quinta (16/01)
calendarService.MoveRecurringEventInstance(
    recurringEventId: weeklyMeeting.Id,
    originalDate: new DateOnly(2025, 1, 17),  // Sexta
    newDate: new DateOnly(2025, 1, 16),        // Quinta
    newStartTime: new TimeOnly(15, 0)
);

// Internamente cria:
// 1. EventException para 17/01 (exclusão)
// 2. OneTimeEvent para 16/01 (substituição)
```

### 4. Algoritmo de Consulta

```csharp
public IEnumerable<CalendarEventDto> GetEventsForDate(DateOnly date)
{
    // 1. Buscar exceções (exclusões) para esta data
    var exceptions = GetExceptionsForDate(date);

    // 2. Buscar eventos recorrentes que correspondem à regra
    var recurringEvents = GetAllRecurringEvents()
        .Where(e => e.OccursOn(date));

    // 3. Filtrar eventos excluídos
    foreach (var event in recurringEvents)
        if (!exceptions.Contains(event.Id))
            yield return event;

    // 4. Adicionar eventos únicos (substituições)
    var oneTimeEvents = GetAllOneTimeEvents()
        .Where(e => e.OccursOn(date));
    
    foreach (var event in oneTimeEvents)
        yield return event;
}
```

## 🚀 Como Executar

```bash
cd RecurringEventsCalendar
dotnet run
```

## 📊 Resultados da Execução

A aplicação demonstra 5 cenários:

1. **Evento Semanal Simples**: Reunião toda sexta-feira
2. **Movendo Instância**: Move reunião de sexta para quinta (Going Further)
3. **Cancelamento**: Cancela instância específica
4. **Regras Complexas**: Múltiplos eventos com expressões compostas
5. **Intervalos**: Evento a cada N dias

### Output Exemplo
```
📅 CENÁRIO 2: Movendo uma Instância Específica
─────────────────────────────────────────────────────────────

⚠ A reunião de 17/01/2025 (sexta-feira) será movida para quinta-feira

✓ Exceção criada: Sexta-feira, 17/01 excluída da regra
✓ Evento substituto criado: Quinta-feira, 16/01

16/01/2025 (quinta-feira): ✓ Reunião de Equipe (Remarcado) - 15:00
17/01/2025 (sexta-feira): Sem eventos
24/01/2025 (sexta-feira): ✓ Reunião de Equipe (Recorrente) - 14:00
```

## 📚 Benefícios do Padrão

| Benefício | Descrição |
|-----------|-----------|
| 🚀 **Performance** | Não gera milhares de registros no banco |
| ♾️ **Escalabilidade** | Permite eventos infinitos (ex: "para sempre") |
| 🔧 **Manutenção** | Alterar regra afeta todos eventos futuros automaticamente |
| 🎯 **Flexibilidade** | Exceções pontuais não afetam a regra geral |
| 🧹 **Clean Code** | Separação clara entre regras e instâncias |

## 🎓 Para Desenvolvedores Júnior

### Por que não armazenar todas as datas?

**Problemas:**
- 📦 Banco de dados cresce exponencialmente
- 🐌 Consultas ficam lentas
- 🔄 Alterar horário de "todas reuniões futuras" = UPDATE em milhares de linhas
- ♾️ Eventos infinitos são impossíveis

**Solução com Temporal Expressions:**
- ✅ 1 registro = infinitos eventos
- ✅ Mudança na regra = 1 UPDATE
- ✅ Exceções tratadas separadamente
- ✅ Performance previsível

## 📖 Referências

- [Martin Fowler - Recurring Events for Calendars](https://martinfowler.com/apsupp/recurring.pdf)
- [Domain-Driven Design (DDD)](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
- [SOLID Principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

## 🛠️ Tecnologias

- **.NET 10.0** (LTS)
- **C# 13**
- **DateOnly/TimeOnly** (tipos modernos do .NET 6+)

## 📝 Licença

Código educacional - livre para uso e modificação.

---

**Desenvolvido seguindo princípios SOLID, DDD e Clean Code** 🎯
