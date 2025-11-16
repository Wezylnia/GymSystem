# GymSystem Database Infrastructure

Bu dokümantasyon, LifomatikCore projesindeki database connection mantığının GymSystem projesine nasıl uyarlandığını açıklar.

## Yapı

### 1. **GymSystem.Persistance** (Data Access Layer)
Database erişim katmanıdır. Entity Framework Core DbContext'leri ve database konfigürasyonları burada bulunur.

#### Dosyalar:
- **Contexts/GymDbContext.cs**: Ana database context
- **Contexts/GymDbContextFactory.cs**: Migration işlemleri için design-time factory
- **Database/ConnectionStringManager.cs**: Connection string yönetimi
- **ServiceCollectionExtensions.cs**: Dependency injection için extension metodlar

### 2. **GymSystem.Infastructure** (Infrastructure Layer)
Tüm infrastructure servislerini (database, caching, logging vb.) bir araya getiren katmandır.

#### Dosyalar:
- **Extensions/ServiceCollectionExtensions.cs**: Infrastructure servislerini kaydetmek için extension metod

## Database: PostgreSQL

Proje **PostgreSQL** database kullanmaktadır.

### Connection String Bilgileri:
- **Host**: localhost
- **Database**: postgres
- **Username**: postgres
- **Password**: 123

## Kullanım

### Program.cs'de Servis Kaydı (GymSystem.Api)

```csharp
using GymSystem.Infastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure servisleri ekle
builder.Services.AddInfrastructureServices(builder.Configuration, "appsettings.json");
```

### appsettings.json Konfigürasyonu

```json
{
  "ConnectionStrings": {
    "GymDbContext": "Host=localhost;Database=postgres;Username=postgres;Password=123"
  },
  "Data": {
    "Gym": {
      "MigrationsAssembly": "GymSystem.Persistance"
    }
  }
}
```

### Yeni DbContext Ekleme

1. **Yeni DbContext oluştur:**
```csharp
public class AnotherDbContext : DbContext
{
    public AnotherDbContext(DbContextOptions<AnotherDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        base.OnModelCreating(modelBuilder);
    }
}
```

2. **ServiceCollectionExtensions.cs'e ekle:**
```csharp
public static void AddPersistenceInfrastructure(...)
{
    // Mevcut context
    serviceCollection.AddDbContext<GymDbContext>(...);
    
    // Yeni context
    serviceCollection.AddDbContext<AnotherDbContext>((serviceProvider, options) =>
    {
        options.ConfigureDatabase("AnotherDbContext", configuration["Data:Another:MigrationsAssembly"], settingsFileName);
    });
}

public static void ConfigureDatabase(...)
{
    // Mevcut configuration
    if (contextName == "GymDbContext") { ... }
    
    // Yeni configuration
    if (contextName == "AnotherDbContext")
    {
        var connectionStringManager = new ConnectionStringManager(
            connectionStringKey: "AnotherDbContext", 
            settingsFileName: settingsFileName);
        
        string connectionString = connectionStringManager.GetConnectionString();
        builder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            if (!string.IsNullOrEmpty(migrationAssembly))
            {
                npgsqlOptions.MigrationsAssembly(migrationAssembly);
            }
        });
    }
}
```

3. **appsettings.json'a ekle:**
```json
{
  "ConnectionStrings": {
    "GymDbContext": "Host=localhost;Database=postgres;Username=postgres;Password=123",
    "AnotherDbContext": "Host=localhost;Database=another_db;Username=postgres;Password=123"
  },
  "Data": {
    "Gym": { "MigrationsAssembly": "GymSystem.Persistance" },
    "Another": { "MigrationsAssembly": "GymSystem.Persistance" }
  }
}
```

## Migration İşlemleri

### Migration Oluşturma
```bash
dotnet ef migrations add InitialCreate --project GymSystem.Persistance --startup-project GymSystem.Api
```

### Database Güncelleme
```bash
dotnet ef database update --project GymSystem.Persistance --startup-project GymSystem.Api
```

### Migration Listesini Görüntüleme
```bash
dotnet ef migrations list --project GymSystem.Persistance --startup-project GymSystem.Api
```

### Migration Geri Alma
```bash
dotnet ef database update <PreviousMigrationName> --project GymSystem.Persistance --startup-project GymSystem.Api
```

### Migration'ı Silme
```bash
dotnet ef migrations remove --project GymSystem.Persistance --startup-project GymSystem.Api
```

## PostgreSQL Özel Notlar

### 1. **Naming Convention**
PostgreSQL'de genelde lowercase ve snake_case kullanılır:
```csharp
entity.ToTable("members"); // lowercase
entity.ToTable("member_subscriptions"); // snake_case
```

### 2. **Default Schema**
PostgreSQL'de default schema "public"tir:
```csharp
modelBuilder.HasDefaultSchema("public");
```

### 3. **Identity Column**
PostgreSQL'de auto-increment için:
```csharp
entity.Property(e => e.Id).UseIdentityAlwaysColumn();
// veya
entity.Property(e => e.Id).UseIdentityByDefaultColumn();
```

### 4. **Timestamp**
PostgreSQL'de current timestamp için:
```csharp
entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
// veya
entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
```

## LifomatikCore'dan Farklar

1. **FordOtosan NuGet Paketleri Kullanılmıyor**: 
   - `FordOtosan.WebFramework.Common` yerine native .NET kullanılıyor
   - `FordOtosan.WebFramework.Data.Relational` yerine standart EF Core kullanılıyor

2. **Basitleştirilmiş Yapı**:
   - Oracle yerine **PostgreSQL** kullanılıyor
   - CyberArk entegrasyonu yok
   - Daha temiz ve anlaşılır kod

3. **Aynı Mantık**:
   - Multiple DbContext desteği
   - Connection string yönetimi
   - Migration assembly konfigürasyonu
   - Dependency injection pattern

## Örnek API Controller Kullanımı

```csharp
[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly GymDbContext _context;

    public MembersController(GymDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Member>>> GetMembers()
    {
        var members = await _context.Members.ToListAsync();
        return Ok(members);
    }
}
```

## API Endpoints

### Members Controller
- **GET** `/api/members` - Tüm aktif üyeleri getir
- **GET** `/api/members/{id}` - Belirli bir üyeyi getir
- **POST** `/api/members` - Yeni üye ekle
- **PUT** `/api/members/{id}` - Üye bilgilerini güncelle
- **DELETE** `/api/members/{id}` - Üyeyi sil (soft delete)
- **GET** `/api/members/health` - Database bağlantısını test et

### Health Check Endpoint
API'yi test etmek için:
```bash
curl https://localhost:7xxx/api/members/health
```

Başarılı yanıt:
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

## PostgreSQL Bağlantı Testi

pgAdmin veya terminal üzerinden bağlantıyı test edebilirsiniz:

```bash
psql -h localhost -U postgres -d postgres
```

Şifre: `123`
