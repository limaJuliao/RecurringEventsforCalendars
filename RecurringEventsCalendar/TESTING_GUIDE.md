# Guia de Testes Unitários

Exemplos de como testar cada componente do padrão usando xUnit e Fluent Assertions.

## 📦 Setup Inicial

```bash
# Criar projeto de testes
dotnet new xunit -n RecurringEventsCalendar.Tests

# Adicionar pacotes
dotnet add package FluentAssertions
dotnet add package Moq

# Referenciar projeto principal
dotnet add reference ../RecurringEventsCalendar/RecurringEventsCalendar.csproj
```

---

## 🧪 Testes de Temporal Expressions

### DayOfWeekExpression
```csharp
using FluentAssertions;
using RecurringEventsCalendar.Domain.ValueObjects;
using Xunit;

namespace RecurringEventsCalendar.Tests.Domain.ValueObjects;

public class DayOfWeekExpressionTests
{
    [Fact]
    public void Includes_WhenDateIsFriday_ShouldReturnTrue()
    {
        // Arrange
        var expression = new DayOfWeekExpression(DayOfWeek.Friday);
        var friday = new DateOnly(2025, 1, 10); // É uma sexta-feira

        // Act
        var result = expression.Includes(friday);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Includes_WhenDateIsNotFriday_ShouldReturnFalse()
    {
        // Arrange
        var expression = new DayOfWeekExpression(DayOfWeek.Friday);
        var monday = new DateOnly(2025, 1, 6); // É uma segunda-feira

        // Act
        var result = expression.Includes(monday);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(6, DayOfWeek.Monday)]    // 06/01 é segunda
    [InlineData(8, DayOfWeek.Wednesday)] // 08/01 é quarta
    [InlineData(10, DayOfWeek.Friday)]   // 10/01 é sexta
    public void Includes_ShouldMatchCorrectDayOfWeek(int day, DayOfWeek expectedDay)
    {
        // Arrange
        var expression = new DayOfWeekExpression(expectedDay);
        var date = new DateOnly(2025, 1, day);

        // Act & Assert
        expression.Includes(date).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithMultipleDays_ShouldMatchAnyDay()
    {
        // Arrange
        var expression = new DayOfWeekExpression(
            DayOfWeek.Monday, 
            DayOfWeek.Wednesday, 
            DayOfWeek.Friday
        );

        // Act & Assert
        expression.Includes(new DateOnly(2025, 1, 6)).Should().BeTrue();  // Segunda
        expression.Includes(new DateOnly(2025, 1, 8)).Should().BeTrue();  // Quarta
        expression.Includes(new DateOnly(2025, 1, 10)).Should().BeTrue(); // Sexta
        expression.Includes(new DateOnly(2025, 1, 7)).Should().BeFalse(); // Terça
    }

    [Fact]
    public void Constructor_WithNoDays_ShouldThrowException()
    {
        // Act
        Action act = () => new DayOfWeekExpression();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos um dia*");
    }
}
```

### IntervalExpression
```csharp
public class IntervalExpressionTests
{
    [Fact]
    public void Includes_WithEveryThreeDays_ShouldMatchCorrectly()
    {
        // Arrange
        var startDate = new DateOnly(2025, 1, 1);
        var expression = new IntervalExpression(startDate, intervalInDays: 3);

        // Act & Assert
        expression.Includes(new DateOnly(2025, 1, 1)).Should().BeTrue();  // Dia 0
        expression.Includes(new DateOnly(2025, 1, 2)).Should().BeFalse(); // Dia 1
        expression.Includes(new DateOnly(2025, 1, 3)).Should().BeFalse(); // Dia 2
        expression.Includes(new DateOnly(2025, 1, 4)).Should().BeTrue();  // Dia 3
        expression.Includes(new DateOnly(2025, 1, 7)).Should().BeTrue();  // Dia 6
    }

    [Fact]
    public void Includes_BeforeStartDate_ShouldReturnFalse()
    {
        // Arrange
        var startDate = new DateOnly(2025, 1, 10);
        var expression = new IntervalExpression(startDate, 2);
        var beforeStart = new DateOnly(2025, 1, 5);

        // Act
        var result = expression.Includes(beforeStart);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidInterval_ShouldThrowException(int interval)
    {
        // Act
        Action act = () => new IntervalExpression(DateOnly.FromDateTime(DateTime.Today), interval);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*intervalo*");
    }
}
```

