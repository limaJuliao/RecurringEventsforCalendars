# Casos de Uso Avançados

Exemplos práticos de como usar o padrão para cenários reais do dia a dia.

## 📚 Índice
1. [Reuniões Corporativas Complexas](#1-reuniões-corporativas-complexas)
2. [Sistema de Turnos](#2-sistema-de-turnos)
3. [Lembretes de Medicação](#3-lembretes-de-medicação)
4. [Manutenção Preventiva](#4-manutenção-preventiva)
5. [Horários de Aulas](#5-horários-de-aulas)
6. [Sistema de Backup Automático](#6-sistema-de-backup-automático)

---

## 1. Reuniões Corporativas Complexas

### Cenário
"Sprint Planning toda segunda-feira às 9h, mas não em feriados nem na primeira semana do mês."

### Implementação
```csharp
// 1. Criar expressão: Todas as segundas-feiras
var allMondays = new DayOfWeekExpression(DayOfWeek.Monday);

// 2. Excluir primeira semana do mês (dias 1-7)
var firstWeek = new DayOfMonthExpression(1, 2, 3, 4, 5, 6, 7);
var mondaysNotFirstWeek = new DifferenceExpression(allMondays, firstWeek);

// 3. Criar evento
var sprintPlanning = new RecurringEvent(
    "Sprint Planning",
    "Planejamento da sprint",
    new TimeOnly(9, 0),
    new TimeOnly(11, 0),
    mondaysNotFirstWeek,
    startDate: new DateOnly(2025, 1, 1)
);

repository.AddRecurringEvent(sprintPlanning);

// 4. Adicionar exceções para feriados
var feriados = new[] {
    new DateOnly(2025, 1, 1),  // Ano Novo
    new DateOnly(2025, 4, 21), // Tiradentes
    new DateOnly(2025, 12, 25) // Natal
};

foreach (var feriado in feriados)
{
    if (sprintPlanning.OccursOn(feriado))
    {
        calendarService.CancelRecurringEventInstance(
            sprintPlanning.Id, 
            feriado, 
            "Feriado Nacional"
        );
    }
}
```

### Resultado
```
Janeiro 2025:
- 06/01 (Segunda) ✅ Sprint Planning
- 13/01 (Segunda) ✅ Sprint Planning
- 20/01 (Segunda) ✅ Sprint Planning
- 27/01 (Segunda) ✅ Sprint Planning

Fevereiro 2025:
- 03/02 (Segunda) ❌ Primeira semana (excluído pela regra)
- 10/02 (Segunda) ✅ Sprint Planning
- 17/02 (Segunda) ✅ Sprint Planning
```

---

## 2. Sistema de Turnos

### Cenário
"Turnos de 12 horas alternados: João trabalha dias ímpares, Maria dias pares."

### Implementação
```csharp
// Turno de João: Dias ímpares do mês
public class OddDaysExpression : ITemporalExpression
{
    public bool Includes(DateOnly date)
    {
        return date.Day % 2 != 0;
    }
}

// Turno de Maria: Dias pares do mês
public class EvenDaysExpression : ITemporalExpression
{
    public bool Includes(DateOnly date)
    {
        return date.Day % 2 == 0;
    }
}

// Criar turnos
var turnoJoao = new RecurringEvent(
    "Turno - João",
    "Plantão de 12h",
    new TimeOnly(8, 0),
    new TimeOnly(20, 0),
    new OddDaysExpression(),
    startDate: new DateOnly(2025, 1, 1)
);

var turnoMaria = new RecurringEvent(
    "Turno - Maria",
    "Plantão de 12h",
    new TimeOnly(8, 0),
    new TimeOnly(20, 0),
    new EvenDaysExpression(),
    startDate: new DateOnly(2025, 1, 1)
);

repository.AddRecurringEvent(turnoJoao);
repository.AddRecurringEvent(turnoMaria);

// João precisa trocar com Maria no dia 15
calendarService.MoveRecurringEventInstance(
    turnoJoao.Id,
    originalDate: new DateOnly(2025, 1, 15),
    newDate: new DateOnly(2025, 1, 16)
);

calendarService.MoveRecurringEventInstance(
    turnoMaria.Id,
    originalDate: new DateOnly(2025, 1, 16),
    newDate: new DateOnly(2025, 1, 15)
);
```

### Resultado
```
Janeiro 2025:
01: João
02: Maria
03: João
...
15: Maria (trocado)
16: João (trocado)
17: João
18: Maria
```

---

## 3. Lembretes de Medicação

### Cenário
"Tomar medicação de 8 em 8 horas por 10 dias."

### Implementação
```csharp
// Expressão: A cada 8 horas = 3 vezes por dia
var startDate = new DateOnly(2025, 1, 10);
var dailyMedication = new DailyExpression();

var morning = new RecurringEvent(
    "Medicação - Manhã",
    "Tomar 1 comprimido de antibiótico",
    new TimeOnly(8, 0),
    new TimeOnly(8, 15),
    dailyMedication,
    startDate: startDate,
    endDate: startDate.AddDays(10)
);

var afternoon = new RecurringEvent(
    "Medicação - Tarde",
    "Tomar 1 comprimido de antibiótico",
    new TimeOnly(16, 0),
    new TimeOnly(16, 15),
    dailyMedication,
    startDate: startDate,
    endDate: startDate.AddDays(10)
);

var night = new RecurringEvent(
    "Medicação - Noite",
    "Tomar 1 comprimido de antibiótico",
    new TimeOnly(0, 0),
    new TimeOnly(0, 15),
    dailyMedication,
    startDate: startDate,
    endDate: startDate.AddDays(10)
);

repository.AddRecurringEvent(morning);
repository.AddRecurringEvent(afternoon);
repository.AddRecurringEvent(night);
```

### Alternativa: Intervalo Exato de 8h
```csharp
public class HourIntervalExpression : ITemporalExpression
{
    private readonly DateTimeOffset _startDateTime;
    private readonly int _intervalHours;

    public HourIntervalExpression(DateOnly startDate, TimeOnly startTime, int intervalHours)
    {
        _startDateTime = new DateTimeOffset(
            startDate.ToDateTime(startTime), 
            TimeSpan.Zero
        );
        _intervalHours = intervalHours;
    }

    public bool Includes(DateOnly date)
    {
        // Verifica se algum momento do dia corresponde ao intervalo
        for (int hour = 0; hour < 24; hour += _intervalHours)
        {
            var checkTime = date.ToDateTime(new TimeOnly(hour, 0));
            var hoursSinceStart = (checkTime - _startDateTime.DateTime).TotalHours;
            
            if (hoursSinceStart >= 0 && hoursSinceStart % _intervalHours == 0)
                return true;
        }
        return false;
    }
}
```

---

## 4. Manutenção Preventiva

### Cenário
"Trocar óleo do carro a cada 5.000km OU a cada 6 meses, o que ocorrer primeiro."

### Implementação
```csharp
// Abordagem: Combinar evento temporal com evento manual
public class MaintenanceService
{
    private readonly CalendarService _calendar;
    private int _currentKm = 0;
    private Guid _nextMaintenanceEventId;

    public void ScheduleNextMaintenance(DateOnly lastMaintenanceDate)
    {
        // Criar evento: Daqui a 6 meses
        var sixMonthsLater = lastMaintenanceDate.AddMonths(6);
        
        var maintenance = new OneTimeEvent(
            "Manutenção Programada do Veículo",
            "Trocar óleo e filtros",
            sixMonthsLater,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0)
        );

        _repository.AddOneTimeEvent(maintenance);
        _nextMaintenanceEventId = maintenance.Id;
    }

    public void RegisterKilometers(int km)
    {
        _currentKm += km;

        // Se atingiu 5.000km, antecipar manutenção
        if (_currentKm >= 5000)
        {
            var urgentMaintenance = new OneTimeEvent(
                "Manutenção URGENTE do Veículo",
                $"5.000km atingidos ({_currentKm}km rodados)",
                DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                new TimeOnly(9, 0),
                new TimeOnly(11, 0)
            );

            _repository.AddOneTimeEvent(urgentMaintenance);
            
            // Cancelar manutenção programada anterior
            // (lógica de cancelamento)
        }
    }
}
```

---

## 5. Horários de Aulas

### Cenário
"Aula de Matemática: Segundas e Quartas às 8h, mas na semana de provas mover para 10h."

### Implementação
```csharp
// Regra base: Segunda e Quarta
var mathDays = new UnionExpression(
    new DayOfWeekExpression(DayOfWeek.Monday),
    new DayOfWeekExpression(DayOfWeek.Wednesday)
);

var mathClass = new RecurringEvent(
    "Matemática - Prof. Silva",
    "Álgebra Linear",
    new TimeOnly(8, 0),
    new TimeOnly(9, 30),
    mathDays,
    startDate: new DateOnly(2025, 1, 1),
    endDate: new DateOnly(2025, 6, 30) // Fim do semestre
);

repository.AddRecurringEvent(mathClass);

// Semana de provas: 15-19/03
var examWeek = new[] {
    new DateOnly(2025, 3, 17), // Segunda
    new DateOnly(2025, 3, 19)  // Quarta
};

foreach (var date in examWeek)
{
    calendarService.MoveRecurringEventInstance(
        mathClass.Id,
        originalDate: date,
        newDate: date, // Mesma data
        newStartTime: new TimeOnly(10, 0), // Muda apenas horário
        newEndTime: new TimeOnly(11, 30)
    );
}
```

### Horário Completo da Semana
```csharp
public class WeeklyScheduleBuilder
{
    public RecurringEvent CreateClass(
        string subject,
        DayOfWeek[] days,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        var dayExpressions = days
            .Select(d => new DayOfWeekExpression(d))
            .ToArray();

        var schedule = days.Length == 1
            ? (ITemporalExpression)dayExpressions[0]
            : new UnionExpression(dayExpressions);

        return new RecurringEvent(
            subject,
            $"Aula de {subject}",
            startTime,
            endTime,
            schedule,
            startDate: new DateOnly(2025, 2, 1),
            endDate: new DateOnly(2025, 6, 30)
        );
    }
}

// Uso:
var builder = new WeeklyScheduleBuilder();

var classes = new[]
{
    builder.CreateClass("Matemática", 
        new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, 
        new TimeOnly(8, 0), new TimeOnly(9, 30)),
    
    builder.CreateClass("Física", 
        new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, 
        new TimeOnly(10, 0), new TimeOnly(11, 30)),
    
    builder.CreateClass("Programação", 
        new[] { DayOfWeek.Friday }, 
        new TimeOnly(14, 0), new TimeOnly(17, 0))
};

foreach (var cls in classes)
    repository.AddRecurringEvent(cls);
```

---

## 6. Sistema de Backup Automático

### Cenário
"Backup incremental diário às 2h, backup completo todo domingo às 3h."

### Implementação
```csharp
// Backup diário (incremental)
var dailyBackup = new RecurringEvent(
    "Backup Incremental",
    "Backup automático das alterações diárias",
    new TimeOnly(2, 0),
    new TimeOnly(2, 30),
    new DailyExpression()
);

// Backup semanal (completo)
var weeklyBackup = new RecurringEvent(
    "Backup Completo",
    "Backup completo de todo o sistema",
    new TimeOnly(3, 0),
    new TimeOnly(5, 0),
    new DayOfWeekExpression(DayOfWeek.Sunday)
);

repository.AddRecurringEvent(dailyBackup);
repository.AddRecurringEvent(weeklyBackup);

// Handler de eventos para executar backups
public class BackupScheduler
{
    public async Task ExecuteScheduledBackups()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var events = _calendarService.GetEventsForDate(today);

        foreach (var evt in events)
        {
            if (evt.Title.Contains("Backup"))
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                
                if (now >= evt.StartTime && now <= evt.EndTime)
                {
                    if (evt.Title.Contains("Incremental"))
                        await ExecuteIncrementalBackup();
                    else if (evt.Title.Contains("Completo"))
                        await ExecuteFullBackup();
                }
            }
        }
    }
}
```

### Política de Retenção Avançada
```csharp
// Backup com política 3-2-1: 
// - 3 cópias dos dados
// - 2 mídias diferentes
// - 1 cópia off-site

public class AdvancedBackupSchedule
{
    public void ConfigureBackups()
    {
        // Diário: Disco local
        var localDaily = new RecurringEvent(
            "Backup Local Diário",
            "Target: D:\\Backups\\Daily",
            new TimeOnly(2, 0),
            new TimeOnly(2, 30),
            new DailyExpression()
        );

        // Semanal: NAS
        var nasWeekly = new RecurringEvent(
            "Backup NAS Semanal",
            "Target: \\\\nas\\backups\\weekly",
            new TimeOnly(3, 0),
            new TimeOnly(4, 0),
            new DayOfWeekExpression(DayOfWeek.Sunday)
        );

        // Mensal: Cloud
        var cloudMonthly = new RecurringEvent(
            "Backup Cloud Mensal",
            "Target: Azure Blob Storage",
            new TimeOnly(4, 0),
            new TimeOnly(6, 0),
            new DayOfMonthExpression(1) // Primeiro dia do mês
        );

        repository.AddRecurringEvent(localDaily);
        repository.AddRecurringEvent(nasWeekly);
        repository.AddRecurringEvent(cloudMonthly);
    }
}
```

---

## 🎯 Caso de Uso Especial: Sincronização com Google Calendar

### Exportar para iCal/ICS
```csharp
public class ICalExporter
{
    public string ExportToICal(IEnumerable<RecurringEvent> events)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//RecurringEvents//NONSGML v1.0//EN");

        foreach (var evt in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{evt.Id}");
            sb.AppendLine($"SUMMARY:{evt.Title}");
            sb.AppendLine($"DESCRIPTION:{evt.Description}");
            
            // Converter Temporal Expression para RRULE
            var rrule = ConvertToRRule(evt.RecurrenceRule);
            sb.AppendLine($"RRULE:{rrule}");
            
            if (evt.StartDate.HasValue)
                sb.AppendLine($"DTSTART:{evt.StartDate.Value:yyyyMMdd}T{evt.StartTime:HHmmss}");
            
            if (evt.EndDate.HasValue)
                sb.AppendLine($"UNTIL:{evt.EndDate.Value:yyyyMMdd}");
            
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private string ConvertToRRule(ITemporalExpression expression)
    {
        return expression switch
        {
            DayOfWeekExpression dow => $"FREQ=WEEKLY;BYDAY={GetDays(dow)}",
            DailyExpression => "FREQ=DAILY",
            DayOfMonthExpression dom => $"FREQ=MONTHLY;BYMONTHDAY={GetDays(dom)}",
            IntervalExpression interval => $"FREQ=DAILY;INTERVAL={interval.Interval}",
            _ => "FREQ=DAILY" // Fallback
        };
    }
}
```

---

## 📊 Métricas e Análises

### Calcular Total de Ocorrências
```csharp
public class EventAnalytics
{
    public int CountOccurrences(RecurringEvent evt, DateOnly start, DateOnly end)
    {
        int count = 0;
        
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (evt.OccursOn(date))
                count++;
        }

        return count;
    }

    public TimeSpan CalculateTotalDuration(RecurringEvent evt, DateOnly start, DateOnly end)
    {
        var occurrences = CountOccurrences(evt, start, end);
        var durationPerOccurrence = evt.EndTime - evt.StartTime;
        
        return TimeSpan.FromHours(occurrences * durationPerOccurrence.TotalHours);
    }
}

// Uso:
var analytics = new EventAnalytics();

var totalMeetings = analytics.CountOccurrences(
    weeklyMeeting,
    new DateOnly(2025, 1, 1),
    new DateOnly(2025, 12, 31)
);

var totalHours = analytics.CalculateTotalDuration(
    weeklyMeeting,
    new DateOnly(2025, 1, 1),
    new DateOnly(2025, 12, 31)
);

Console.WriteLine($"Total de reuniões em 2025: {totalMeetings}");
Console.WriteLine($"Total de horas em reuniões: {totalHours.TotalHours:F1}h");
```

---

## 🔧 Utilitários Práticos

### Builder para Expressões Complexas
```csharp
public class TemporalExpressionBuilder
{
    private ITemporalExpression _expression;

    public TemporalExpressionBuilder Every(params DayOfWeek[] days)
    {
        _expression = new DayOfWeekExpression(days);
        return this;
    }

    public TemporalExpressionBuilder Except(params DayOfWeek[] days)
    {
        var excluded = new DayOfWeekExpression(days);
        _expression = new DifferenceExpression(_expression, excluded);
        return this;
    }

    public TemporalExpressionBuilder OnDays(params int[] days)
    {
        var dayExpression = new DayOfMonthExpression(days);
        _expression = new IntersectionExpression(_expression, dayExpression);
        return this;
    }

    public ITemporalExpression Build() => _expression;
}

// Uso fluente:
var expression = new TemporalExpressionBuilder()
    .Every(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
    .Except(DayOfWeek.Monday) // Remove segundas
    .Build();

// Resultado: Apenas Quartas e Sextas
```

---

**Estes casos de uso demonstram a versatilidade e poder do padrão Temporal Expressions!** 🎯
