# Varyo CMS — Claude Code Cheat Sheet

## Her Yeni Session Başında (ZORUNLU)
```
Read @CLAUDE.md and @docs/prompts/current-task.md.
Summarize the current state and tell me the next step. Don't write code yet.
```

---

## Proje Kurulumu
```
Create the Varyo CMS solution structure:
- dotnet new sln -n VaryoCms
- 4 class library projects as defined in @docs/architecture.md
- Add project references per layer rules
- Create docker/docker-compose.yml per the template
- Create appsettings.json + appsettings.Development.json with DevTenantSlug
```

---

## Yeni Migration
```
Write migration [NNN]_[description].sql following @docs/database-schema.md.
Tables needed: [list tables].
Use the migration template from the dotnet-migration skill.
```

---

## Yeni Feature (Tam Stack)
```
Implement [FeatureName] across all layers.
Use the dotnet-layered-arch skill template.
Domain entity: [describe fields]
DB table: [table name from @docs/database-schema.md]
Controller route: /admin/[route]
Access: [TenantAdmin / Editor / All]
```

---

## İçerik Tipi İşlemleri
```
[ContentType field builder / form renderer / drag-drop reorder] için:
Use the dotnet-content-type skill.
ContentType ID/Slug: [value]
```

---

## API Endpoint Açma
```
Expose [content-type-slug] as a public API endpoint.
Use the dotnet-api-endpoint skill.
Auth type: [None / ApiKey / JWT]
Rate limit: [N] req/min
Hidden fields: [list or "none"]
```

---

## Bug Fix
```
Bug: [symptom]
File: [path or "unknown"]
Expected: [behavior]
Actual: [behavior]
Check tenant_id filters, Result<T> handling, and layer violations first.
```

---

## Session Sonu (Checkpoint)
```
Update @docs/prompts/current-task.md:
- Mark completed: [task]
- Next step: [task]
- Any blockers: [or "none"]
```

---

## Özel Komutlar

| Ne yapmak istiyorsun | Prompt |
|---|---|
| Field yeniden sırala | "Implement drag-drop field reordering for content type [X]. Use dotnet-content-type skill." |
| Media upload + crop | "Implement image upload with Cropper.js for field [X]. Use SixLabors.ImageSharp for server-side crop." |
| Relation field | "Add Relation field type between [ContentType A] and [ContentType B]. Display field: [slug]. Use dotnet-content-type skill." |
| Sözlük yönetimi | "Implement Dictionary (Sözlük) CRUD for TenantAdmin. Keys editable per language. Use dotnet-multitenant skill." |
| Kullanıcı izinleri | "Implement permission matrix UI: users × content types with can_read/create/update/delete per cell." |
| API key rotasyonu | "Implement API key generation and rotation for api_configurations. BCrypt hash the key, show it only once." |
| Dil switcher | "Add language switcher to the admin layout. Persist selection in cookie. Use dotnet-multitenant skill." |
