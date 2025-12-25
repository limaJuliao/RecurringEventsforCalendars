# Recurring Events for Calendars - Padrão Martin Fowler

Implementação completa em **.NET 10** do padrão de design **"Recurring Events for Calendars"** proposto por Martin Fowler.

## 🎯 Sobre o Projeto

Esta aplicação demonstra como implementar eventos recorrentes em calendários **sem criar milhares de registros** no banco de dados, usando o conceito de **Temporal Expressions** combinado com **exceções** para instâncias específicas.

### Princípios Aplicados
- ✅ **SOLID** (todos os 5 princípios)
- ✅ **DDD** (Domain-Driven Design)
- ✅ **Clean Code**
- ✅ **Clean Architecture**
- ✅ **OOP** (Orientação a Objetos)

## 🚀 Quick Start

```bash
cd RecurringEventsCalendar
dotnet run
```

## 📚 Documentação Completa

O projeto inclui documentação detalhada em múltiplos arquivos:

### 📖 Documentos Principais

1. **[README.md](RecurringEventsCalendar/README.md)** - Documentação principal do projeto
   - Arquitetura e estrutura de pastas
   - Explicação do padrão
   - Conceitos fundamentais
   - Benefícios e trade-offs

2. **[DIAGRAMS.md](RecurringEventsCalendar/DIAGRAMS.md)** - Diagramas visuais
   - Arquitetura em camadas
   - Fluxo de algoritmos
   - Hierarquia de classes
   - Comparação com abordagem ingênua

3. **[ADVANCED_EXAMPLES.md](RecurringEventsCalendar/ADVANCED_EXAMPLES.md)** - Casos de uso práticos
   - Reuniões corporativas complexas
   - Sistema de turnos
   - Lembretes de medicação
   - Manutenção preventiva
   - Horários de aulas
   - Backups automáticos

4. **[DATABASE_GUIDE.md](RecurringEventsCalendar/DATABASE_GUIDE.md)** - Evolução para produção
   - Migração para Entity Framework Core
   - Implementação com Dapper
   - Alternativa NoSQL (MongoDB)
   - Schema SQL completo
   - Otimizações de performance

5. **[TESTING_GUIDE.md](RecurringEventsCalendar/TESTING_GUIDE.md)** - Guia de testes
   - Testes unitários (xUnit)
   - Testes de integração
   - Mocks com Moq
   - Cobertura de código

## 🏗️ Estrutura do Projeto

```
RecurringEventsCalendar/
├── Domain/                      # Camada de Domínio (DDD)
│   ├── Entities/               # Entidades do negócio
│   ├── ValueObjects/           # Temporal Expressions
│   └── Interfaces/             # Contratos (DIP)
├── Application/                # Camada de Aplicação
│   └── Services/              # Orquestração e casos de uso
├── Infrastructure/             # Camada de Infraestrutura
│   └── Repositories/          # Implementações de persistência
└── Program.cs                 # Console App com demonstrações
```

## 💡 Conceito Principal

### ❌ Abordagem Tradicional (Problemática)
```
Reunião semanal por 1 ano = 52 registros no banco

Events:
- 03/01/2025 - Reunião
- 10/01/2025 - Reunião
- 17/01/2025 - Reunião
- ... (49 linhas)
```

### ✅ Padrão Martin Fowler
```
Reunião semanal por 1 ano = 1 regra + exceções pontuais

RecurringEvent:
- Title: "Reunião"
- Rule: DayOfWeekExpression(Friday)

EventExceptions: (apenas quando necessário)
- 17/01/2025: Movido para quinta-feira
```

## 🎯 Exemplo de Uso

```csharp
// Criar evento recorrente: toda sexta-feira
var everyFriday = new DayOfWeekExpression(DayOfWeek.Friday);
var weeklyMeeting = new RecurringEvent(
    "Reunião de Equipe",
    "Reunião semanal",
    startTime: new TimeOnly(14, 0),
    endTime: new TimeOnly(15, 0),
    recurrenceRule: everyFriday
);

repository.AddRecurringEvent(weeklyMeeting);

// Mover UMA instância específica
calendarService.MoveRecurringEventInstance(
    weeklyMeeting.Id,
    originalDate: new DateOnly(2025, 1, 17), // Sexta
    newDate: new DateOnly(2025, 1, 16)       // Quinta
);

// Consultar eventos de um dia
var events = calendarService.GetEventsForDate(new DateOnly(2025, 1, 17));
// Resultado: Vazio (evento foi movido)
```

## 🔥 Destaques Técnicos

### 1. Temporal Expressions
Sistema composável de regras de recorrência:

- `DayOfWeekExpression` - Dias da semana específicos
- `DayOfMonthExpression` - Dias do mês
- `IntervalExpression` - A cada N dias
- `UnionExpression` - OR lógico
- `IntersectionExpression` - AND lógico
- `DifferenceExpression` - Exclusão (NOT)

### 2. Sistema de Exceções ("Going Further")
Implementa o padrão completo do Martin Fowler para tratar instâncias específicas:

- **Exclusão**: Marca data para ser ignorada pela regra
- **Substituição**: Cria evento único na nova data

### 3. SOLID em Ação

**Single Responsibility**: Cada classe tem uma responsabilidade única

**Open/Closed**: Extensível via novas `ITemporalExpression`

**Liskov Substitution**: Qualquer expressão temporal funciona em `RecurringEvent`

**Interface Segregation**: Interfaces focadas (`ITemporalExpression` tem 1 método)

**Dependency Inversion**: `CalendarService` depende de `IEventRepository` (abstração)

## 📊 Performance

| Cenário | Tradicional | Temporal Expressions |
|---------|-------------|---------------------|
| Evento semanal (1 ano) | 52 registros | 1 registo |
| Alterar horário futuro | UPDATE em 52 linhas | UPDATE em 1 linha |
| Evento infinito | Impossível | Possível |
| Mover 1 instância | UPDATE em 1 linha | INSERT de 2 registros |

## 🛠️ Tecnologias

- **.NET 10.0** (LTS mais recente)
- **C# 13**
- **DateOnly/TimeOnly** (tipos modernos do .NET)
- Repositório em memória (facilmente substituível por EF Core, Dapper, MongoDB)

## 📈 Próximos Passos

1. ✅ Implementação base completa
2. ⬜ Adicionar persistência com Entity Framework Core
3. ⬜ Criar API REST
4. ⬜ Adicionar autenticação/autorização
5. ⬜ Frontend (Blazor/React)

## 📖 Leitura Recomendada

- [Martin Fowler - Recurring Events for Calendars](https://martinfowler.com/apsupp/recurring.pdf)
- [Domain-Driven Design (Eric Evans)](https://www.domainlanguage.com/ddd/)
- [Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 📝 Licença

Código educacional - livre para uso e modificação.

---

**Desenvolvido com ❤️ seguindo as melhores práticas de engenharia de software** 

Para explorar o código completo, navegue até a pasta [RecurringEventsCalendar](RecurringEventsCalendar/)