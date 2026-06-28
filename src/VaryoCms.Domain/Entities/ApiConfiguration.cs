using VaryoCms.Domain.Enums;

namespace VaryoCms.Domain.Entities;

// API exposure config per (tenant, content type). No IsDeleted (per schema).
// auth_type / api_key columns are dormant (kept for migration safety); auth is now via api_credentials.
// is_public replaces auth_type='None': when true, no credential is required.
public class ApiConfiguration
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ContentTypeId { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }              // true = no auth; false = valid credential required
    public ApiAuthType AuthType { get; set; }       // dormant (kept for schema compat; do not use for auth)
    public string? ApiKey { get; set; }             // dormant
    public bool AllowFiltering { get; set; }
    public bool AllowSorting { get; set; }
    public bool AllowPagination { get; set; }
    public int? RateLimitPerMin { get; set; }
    public int? CacheSeconds { get; set; }
    // Per-verb CRUD permissions (migration 034). allow_read defaults true; others are opt-in.
    public bool AllowRead { get; set; } = true;
    public bool AllowCreate { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
