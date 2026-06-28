# Varyo CMS — Proje Memory

## Proje Özeti
- **Ad**: Varyo CMS
- **Tip**: Multi-tenant, çoklu dil destekli Dinamik İçerik Yönetim Sistemi
- **Stack**: ASP.NET Core 8 MVC + Dapper + SQL Server (Docker)
- **Ortam**: macOS, VS Code, Docker Desktop, DBeaver
- **Mimari**: Clean Architecture (4 katman)

## Referans Dokümanlar
@docs/architecture.md
@docs/database-schema.md
@docs/content-types.md
@docs/api-design.md
@docs/multitenancy.md

## Aktif Görev Takibi
@docs/prompts/current-task.md

---

## Katman Yapısı

```
VaryoCms.sln
└── src/
    ├── VaryoCms.Domain/          ← Entity, Enum, Interface (bağımlılık yok)
    ├── VaryoCms.Application/     ← Use Case, DTO, Service Interface
    ├── VaryoCms.Infrastructure/  ← Dapper Repository, FileStorage, Media
    └── VaryoCms.Web/             ← ASP.NET Core MVC
db/
└── migrations/                     ← Numaralı SQL scriptleri
docker/
└── docker-compose.yml
docs/
└── prompts/
```

## Katman Bağımlılık Kuralları (ASLA İHLAL ETME)

- **Domain** → hiçbir projeye bağımlı değil
- **Application** → yalnızca Domain'e bağımlı
- **Infrastructure** → Application + Domain'e bağımlı
- **Web** → yalnızca Application'a bağımlı (Infrastructure'a doğrudan erişim yok)

---

## Mimari Kurallar

### Controller
- Maksimum 80 satır
- Sıfır business logic
- Sadece: input al → service çağır → View/JSON döndür
- Constructor injection zorunlu
- Async all the way

### Application Service
- Yalnızca use case orkestrasyon
- Web katmanına DTO döner, Domain Entity dönemez
- Transaction yönetimi: `IUnitOfWork`
- Validation: FluentValidation

### Repository (Infrastructure)
- Sadece Dapper — EF Core yasak
- `IDbConnectionFactory.CreateConnection()` pattern
- Parameterized query zorunlu — SQL'de string interpolation yasak
- `using var conn = ...` — connection'ı açık tutma
- Soft delete: her sorguda `is_deleted = 0` filtresi

### Multi-Tenancy
- `tenants` ve `system_admins` dışındaki her tabloda `tenant_id INT NOT NULL`
- `ITenantContext` her zaman constructor injection ile gelir
- Tenant subdomain'den çözümlenir → middleware set eder
- Hiçbir zaman URL param ile tenant belirleme
- SystemAdmin rolü dışında cross-tenant sorgu yapma

### Multi-Language
- Translation tabloları: `{entity}_translations` + `language_code CHAR(5)`
- Her sorguda `ILanguageContext.CurrentCode` ile join
- Sözlük: `dictionary_entries` + `dictionary_translations`

---

## İsimlendirme

| Şey | Kural | Örnek |
|---|---|---|
| C# Class | PascalCase | `ContentTypeService` |
| Interface | `I` prefix | `IContentTypeRepository` |
| DTO | suffix ile | `CreateContentTypeRequest`, `ContentTypeDto` |
| DB Tablo | snake_case plural | `content_types` |
| DB Kolon | snake_case | `tenant_id`, `created_at` |
| DB FK | `{tablo_tekil}_id` | `content_type_id` |
| Migration | numaralı prefix | `001_init_tenants.sql` |
| Controller | `{Feature}Controller` | `ContentTypeController` |

---

## Kod Standartları

```csharp
// Her zaman: async/await + nullable check
public async Task<ContentTypeDto?> GetByIdAsync(int id, CancellationToken ct = default)
{
    var result = await _service.GetByIdAsync(id, ct);
    if (result is null) return NotFound();
    ...
}

// Her zaman: parameterized Dapper + tenant_id filtresi
var result = await conn.QueryFirstOrDefaultAsync<ContentType>(
    @"SELECT * FROM content_types
      WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
    new { Id = id, TenantId = _tenantContext.TenantId });

// Application layer'da Result<T> pattern
public static Result<T> Success(T value) => new(true, value, null);
public static Result<T> Failure(string error) => new(false, default, error);

// YASAK: string interpolation SQL içinde
// $"SELECT * FROM content_types WHERE id = {id}"  ← YAPMA
```

---

## Desteklenen Field Tipleri
Detay: @docs/content-types.md

Özet: `Text`, `RichText`, `Markdown`, `Number`, `Decimal`, `Boolean`,
`Date`, `DateTime`, `Time`, `DateRange`, `Email`, `URL`, `Phone`, `Color`,
`JSON`, `CodeSnippet`, `Image`, `Video`, `Audio`, `File`, `Gallery`,
`Select`, `MultiSelect`, `Tags`, `Relation`, `MultiRelation`,
`Slug`, `Password`, `GeoLocation`, `Rating`

---

## Dev Komutları

```bash
# SQL Server başlat
docker-compose -f docker/docker-compose.yml up -d

# Build
dotnet build VaryoCms.sln

# Çalıştır (port: http://localhost:5267)
dotnet run --project src/VaryoCms.Web

# Arka planda çalıştır (Claude bu formu kullanır)
dotnet run --project src/VaryoCms.Web --no-build &

# Yeniden başlat (kod değişikliklerinden sonra Razor/JS cache'i temizlemek için gerekli)
pkill -f "dotnet.*VaryoCms.Web" 2>/dev/null; sleep 1
dotnet run --project src/VaryoCms.Web --no-build &

# Test
dotnet test

# Migration uygula (otomatik runner YOK — manuel sqlcmd; migration'lar container'a /migrations mount'lu)
docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Varyo_Dev2024!" -C \
  -d Varyo -i /migrations/NNN_x.sql -b
# (Tam migration listesi ve adım adım kurulum için: README.md)
```

> **Ne zaman yeniden başlatmak gerekir?**
> - Middleware, DI kaydı veya `appsettings.json` değişikliği
> - Razor view değişikliği (`asp-append-version` JS/CSS cache buster'ı için de)
> - Migration uygulandıktan sonra (DB-backed localizer cache'i için)
> - UI dil çevirisi seed'lendikten sonra (IUiTranslationStore cache'ini sıfırlamak için)

---

## Kullanıcı Rolleri

| Rol | Erişim |
|---|---|
| `SystemAdmin` | Tüm tenant'larda her şey |
| `TenantAdmin` | Kendi tenant'ında her şey: Settings, Dictionary, Users, API |
| `Editor` | İzin verilen content type'lar (can_read/create/update/delete) |
| `Viewer` | İzin verilen content type'larda salt okunur |

---

## Dinamik Sol Menü Kuralı
Sol menü `content_types` tablosundan üretilir:
```sql
SELECT id, name, slug, icon, sort_order
FROM content_types
WHERE tenant_id = @TenantId AND is_published = 1 AND is_deleted = 0
ORDER BY sort_order ASC
```
`user_content_type_permissions` ile filtrelenir. Genel Ayarlar ve Sözlük menüsü sadece TenantAdmin+ görür.

---

## YAPMA Listesi
- NuGet package listelmeden kurma
- EF Core veya başka ORM önerme
- ViewBag kullanma — typed ViewModel zorunlu
- `tenant_id` filtresini atlama
- Infrastructure'ı doğrudan Web'den çağırma
- Uygulama katmanında SQL yazma
- Controller'a business logic koyma
- Uygulanmış migration dosyasını değiştirme — yeni migration yaz
