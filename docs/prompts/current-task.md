# Current Task

## Status: ✅ v1.1.0 + Mert Özen seed v3 + güvenlik fixleri hazır

**Tarih:** 2026-07-01  
**Branch:** main (`v1.1.0` tag'li)

---

## 🔄 Aktif: MertOzenWeb — Frontend MVC Projesi

**Durum:** CMS seed v3 tamamlandı (13 content type), MVC frontend projesi henüz oluşturulmadı.

**Brief dosyası:** `~/Desktop/MertOzenWebsite-ProjectBrief.md`  
**Tenant slug:** `mert-ozen` (DevTenantSlug güncellendi)  
**CMS API:** `http://localhost:5267/api/v1/mert-ozen/{slug}`  
**API Key:** `vk_1_M3rtOzen_SecretKey2026!` (X-API-Key header; auth_type=ApiKey, is_public=0)

**Seed içeriği (003_mert_ozen_seed.sql — v3):**
- 13 content type: `site-settings`, `category`, `series`, `post`, `video`, `about`, `video-list`, `experience`, `education`, `skill-group`, `book`, `movie`, `activity`
- Sosyal linkler `site-settings` singleton'da (linkedin, github, youtube, instagram, email)
- `series` content type (Microservices 101, PostgreSQL Derinlemesine)
- 8 kategori: Backend, Frontend, DevOps, Veritabanı, Yapay Zeka, Mobil, Güvenlik, Sistem
- 14 blog yazısı (4 öne çıkan, series Relation ile bağlı P1/P5/P6/P8/P9)
- 8 video (`youtubeId` alanı, viewCount YOK; 4 video-list'e bağlı Relation)
- 4 video listesi: Backend & Mimari, DevOps & Altyapı, Veri & Yapay Zeka, Frontend & İstemci
- 3 deneyim, 2 eğitim, 5 yetkinlik grubu, 7 kitap, 5 film & dizi, 4 aktivite
- 1 hakkımda kaydı (6 MultiRelation: experiences, educations, skillGroups, books, moviesSeries, activities)
- Tüm API yapılandırmaları: auth_type=ApiKey, is_public=0, allow_read=1
- Tüm API yapılandırmaları açık (is_public=1, allow_read=1, auth_type=None)

**Sosyal linkler:** `site-settings` API'sinden çekilir (site-settings→linkedinUrl, githubUrl, youtubeUrl, instagramUrl, contactEmail)

**Sıradaki adım:** `~/Desktop/MertOzenWebsite-ProjectBrief.md` brief dosyasını güncellenmiş haliyle Claude'a ver → sıfırdan ASP.NET Core 8 MVC projesi oluştur.

---

## v1.1.0 Kapsamı (Tamamlandı)

- **Modal medya seçici** — "Dosya Seç" butonu → Bootstrap modal (Kütüphane + Bilgisayardan Yükle sekmeleri)
- **Form içi upload** — Editor ve adminler content formundan ayrılmadan medya yükleyebilir
- **Editor upload yetkisi** — Upload endpoint'e Editor rolü eklendi
- **Alan-bazlı limit (istemci + sunucu)** — `UploadAsync` `maxSizeMb` + `allowedFormats` parametreleri
- 9 yeni UI çeviri anahtarı (tr + en)

---

## v1.0.0 Kapsamı (Tamamlandı)

Varyo CMS v1.0.0 tüm çekirdek özellikleri içeriyor:

- **Multi-tenant mimari** — subdomain çözümleme, ITenantContext, tam izolasyon
- **EAV içerik modeli** — 30+ field tipi, drag-drop field builder, çoklu dil
- **Medya yönetimi** — yükleme, kırpma (Cropper.js + ImageSharp), arama
- **Sözlük (Dictionary)** — per-tenant i18n anahtar-değer deposu
- **Kullanıcı yönetimi** — BCrypt, 4 rol, per-content-type CRUD izin matrisi
- **Public REST API** — ApiKey/JWT auth, camelCase alan anahtarları, rate limit, caching, write API
- **API Explorer** — `/api/docs/{tenantSlug}` credential-gated dökümantasyon
- **Auth** — cookie login, 2FA e-posta (isteğe bağlı), remember-me, şifre değiştir
- **SystemAdmin Konsolu** — /system cross-tenant yönetim, impersonation, UI çeviri yönetimi
- **Audit log** — Serilog diagnostic + Dapper audit trail, /admin/logs UI
- **Docker** — multi-stage Dockerfile, docker-compose.yml (DB + web), named volumes

---

## Invariant'lar (ASLA İHLAL ETME)

1. **Tenant izolasyonu:** Her repository sorgusu `tenant_id = @TenantId AND is_deleted = 0` içermeli
2. **Audit logging:** Her write işlemi sonunda `await _audit.LogAsync(AuditActions.Xyz, ...)` çağrılmalı
3. **camelCase API:** Public API `fields` anahtarları her zaman camelCase (`Slugifier.ToCamelCase(alias ?? slug)`)
4. **Katman sınırları:** Web → Application → Infrastructure → Domain; Web Infrastructure'ı doğrudan çağırmaz
5. **SQL güvenlik:** Parameterized query zorunlu; string interpolation yasak
6. **ImageSharp:** 2.1.10'da kal (3.x/4.x ücretli lisans)
7. **BCrypt:** workfactor 12; hash asla SQL içinde üretme
8. **EmailVerification:Enabled=false** varsayılanını bozma

---

## Sonraki Adımlar (Öneri)

- [ ] CI/CD pipeline (GitHub Actions: build + test + Docker push)
- [ ] Webhook desteği (içerik publish/delete olayları)
- [ ] Çoklu domain desteği (şu an subdomain only)

---

## Yeni Görev Başlatırken

```
Read @docs/prompts/current-task.md and continue from where we left off.
```

Yeni bir özellik eklerken:
1. Bu dosyaya **aktif görev** olarak ekle (durum: 🔄 devam ediyor)
2. Gerekiyorsa migration dosyası ekle: `00N_<description>.sql`
3. Servise `await _audit.LogAsync(...)` ekle
4. `AuditActions.cs`'e yeni sabit ekle
5. Görev tamamlanınca bu dosyaya ✅ olarak işaretle

---

## DB Bağlantı Bilgileri (dev)

```
Host: localhost,1433  |  User: sa  |  Pass: Varyo_Dev2024!  |  DB: Varyo
```

```bash
# Migration uygula
docker exec varyo_db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "Varyo_Dev2024!" -C -d Varyo \
  -i /migrations/NNN_x.sql -b
```

```bash
# Uygulamayı yeniden başlat
pkill -f "dotnet.*VaryoCms.Web" 2>/dev/null; sleep 1
dotnet run --project src/VaryoCms.Web --no-build &
```

> **Ne zaman yeniden başlatmak gerekir?**
> - Middleware, DI kaydı veya `appsettings.json` değişikliği
> - Razor view değişikliği (JS/CSS cache buster için)
> - Migration uygulandıktan sonra (DB-backed localizer cache)
> - UI dil çevirisi seed'lendikten sonra (IUiTranslationStore cache)
