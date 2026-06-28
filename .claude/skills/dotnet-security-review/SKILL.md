---
name: dotnet-security-review
description: >
  Use when reviewing code for security vulnerabilities, fixing a reported security issue,
  or implementing security-sensitive features. Triggers on: "güvenlik", "security",
  "açık", "vulnerability", "SQL injection", "XSS", "CSRF", "auth", "token",
  "password", "api key", "rate limit", "permission", "yetkisiz erişim".
---

# Varyo CMS Security Review Checklist

## Bu Skill Ne Zaman Devreye Girer
- Semgrep / security-scan çıktısını fix ederken
- Auth, permission, API key ile ilgili kod yazarken
- Yeni bir Controller veya public endpoint eklerken
- Media upload, file işleme kodunda
- Kullanıcı girdisinin işlendiği her yerde

---

## 1. SQL Injection
```csharp
// GÜVENLI — her zaman böyle yaz
await conn.QueryAsync<T>("SELECT * FROM t WHERE id = @Id", new { Id = id });

// AÇIK — asla yapma
await conn.QueryAsync<T>($"SELECT * FROM t WHERE id = {id}");
await conn.QueryAsync<T>("SELECT * FROM t WHERE name = '" + name + "'");
```
**Semgrep kuralı:** `p/csharp` paketi bunu yakalar.
**Fix:** Tüm Dapper sorgularında `@Param` kullan, string concatenation yasak.

---

## 2. Tenant Isolation (Kritik — bu projeye özel)
```csharp
// GÜVENLI — her sorguda tenant_id zorunlu
WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0

// AÇIK — tenant_id filtresi eksik, başka tenant'ın verisine erişilir
WHERE id = @Id AND is_deleted = 0
```
**Review:** Her repository metodunu tara, `tenant_id` eksik olan var mı?
**Semgrep custom kural:** `content_types` / `content_items` sorgularında `tenant_id` yoksa flag at.

---

## 3. CSRF Koruması
```csharp
// Her POST action'da zorunlu
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateViewModel vm) { }

// View'da form içinde zorunlu
@Html.AntiForgeryToken()
// veya Tag Helper:
<form method="post">  // otomatik ekler asp-antiforgery="true" ile
```
**Review:** Tüm POST/PUT/DELETE action'larında `[ValidateAntiForgeryToken]` var mı?

---

## 4. XSS Koruması
```csharp
// Razor otomatik encode eder — güvenli
<p>@Model.UserInput</p>

// AÇIK — Html.Raw sadece güvenilir içerik için
@Html.Raw(Model.RichTextContent)  // ← TinyMCE çıktısı için kaçınılmaz
                                   // ama sanitize edilmeli
```
**Fix:** RichText alanları için `HtmlSanitizer` paketi kullan:
```csharp
dotnet add package HtmlSanitizer
var sanitizer = new HtmlSanitizer();
var clean = sanitizer.Sanitize(richTextInput);
```

---

## 5. Password Güvenliği
```csharp
// GÜVENLI — BCrypt ile hash
services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

// Implemente et
public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

// AÇIK — asla
var hash = MD5(password);   // kırılabilir
var hash = SHA1(password);  // kırılabilir
// düz metin saklamak
```

---

## 6. API Key Güvenliği
```csharp
// Üretirken: cryptographically secure random
var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

// DB'ye kayıt: hash'le, düz metin saklama
var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKey, workFactor: 10);
// DB'de: api_key_hash kolonuna keyHash sakla

// Doğrularken:
var isValid = BCrypt.Net.BCrypt.Verify(requestKey, storedHash);

// Kullanıcıya: sadece üretildiği anda göster, bir daha gösterme
```

---

## 7. Media Upload Güvenliği
```csharp
// GÜVENLI file upload
private static readonly HashSet<string> AllowedMimeTypes = new()
{
    "image/jpeg", "image/png", "image/webp", "image/gif"
};

// MIME type'ı extension'dan değil, dosya içeriğinden doğrula
var mimeType = MimeDetective.Inspect(stream);
if (!AllowedMimeTypes.Contains(mimeType))
    return Result.Failure("Geçersiz dosya tipi");

// Dosya adını asla kullanıcıdan alma — kendisi üret
var fileName = $"{Guid.NewGuid()}{GetSafeExtension(mimeType)}";

// Upload path dışına çıkmayı engelle (path traversal)
var safePath = Path.GetFullPath(Path.Combine(uploadDir, fileName));
if (!safePath.StartsWith(uploadDir))
    throw new SecurityException("Path traversal attempt");
```

---

## 8. Rate Limiting (API + Login)
```csharp
// Login endpoint için brute force koruması
// Program.cs'e ekle:
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Controller'da:
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<IActionResult> Login(LoginViewModel vm) { }
```

---

## 9. Sensitive Data Exposure
```csharp
// AÇIK — hata mesajında stack trace / iç detay verme
return BadRequest(ex.Message);  // exception mesajı dışarı sızabilir

// GÜVENLI
_logger.LogError(ex, "Content item create failed");
return BadRequest("İşlem gerçekleştirilemedi.");  // generic mesaj

// appsettings.Production.json'da:
// "DetailedErrors": false   ← kesinlikle false olmalı
```

---

## 10. Security Headers
```csharp
// Program.cs'e ekle
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});
```

---

## Tarama Komutları (her PR öncesi çalıştır)

```bash
# 1. NuGet vulnerable paket kontrolü
dotnet list package --vulnerable

# 2. Semgrep OWASP taraması
semgrep --config "p/owasp-top-ten" src/ --output semgrep-report.json

# 3. Secret sızıntısı kontrolü
trufflehog filesystem ./ --only-verified

# 4. .NET güvenlik analizörü (build sırasında otomatik)
dotnet build /p:TreatWarningsAsErrors=false /p:AnalysisLevel=latest
```

---

## Öncelik Sırası (bu proje için)
1. **Tenant isolation** — en kritik, veri sızıntısı riski
2. **SQL injection** — Dapper parameterized sorgu zorunluluğu
3. **Media upload** — path traversal + MIME type doğrulama
4. **API key** — hash'leme + rotation
5. **CSRF** — tüm POST action'larda token
6. **XSS** — RichText alanları sanitize
7. **Rate limiting** — login + public API
8. **Security headers** — production'a geçmeden