### Expressões Compostas
```csharp
public class CompositeExpressionsTests
{
    [Fact]
    public void UnionExpression_ShouldMatchEitherExpression()
    {
        // Arrange
        var mondays = new DayOfWeekExpression(DayOfWeek.Monday);
        var fridays = new DayOfWeekExpression(DayOfWeek.Friday);
        var expression = new UnionExpression(mondays, fridays);

        // Act & Assert
        expression.Includes(new DateOnly(2025, 1, 6)).Should().BeTrue();  // Segunda
        expression.Includes(new DateOnly(2025, 1, 7)).Should().BeFalse(); // Terça
        expression.Includes(new DateOnly(2025, 1, 10)).Should().BeTrue(); // Sexta
    }

    [Fact]
    public void IntersectionExpression_ShouldMatchBothExpressions()
    {
        // Arrange
        var mondays = new DayOfWeekExpression(DayOfWeek.Monday);
        var firstDayOfMonth = new DayOfMonthExpression(1);
        var expression = new IntersectionExpression(mondays, firstDayOfMonth);

        // Act & Assert
        expression.Includes(new DateOnly(2025, 9, 1)).Should().BeTrue();  // Segunda, dia 1
        expression.Includes(new DateOnly(2025, 1, 1)).Should().BeFalse(); // Quarta, dia 1
        expression.Includes(new DateOnly(2025, 1, 6)).Should().BeFalse(); // Segunda, dia 6
    }

    [Fact]
    public void DifferenceExpression_WeekdaysOnly_ShouldExcludeWeekends()
    {
        // Arrange
        var allDays = new DailyExpression();
        var weekends = new DayOfWeekExpression(DayOfWeek.Saturday, DayOfWeek.Sunday);
        var weekdays = new DifferenceExpression(allDays, weekends);

        // Act & Assert
        weekdays.Includes(new DateOnly(2025, 1, 6)).Should().BeTrue();  // Segunda
        weekdays.Includes(new DateOnly(2025, 1, 10)).Should().BeTrue(); // Sexta
        weekdays.Includes(new DateOnly(2025, 1, 11)).Should().BeFalse();// Sábado
        weekdays.Includes(new DateOnly(2025, 1, 12)).Should().BeFalse();// Domingo
    }
}
```

---

## 🧪 Testes de Entidades

