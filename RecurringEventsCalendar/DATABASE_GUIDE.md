# Guia de Evolução: Da Memória para Banco de Dados

Este guia mostra como evoluir a aplicação de repositório em memória para persistência real, mantendo os princípios SOLID.

## 🎯 Vantagem do DIP (Dependency Inversion Principle)

Graças ao uso de `IEventRepository`, podemos trocar a implementação **sem alterar** o `CalendarService` ou as entidades de domínio.

## 📊 Opção 1: Entity Framework Core (SQL Server)

### 1. Adicionar Pacotes NuGet
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. Criar DbContext
```csharp
// Infrastructure/Data/CalendarDbContext.cs
using Microsoft.EntityFrameworkCore;
using RecurringEventsCalendar.Domain.Entities;

namespace RecurringEventsCalendar.Infrastructure.Data;

public class CalendarDbContext : DbContext
{
    public DbSet<RecurringEvent> RecurringEvents { get; set; }
    public DbSet<OneTimeEvent> OneTimeEvents { get; set; }
    public DbSet<EventException> EventExceptions { get; set; }

    public CalendarDbContext(DbContextOptions<CalendarDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuração para RecurringEvent
        modelBuilder.Entity<RecurringEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Serializar TemporalExpression como JSON
            entity.Property(e => e.RecurrenceRule)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<ITemporalExpression>(v, JsonOptions)
                );
            
            // Converter DateOnly para Date (SQL Server)
            entity.Property(e => e.StartDate)
                .HasConversion<DateOnlyConverter>();
            
            entity.Property(e => e.EndDate)
                .HasConversion<DateOnlyConverter>();
        });

        // Configuração para OneTimeEvent
        modelBuilder.Entity<OneTimeEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Date)
                .HasConversion<DateOnlyConverter>();
        });

        // Configuração para EventException
        modelBuilder.Entity<EventException>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ExceptionDate)
                .HasConversion<DateOnlyConverter>();
            
            entity.HasIndex(e => e.RecurringEventId);
            entity.HasIndex(e => e.ExceptionDate);
        });
    }
}

// Converter DateOnly <-> DateTime
public class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter() 
        : base(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d))
    { }
}
```

### 3. Implementar Repositório com EF Core
```csharp
// Infrastructure/Repositories/EfEventRepository.cs
using Microsoft.EntityFrameworkCore;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;
using RecurringEventsCalendar.Infrastructure.Data;

namespace RecurringEventsCalendar.Infrastructure.Repositories;

public class EfEventRepository : IEventRepository
{
    private readonly CalendarDbContext _context;

    public EfEventRepository(CalendarDbContext context)
    {
        _context = context;
    }

    public void AddRecurringEvent(RecurringEvent recurringEvent)
    {
        _context.RecurringEvents.Add(recurringEvent);
        _context.SaveChanges();
    }

    public void AddOneTimeEvent(OneTimeEvent oneTimeEvent)
    {
        _context.OneTimeEvents.Add(oneTimeEvent);
        _context.SaveChanges();
    }

    public void AddException(EventException exception)
    {
        _context.EventExceptions.Add(exception);
        _context.SaveChanges();
    }

    public IEnumerable<RecurringEvent> GetAllRecurringEvents()
    {
        return _context.RecurringEvents
            .AsNoTracking()
            .ToList();
    }

    public IEnumerable<OneTimeEvent> GetAllOneTimeEvents()
    {
        return _context.OneTimeEvents
            .AsNoTracking()
            .ToList();
    }

    public IEnumerable<EventException> GetExceptionsForEvent(Guid recurringEventId)
    {
        return _context.EventExceptions
            .AsNoTracking()
            .Where(e => e.RecurringEventId == recurringEventId)
            .ToList();
    }

    public IEnumerable<EventException> GetExceptionsForDate(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        
        return _context.EventExceptions
            .AsNoTracking()
            .Where(e => e.ExceptionDate == date)
            .ToList();
    }
}
```

