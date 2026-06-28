---
name: dotnet-layered-arch
description: >
  Use when scaffolding a new feature across all Clean Architecture layers in Varyo CMS.
  Triggers on: "add feature", "yeni özellik ekle", "implement [X] from scratch",
  "scaffold [entity]". Generates Domain entity, Application service+DTO,
  Infrastructure Dapper repository, and Web Controller+ViewModel in one pass.
---

# Varyo CMS Clean Architecture Scaffolder

## Layer Generation Order
Always generate in this order — never skip layers:
1. **Domain**: Entity class + Repository interface
2. **Application**: DTO classes + Service interface + Service implementation
3. **Infrastructure**: Dapper Repository implementation + DI registration
4. **Web**: Controller + ViewModel + View skeleton + DI registration

## Domain Entity Template
```csharp
// VaryoCms.Domain/Entities/{Entity}.cs
namespace VaryoCms.Domain.Entities;

public class {Entity}
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    // ... domain properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

## Repository Interface Template (Domain layer)
```csharp
// VaryoCms.Domain/Interfaces/Repositories/I{Entity}Repository.cs
namespace VaryoCms.Domain.Interfaces.Repositories;

public interface I{Entity}Repository
{
    Task<{Entity}?> GetByIdAsync(int id, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<{Entity}>> GetAllAsync(int tenantId, CancellationToken ct = default);
    Task<int> CreateAsync({Entity} entity, CancellationToken ct = default);
    Task UpdateAsync({Entity} entity, CancellationToken ct = default);
    Task DeleteAsync(int id, int tenantId, CancellationToken ct = default);
}
```

## DTO Templates (Application layer)
```csharp
// Request
public record Create{Entity}Request(/* props */);
public record Update{Entity}Request(int Id, /* props */);

// Response
public record {Entity}Dto(int Id, /* props */, DateTime CreatedAt);

// Paged (if list endpoint needed)
public record {Entity}ListRequest(int Page = 1, int PageSize = 20, string? Search = null);
```

## Application Service Template
```csharp
// Interface
public interface I{Entity}Service
{
    Task<Result<{Entity}Dto?>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PagedResult<{Entity}Dto>>> GetAllAsync({Entity}ListRequest request, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(Create{Entity}Request request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Update{Entity}Request request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

// Implementation
public class {Entity}Service(I{Entity}Repository repository, ITenantContext tenantContext) : I{Entity}Service
{
    public async Task<Result<{Entity}Dto?>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, tenantContext.TenantId, ct);
        if (entity is null) return Result<{Entity}Dto?>.Failure("Not found");
        return Result<{Entity}Dto?>.Success(MapToDto(entity));
    }
    private static {Entity}Dto MapToDto({Entity} e) => new(e.Id, /* ... */);
}
```

## Dapper Repository Template
```csharp
// VaryoCms.Infrastructure/Persistence/Repositories/{Entity}Repository.cs
public class {Entity}Repository(IDbConnectionFactory connectionFactory) : I{Entity}Repository
{
    public async Task<{Entity}?> GetByIdAsync(int id, int tenantId, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<{Entity}>(
            @"SELECT * FROM {table_name}
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = tenantId });
    }
    
    public async Task<int> CreateAsync({Entity} entity, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO {table_name} (tenant_id, /* cols */, created_at, updated_at, is_deleted)
              OUTPUT INSERTED.id
              VALUES (@TenantId, /* vals */, GETUTCDATE(), GETUTCDATE(), 0)",
            entity);
    }
    
    public async Task UpdateAsync({Entity} entity, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE {table_name}
              SET /* col = @val */, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            entity);
    }
    
    public async Task DeleteAsync(int id, int tenantId, CancellationToken ct = default)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE {table_name} SET is_deleted = 1, updated_at = GETUTCDATE() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }
}
```

## Controller Template
```csharp
// VaryoCms.Web/Controllers/Admin/{Entity}Controller.cs
[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/{entity-slug}")]
public class {Entity}Controller(I{Entity}Service service) : Controller
{
    [HttpGet] public async Task<IActionResult> Index() { ... }
    [HttpGet("create")] public IActionResult Create() => View(new Create{Entity}ViewModel());
    [HttpPost("create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Create{Entity}ViewModel vm) { ... }
    [HttpGet("{id}/edit")] public async Task<IActionResult> Edit(int id) { ... }
    [HttpPost("{id}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Edit{Entity}ViewModel vm) { ... }
    [HttpPost("{id}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) { ... }
}
```

## DI Registration
```csharp
// In Infrastructure/DependencyInjection.cs
services.AddScoped<I{Entity}Repository, {Entity}Repository>();

// In Application/DependencyInjection.cs
services.AddScoped<I{Entity}Service, {Entity}Service>();
```