### RecurringEvent
```csharp
using FluentAssertions;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.ValueObjects;
using Xunit;

namespace RecurringEventsCalendar.Tests.Domain.Entities;

public class RecurringEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateEvent()
    {
        // Arrange
        var rule = new DayOfWeekExpression(DayOfWeek.Monday);

        // Act
        var evt = new RecurringEvent(
            "Reunião",
            "Descrição",
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            rule
        );

        // Assert
        evt.Title.Should().Be("Reunião");
        evt.StartTime.Should().Be(new TimeOnly(9, 0));
        evt.EndTime.Should().Be(new TimeOnly(10, 0));
        evt.RecurrenceRule.Should().Be(rule);
    }

    [Fact]
    public void Constructor_WithEndTimeBeforeStartTime_ShouldThrowException()
    {
        // Arrange
        var rule = new DayOfWeekExpression(DayOfWeek.Monday);

        // Act
        Action act = () => new RecurringEvent(
            "Evento",
            "Descrição",
            new TimeOnly(10, 0),
            new TimeOnly(9, 0), // Termina antes de começar
            rule
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*término*");
    }

    [Fact]
    public void OccursOn_WhenRuleMatches_ShouldReturnTrue()
    {
        // Arrange
        var rule = new DayOfWeekExpression(DayOfWeek.Friday);
        var evt = new RecurringEvent(
            "Reunião Semanal",
            "",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            rule
        );
        var friday = new DateOnly(2025, 1, 10);

        // Act
        var result = evt.OccursOn(friday);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OccursOn_BeforeStartDate_ShouldReturnFalse()
    {
        // Arrange
        var rule = new DailyExpression();
        var evt = new RecurringEvent(
            "Evento Diário",
            "",
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            rule,
            startDate: new DateOnly(2025, 2, 1)
        );
        var beforeStart = new DateOnly(2025, 1, 15);

        // Act
        var result = evt.OccursOn(beforeStart);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OccursOn_AfterEndDate_ShouldReturnFalse()
    {
        // Arrange
        var rule = new DailyExpression();
        var evt = new RecurringEvent(
            "Evento Temporário",
            "",
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            rule,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 1, 31)
        );
        var afterEnd = new DateOnly(2025, 2, 1);

        // Act
        var result = evt.OccursOn(afterEnd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OccursOn_WithinDateRange_ShouldFollowRule()
    {
        // Arrange
        var rule = new DayOfWeekExpression(DayOfWeek.Monday);
        var evt = new RecurringEvent(
            "Reunião",
            "",
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            rule,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 1, 31)
        );

        // Act & Assert
        evt.OccursOn(new DateOnly(2025, 1, 6)).Should().BeTrue();  // Segunda dentro do range
        evt.OccursOn(new DateOnly(2025, 1, 7)).Should().BeFalse(); // Terça dentro do range
        evt.OccursOn(new DateOnly(2025, 2, 3)).Should().BeFalse(); // Segunda fora do range
    }
}
```

### OneTimeEvent
```csharp
public class OneTimeEventTests
{
    [Fact]
    public void OccursOn_WithMatchingDate_ShouldReturnTrue()
    {
        // Arrange
        var targetDate = new DateOnly(2025, 1, 15);
        var evt = new OneTimeEvent(
            "Evento Único",
            "Descrição",
            targetDate,
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)
        );

        // Act
        var result = evt.OccursOn(targetDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OccursOn_WithDifferentDate_ShouldReturnFalse()
    {
        // Arrange
        var evt = new OneTimeEvent(
            "Evento Único",
            "Descrição",
            new DateOnly(2025, 1, 15),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)
        );
        var differentDate = new DateOnly(2025, 1, 16);

        // Act
        var result = evt.OccursOn(differentDate);

        // Assert
        result.Should().BeFalse();
    }
}
```

---

## 🧪 Testes do CalendarService