### 4. Atualizar Program.cs
```csharp
// Program.cs
using Microsoft.EntityFrameworkCore;
using RecurringEventsCalendar.Infrastructure.Data;
using RecurringEventsCalendar.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configurar DbContext
builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Registrar repositório (DIP em ação!)
builder.Services.AddScoped<IEventRepository, EfEventRepository>();
builder.Services.AddScoped<CalendarService>();

var app = builder.Build();

// Aplicar migrations automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    db.Database.Migrate();
}

app.Run();
```

### 5. Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CalendarEvents;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 6. Criar Migration
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 📊 Opção 2: Dapper (Micro-ORM)

Para quem prefere SQL puro com performance máxima:

```csharp
// Infrastructure/Repositories/DapperEventRepository.cs
using System.Data;
using System.Data.SqlClient;
using Dapper;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;

namespace RecurringEventsCalendar.Infrastructure.Repositories;

public class DapperEventRepository : IEventRepository
{
    private readonly string _connectionString;

    public DapperEventRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void AddRecurringEvent(RecurringEvent recurringEvent)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            INSERT INTO RecurringEvents (Id, Title, Description, StartTime, EndTime, RecurrenceRule, StartDate, EndDate)
            VALUES (@Id, @Title, @Description, @StartTime, @EndTime, @RecurrenceRule, @StartDate, @EndDate)
        ";

        connection.Execute(sql, new
        {
            recurringEvent.Id,
            recurringEvent.Title,
            recurringEvent.Description,
            recurringEvent.StartTime,
            recurringEvent.EndTime,
            RecurrenceRule = JsonSerializer.Serialize(recurringEvent.RecurrenceRule),
            recurringEvent.StartDate,
            recurringEvent.EndDate
        });
    }

    public IEnumerable<RecurringEvent> GetAllRecurringEvents()
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = "SELECT * FROM RecurringEvents";
        
        var events = connection.Query<RecurringEventDto>(sql)
            .Select(dto => new RecurringEvent(
                dto.Title,
                dto.Description,
                dto.StartTime,
                dto.EndTime,
                JsonSerializer.Deserialize<ITemporalExpression>(dto.RecurrenceRuleJson),
                dto.StartDate,
                dto.EndDate
            ));

        return events;
    }

    // ... demais métodos
}
```

---

## 📊 Opção 3: MongoDB (NoSQL)

Para flexibilidade máxima com documentos:

```bash
dotnet add package MongoDB.Driver
```

```csharp
// Infrastructure/Repositories/MongoEventRepository.cs
using MongoDB.Driver;
using RecurringEventsCalendar.Domain.Entities;
using RecurringEventsCalendar.Domain.Interfaces;

namespace RecurringEventsCalendar.Infrastructure.Repositories;

public class MongoEventRepository : IEventRepository
{
    private readonly IMongoCollection<RecurringEvent> _recurringEvents;
    private readonly IMongoCollection<OneTimeEvent> _oneTimeEvents;
    private readonly IMongoCollection<EventException> _exceptions;

    public MongoEventRepository(IMongoDatabase database)
    {
        _recurringEvents = database.GetCollection<RecurringEvent>("recurring_events");
        _oneTimeEvents = database.GetCollection<OneTimeEvent>("onetime_events");
        _exceptions = database.GetCollection<EventException>("exceptions");
    }

    public void AddRecurringEvent(RecurringEvent recurringEvent)
    {
        _recurringEvents.InsertOne(recurringEvent);
    }

    public IEnumerable<RecurringEvent> GetAllRecurringEvents()
    {
        return _recurringEvents.Find(_ => true).ToList();
    }

    public IEnumerable<EventException> GetExceptionsForDate(DateOnly date)
    {
        return _exceptions
            .Find(e => e.ExceptionDate == date)
            .ToList();
    }

    // ... demais métodos
}
```

