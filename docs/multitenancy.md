# Varyo CMS — Multi-Tenancy Reference

## Tenant Resolution Flow

```
1. Request: GET https://acme.cms.local/admin/content-types
2. TenantResolutionMiddleware extracts "acme" from host
3. Queries: SELECT id, name, slug FROM tenants WHERE slug = 'acme' AND is_active = 1 AND is_deleted = 0
4. If not found → 404 / redirect to error page
5. Sets: HttpContext.Items["TenantId"] = tenant.Id
6. Scoped ITenantContext.TenantId is set
7. All subsequent service/repo calls use TenantContext.TenantId
```

## ITenantContext Interface

```csharp
public interface ITenantContext
{
    int TenantId { get; }
    string TenantSlug { get; }
    string DefaultLanguageCode { get; }
}
```

Registered as `Scoped` — new instance per request.

## Local Development (No Subdomain)

In `appsettings.Development.json`:
```json
{
  "DevTenantSlug": "dev-tenant"
}
```

`TenantResolutionMiddleware` checks if host is `localhost` or `127.0.0.1`:
- If yes → uses `DevTenantSlug` from config
- Ensures dev workflow is frictionless

## Tenant Isolation Rules

| Rule | Detail |
|---|---|
| Every repo query MUST include `tenant_id = @TenantId` | No exceptions |
| `ITenantContext` injected via constructor DI | Never read from HttpContext directly in repos |
| SystemAdmin bypasses tenant isolation | Check `UserRole.SystemAdmin` explicitly |
| Tenant creation is SystemAdmin-only | `POST /system/tenants` |
| Tenant slug is immutable after creation | Changing it breaks subdomains |

## User Roles & Access

| Role | Access |
|---|---|
| `SystemAdmin` | Everything across all tenants |
| `TenantAdmin` | Everything within own tenant: Settings, Dictionary, Users, API |
| `Editor` | ContentTypes they have permission for (read/create/update per `user_content_type_permissions`) |
| `Viewer` | Read-only access to permitted content types |

## SystemAdmin Platform Console (implemented)

SystemAdmins live in their own root table (`system_admins`, no `tenant_id`) and sign in separately at
**`/system/login`** (cross-tenant; `ISystemAuthService`). The console lives under **`/system`**
(`[Authorize(Roles="SystemAdmin")]`):

- **Dashboard** (`/system`) — tenant list with per-tenant user/content-type counts.
- **Tenant CRUD** (`/system/tenants`) — create (provisions tenant + default language + first TenantAdmin in
  one transaction via `ITenantProvisioningRepository`), edit (name/active; **slug immutable**), soft-delete.
- **Impersonation** (`/system/impersonate/{tenantId}` / `/exit`) — re-issues the auth cookie with an
  `ImpersonatedTenantId` claim. `ImpersonationMiddleware` (after `UseAuthentication`) overrides the request's
  `ITenantContext`/`ILanguageContext` to that tenant, so the entire tenant-scoped admin panel operates on the
  impersonated tenant unchanged. A warning banner + "Exit impersonation" is shown while active.

Cross-tenant repositories (`ISystemAdminRepository`, `ITenantProvisioningRepository`, `ITenantStore`) do
**not** depend on `ITenantContext` (registered Singleton). Everything else keeps the `tenant_id = @TenantId`
invariant; impersonation works precisely because repos read `TenantId` at query time.

## Menu Generation (Left Sidebar)

The left menu is dynamically built from `content_types` table:

```sql
SELECT id, name, slug, icon, sort_order
FROM content_types
WHERE tenant_id = @TenantId
  AND is_published = 1
  AND is_deleted = 0
ORDER BY sort_order ASC
```

Then filtered by `user_content_type_permissions` for the current user.

Admin-only items (`General Settings`, `Dictionary`) are shown only to TenantAdmin+.