```csharp
using FluentAssertions;
using Moq;
using RecurringEventsCalendar.Application.Services;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;
using RecurringEventsCalendar.Domain.ValueObjects;
using Xunit;

namespace RecurringEventsCalendar.Tests.Application.Services;

public class CalendarServiceTests
{
    private readonly Mock<IEventRepository> _repositoryMock;
    private readonly CalendarService _service;

    public CalendarServiceTests()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _service = new CalendarService(_repositoryMock.Object);
    }

    [Fact]
    public void GetEventsForDate_WithRecurringEvent_ShouldReturnEvent()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 10); // Sexta-feira
        var recurringEvent = new RecurringEvent(
            "Reunião",
            "Descrição",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            new DayOfWeekExpression(DayOfWeek.Friday)
        );

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(new[] { recurringEvent });

        _repositoryMock
            .Setup(r => r.GetExceptionsForDate(date))
            .Returns(Enumerable.Empty<EventException>());

        _repositoryMock
            .Setup(r => r.GetAllOneTimeEvents())
            .Returns(Enumerable.Empty<OneTimeEvent>());

        // Act
        var events = _service.GetEventsForDate(date);

        // Assert
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("Reunião");
        events.First().Date.Should().Be(date);
        events.First().IsRecurring.Should().BeTrue();
    }

    [Fact]
    public void GetEventsForDate_WithException_ShouldExcludeEvent()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 10); // Sexta-feira
        var recurringEvent = new RecurringEvent(
            "Reunião",
            "Descrição",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            new DayOfWeekExpression(DayOfWeek.Friday)
        );

        var exception = new EventException(recurringEvent.Id, date, "Cancelado");

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(new[] { recurringEvent });

        _repositoryMock
            .Setup(r => r.GetExceptionsForDate(date))
            .Returns(new[] { exception });

        _repositoryMock
            .Setup(r => r.GetAllOneTimeEvents())
            .Returns(Enumerable.Empty<OneTimeEvent>());

        // Act
        var events = _service.GetEventsForDate(date);

        // Assert
        events.Should().BeEmpty("o evento foi excluído por uma exceção");
    }

    [Fact]
    public void GetEventsForDate_WithOneTimeEvent_ShouldReturnEvent()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 10);
        var oneTimeEvent = new OneTimeEvent(
            "Evento Especial",
            "Descrição",
            date,
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)
        );

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(Enumerable.Empty<RecurringEvent>());

        _repositoryMock
            .Setup(r => r.GetExceptionsForDate(date))
            .Returns(Enumerable.Empty<EventException>());

        _repositoryMock
            .Setup(r => r.GetAllOneTimeEvents())
            .Returns(new[] { oneTimeEvent });

        // Act
        var events = _service.GetEventsForDate(date);

        // Assert
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("Evento Especial");
        events.First().IsRecurring.Should().BeFalse();
    }

    [Fact]
    public void MoveRecurringEventInstance_ShouldCreateExceptionAndOneTimeEvent()
    {
        // Arrange
        var originalDate = new DateOnly(2025, 1, 10);
        var newDate = new DateOnly(2025, 1, 9);
        var recurringEvent = new RecurringEvent(
            "Reunião",
            "Descrição",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            new DayOfWeekExpression(DayOfWeek.Friday)
        );

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(new[] { recurringEvent });

        // Act
        _service.MoveRecurringEventInstance(
            recurringEvent.Id,
            originalDate,
            newDate
        );

        // Assert
        _repositoryMock.Verify(
            r => r.AddException(It.Is<EventException>(
                e => e.RecurringEventId == recurringEvent.Id && e.ExceptionDate == originalDate
            )),
            Times.Once
        );

        _repositoryMock.Verify(
            r => r.AddOneTimeEvent(It.Is<OneTimeEvent>(
                e => e.Date == newDate
            )),
            Times.Once
        );
    }

    [Fact]
    public void MoveRecurringEventInstance_WhenEventDoesNotOccur_ShouldThrowException()
    {
        // Arrange
        var recurringEvent = new RecurringEvent(
            "Reunião",
            "Descrição",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            new DayOfWeekExpression(DayOfWeek.Friday)
        );

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(new[] { recurringEvent });

        var monday = new DateOnly(2025, 1, 6); // Não é sexta-feira

        // Act
        Action act = () => _service.MoveRecurringEventInstance(
            recurringEvent.Id,
            monday,
            monday.AddDays(1)
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não ocorre*");
    }

    [Fact]
    public void GetEventsForDateRange_ShouldReturnAllEventsInRange()
    {
        // Arrange
        var recurringEvent = new RecurringEvent(
            "Daily Event",
            "",
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            new DailyExpression()
        );

        _repositoryMock
            .Setup(r => r.GetAllRecurringEvents())
            .Returns(new[] { recurringEvent });

        _repositoryMock
            .Setup(r => r.GetExceptionsForDate(It.IsAny<DateOnly>()))
            .Returns(Enumerable.Empty<EventException>());

        _repositoryMock
            .Setup(r => r.GetAllOneTimeEvents())
            .Returns(Enumerable.Empty<OneTimeEvent>());

        // Act
        var events = _service.GetEventsForDateRange(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 5)
        );

        // Assert
        events.Should().HaveCount(5, "evento ocorre diariamente por 5 dias");
    }
}
```

---

## 🧪 Testes de Integração