---

## 🎯 Schema SQL Sugerido

```sql
-- Tabela principal de eventos recorrentes
CREATE TABLE RecurringEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    RecurrenceRuleJson NVARCHAR(MAX) NOT NULL, -- Serializado como JSON
    StartDate DATE NULL,
    EndDate DATE NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Tabela de eventos únicos (substituições)
CREATE TABLE OneTimeEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Date DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Índice para busca por data
CREATE INDEX IX_OneTimeEvents_Date ON OneTimeEvents(Date);

-- Tabela de exceções (exclusões)
CREATE TABLE EventExceptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    RecurringEventId UNIQUEIDENTIFIER NOT NULL,
    ExceptionDate DATE NOT NULL,
    Reason NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_EventExceptions_RecurringEvents 
        FOREIGN KEY (RecurringEventId) 
        REFERENCES RecurringEvents(Id) 
        ON DELETE CASCADE
);

-- Índices para performance
CREATE INDEX IX_EventExceptions_RecurringEventId ON EventExceptions(RecurringEventId);
CREATE INDEX IX_EventExceptions_ExceptionDate ON EventExceptions(ExceptionDate);
CREATE UNIQUE INDEX UX_EventExceptions_EventDate ON EventExceptions(RecurringEventId, ExceptionDate);
```

---

## 🚀 Performance: Otimizações

### 1. Cache para Expressões Temporais Frequentes
```csharp
// Application/Services/CachedCalendarService.cs
public class CachedCalendarService : CalendarService
{
    private readonly IMemoryCache _cache;

    public override IEnumerable<CalendarEventDto> GetEventsForDate(DateOnly date)
    {
        var cacheKey = $"events_{date:yyyy-MM-dd}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return base.GetEventsForDate(date);
        });
    }
}
```

### 2. Índices Compostos para Queries Frequentes
```sql
-- Acelerar busca de exceções por evento E data
CREATE INDEX IX_EventExceptions_Composite 
ON EventExceptions(RecurringEventId, ExceptionDate)
INCLUDE (Reason);
```

### 3. Materialização de Eventos Futuros (Hybrid Approach)
```csharp
// Para calendários com muitos acessos, pré-calcular próximos 90 dias
public class HybridEventRepository : IEventRepository
{
    // Gerar cache de eventos para os próximos 3 meses
    public void MaterializeFutureEvents(int days = 90)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        
        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(i);
            var events = GetEventsForDate(date);
            
            // Armazenar em tabela de cache
            _cache.Set($"materialized_{date}", events);
        }
    }
}
```

---

## 📈 Comparação de Performance

| Abordagem | Complexidade | Performance (10k eventos) | Uso de Disco |
|-----------|--------------|---------------------------|--------------|
| **Ingênua** (todas datas) | O(n) | 🐌 500ms | 💾 500MB |
| **Temporal Expressions** | O(r + e) | 🚀 5ms | 💾 5MB |
| **Hybrid** (cache 90 dias) | O(1) | ⚡ <1ms | 💾 10MB |

Onde:
- `n` = número total de instâncias
- `r` = número de regras recorrentes
- `e` = número de exceções

---

## ✅ Checklist de Migração

- [ ] Escolher estratégia de persistência (EF Core, Dapper, MongoDB)
- [ ] Criar DbContext/Connection Manager
- [ ] Implementar nova classe repositório
- [ ] Manter `IEventRepository` interface (DIP)
- [ ] Registrar no DI Container
- [ ] Criar migrations/scripts SQL
- [ ] Adicionar índices de performance
- [ ] Implementar cache (opcional)
- [ ] Testar com dados reais
- [ ] Monitorar performance

---

**O padrão permanece o mesmo, apenas a infraestrutura muda!** 🎯

Graças ao SOLID, especialmente **Dependency Inversion Principle**, podemos evoluir sem reescrever a lógica de negócio.