```csharp
using FluentAssertions;
using RecurringEventsCalendar.Application.Services;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.ValueObjects;
using RecurringEventsCalendar.Infrastructure.Repositories;
using Xunit;

namespace RecurringEventsCalendar.Tests.Integration;

public class CalendarIntegrationTests
{
    [Fact]
    public void FullWorkflow_CreateMoveCancel_ShouldWorkCorrectly()
    {
        // Arrange
        var repository = new InMemoryEventRepository();
        var service = new CalendarService(repository);

        // 1. Criar evento recorrente
        var weeklyMeeting = new RecurringEvent(
            "Reunião Semanal",
            "Alinhamento de equipe",
            new TimeOnly(14, 0),
            new TimeOnly(15, 0),
            new DayOfWeekExpression(DayOfWeek.Friday),
            startDate: new DateOnly(2025, 1, 1)
        );

        repository.AddRecurringEvent(weeklyMeeting);

        // 2. Verificar que evento ocorre nas sextas
        var friday1 = new DateOnly(2025, 1, 10);
        var friday2 = new DateOnly(2025, 1, 17);
        
        service.GetEventsForDate(friday1).Should().HaveCount(1);
        service.GetEventsForDate(friday2).Should().HaveCount(1);

        // 3. Mover instância de 17/01 para 16/01
        service.MoveRecurringEventInstance(
            weeklyMeeting.Id,
            friday2,
            new DateOnly(2025, 1, 16)
        );

        // 4. Verificar resultado
        service.GetEventsForDate(friday2).Should().BeEmpty("evento foi movido");
        service.GetEventsForDate(new DateOnly(2025, 1, 16)).Should().HaveCount(1);

        // 5. Cancelar instância de 10/01
        service.CancelRecurringEventInstance(weeklyMeeting.Id, friday1, "Teste");

        // 6. Verificar cancelamento
        service.GetEventsForDate(friday1).Should().BeEmpty("evento foi cancelado");

        // 7. Verificar que próximas sextas não foram afetadas
        var friday3 = new DateOnly(2025, 1, 24);
        service.GetEventsForDate(friday3).Should().HaveCount(1, "evento continua nas outras sextas");
    }

    [Fact]
    public void ComplexSchedule_MultipleEventsAndExceptions_ShouldWorkCorrectly()
    {
        // Arrange
        var repository = new InMemoryEventRepository();
        var service = new CalendarService(repository);

        // Evento 1: Diário
        var dailyStandup = new RecurringEvent(
            "Daily",
            "",
            new TimeOnly(9, 0),
            new TimeOnly(9, 15),
            new DailyExpression(),
            startDate: new DateOnly(2025, 1, 1)
        );

        // Evento 2: Toda sexta
        var weeklyReview = new RecurringEvent(
            "Weekly Review",
            "",
            new TimeOnly(16, 0),
            new TimeOnly(17, 0),
            new DayOfWeekExpression(DayOfWeek.Friday),
            startDate: new DateOnly(2025, 1, 1)
        );

        repository.AddRecurringEvent(dailyStandup);
        repository.AddRecurringEvent(weeklyReview);

        // Act
        var friday = new DateOnly(2025, 1, 10);
        var events = service.GetEventsForDate(friday);

        // Assert
        events.Should().HaveCount(2, "sexta tem daily E review");
        events.Should().Contain(e => e.Title == "Daily");
        events.Should().Contain(e => e.Title == "Weekly Review");
        events.Should().BeInAscendingOrder(e => e.StartTime);
    }
}
```

---

## 📊 Cobertura de Testes

Para medir cobertura:

```bash
# Instalar ferramenta
dotnet tool install --global dotnet-reportgenerator-globaltool

# Executar testes com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Gerar relatório
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage -reporttypes:Html

# Abrir relatório
start coverage/index.html
```

---

## ✅ Checklist de Testes

- [x] Temporal Expressions básicas
- [x] Temporal Expressions compostas
- [x] Entidades (RecurringEvent, OneTimeEvent)
- [x] EventException
- [x] CalendarService (unitário)
- [x] CalendarService (integração)
- [x] Repositório
- [x] Casos edge (datas inválidas, intervalos zerados)
- [x] Mover instâncias
- [x] Cancelar instâncias

---

**Testes garantem robustez e facilitam refatorações!** ✅
