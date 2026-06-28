---
name: dynamiccms-ui
description: >
  Use when building, improving, or redesigning any UI component, page, or layout
  in Varyo CMS. Triggers on: "tasarım", "UI", "görünüm", "arayüz", "design",
  "layout", "sayfa düzeni", "güzel yap", "iyileştir", "view oluştur",
  "partial yaz", "form tasarla", "tablo", "dashboard", "modal", "sidebar",
  "kötü görünüyor", "daha iyi yap", "frontend". Extends the public frontend-design skill
  with Varyo CMS-specific constraints and component library.
---

# Varyo CMS UI & Frontend Skill

> Bu skill önce genel tasarım felsefesi için `frontend-design` skill'ini referans alır,
> sonra Varyo CMS'e özgü kısıtları ve component kurallarını uygular.
> @/mnt/skills/public/frontend-design/SKILL.md

---

## Stack Kısıtları (DEĞIŞTIRME)

| Şey | Kullanılan | Yasak |
|---|---|---|
| CSS Framework | **Bootstrap 5.3** | Tailwind, custom CSS framework |
| Icons | **Bootstrap Icons (bi-*)** | Font Awesome, Heroicons |
| JS | **Vanilla JS (ES6+)** | React, Vue, Angular |
| Charts | **Chart.js** | D3, Highcharts |
| Rich Text | **TinyMCE** | Quill, CKEditor |
| Date Picker | **Flatpickr** | Datepicker.js, Pikaday |
| Drag-Drop | **SortableJS** | jQuery UI Sortable |
| Maps | **Leaflet.js** | Google Maps, Mapbox |
| Code Editor | **Monaco Editor** | CodeMirror |
| Image Crop | **Cropper.js** | Croppie, imgAreaSelect |
| Toasts | **Bootstrap Toast** | Toastr, SweetAlert |
| Modals | **Bootstrap Modal** | custom modal |

---

## Admin Panel Kimliği

Varyo CMS admin paneli "araç kutusu" estetiğini benimser:
- **Profesyonel ama soğuk değil** — içerik üreticileri için tasarlandı
- **Fonksiyonel önce, dekoratif asla** — her element bir iş yapar
- **Yoğun bilgi, düzenli hiyerarşi** — CMS'ler veri yoğundur, bundan kaçma
- **Tutarlı ritim** — spacing ve renk sistemine sıkı uyu

### Renk Paleti
```css
/* CSS Variables — _variables.css veya site.css başına ekle */
:root {
  --cms-primary:     #4F46E5;   /* indigo — ana aksiyon rengi */
  --cms-primary-hover: #4338CA;
  --cms-secondary:   #6B7280;   /* cool gray */
  --cms-success:     #10B981;   /* emerald */
  --cms-danger:      #EF4444;
  --cms-warning:     #F59E0B;
  --cms-info:        #3B82F6;

  --cms-bg:          #F9FAFB;   /* sayfa arka planı */
  --cms-surface:     #FFFFFF;   /* card, panel yüzeyleri */
  --cms-border:      #E5E7EB;   /* ince border */
  --cms-border-dark: #D1D5DB;

  --cms-text:        #111827;   /* ana metin */
  --cms-text-muted:  #6B7280;   /* ikincil metin */
  --cms-text-light:  #9CA3AF;   /* placeholder, hint */

  --cms-sidebar-bg:  #1E1B4B;   /* koyu indigo sidebar */
  --cms-sidebar-text:#C7D2FE;
  --cms-sidebar-active: #4F46E5;
  --cms-sidebar-hover: #312E81;

  --cms-radius:      6px;
  --cms-radius-lg:   10px;
  --cms-shadow:      0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04);
  --cms-shadow-lg:   0 4px 6px rgba(0,0,0,0.07), 0 2px 4px rgba(0,0,0,0.05);
}
```

### Tipografi
```css
/* Google Fonts — _Layout.cshtml <head>'ine ekle */
/* Inter: UI metni için — nötr, okunabilir */
/* JetBrains Mono: kod, slug, JSON alanları için */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap');

body { font-family: 'Inter', system-ui, sans-serif; }
code, .font-mono, input[type="text"].slug-field { font-family: 'JetBrains Mono', monospace; }

/* Type scale */
.cms-page-title   { font-size: 1.5rem;   font-weight: 700; color: var(--cms-text); }
.cms-section-title{ font-size: 1.125rem; font-weight: 600; color: var(--cms-text); }
.cms-label        { font-size: 0.875rem; font-weight: 500; color: var(--cms-text); }
.cms-hint         { font-size: 0.75rem;  font-weight: 400; color: var(--cms-text-light); }
```

---

## Layout Yapısı

```
┌─────────────────────────────────────────────────────┐
│  TOPBAR (64px) — logo | breadcrumb | lang | user   │
├──────────────┬──────────────────────────────────────┤
│              │  PAGE HEADER (sayfa başlığı + CTA)   │
│  SIDEBAR     ├──────────────────────────────────────┤
│  (260px)     │                                      │
│              │  CONTENT AREA                        │
│  Koyu indigo │  (beyaz card'lar içinde)             │
│  arka plan   │                                      │
│              │                                      │
└──────────────┴──────────────────────────────────────┘
```

### _Layout.cshtml Yapısı
```html
<body class="cms-body">
  <!-- Sidebar -->
  <aside class="cms-sidebar" id="sidebar">
    <div class="cms-sidebar-brand">...</div>
    <nav class="cms-sidebar-nav">
      <!-- Dinamik menü buraya -->
    </nav>
  </aside>

  <!-- Main wrapper -->
  <div class="cms-main" id="main-content">
    <!-- Topbar -->
    <header class="cms-topbar">...</header>

    <!-- Page content -->
    <main class="cms-content">
      <!-- Sayfa başlığı + CTA -->
      <div class="cms-page-header">
        <div>
          <h1 class="cms-page-title">@ViewData["Title"]</h1>
          <nav aria-label="breadcrumb">...</nav>
        </div>
        <div class="cms-page-actions">
          @RenderSection("PageActions", required: false)
        </div>
      </div>

      <!-- Asıl içerik -->
      @RenderBody()
    </main>
  </div>
</body>
```

---

## Reusable Component Patterns

### Card (panel/bölüm sarmalayıcı)
```html
<div class="card cms-card">
  <div class="card-header cms-card-header">
    <h5 class="card-title mb-0">Başlık</h5>
    <div class="cms-card-actions">
      <!-- Sağ üst aksiyonlar -->
    </div>
  </div>
  <div class="card-body">
    ...
  </div>
</div>
```

### Data Table
```html
<div class="cms-table-wrapper">
  <div class="cms-table-toolbar">
    <input type="search" class="form-control cms-search" placeholder="Ara...">
    <div class="cms-table-actions">
      <a href="/create" class="btn cms-btn-primary">
        <i class="bi bi-plus-lg me-1"></i> Yeni Ekle
      </a>
    </div>
  </div>
  <div class="table-responsive">
    <table class="table cms-table">
      <thead>
        <tr>
          <th>Ad</th>
          <th class="text-center" style="width:120px">Durum</th>
          <th style="width:100px"></th>  <!-- Actions -->
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>
            <span class="cms-table-title">İçerik Adı</span>
            <span class="cms-table-meta">slug-degeri</span>
          </td>
          <td class="text-center">
            <span class="badge cms-badge-success">Yayında</span>
          </td>
          <td>
            <div class="cms-row-actions">
              <a href="#" class="cms-action-btn" title="Düzenle">
                <i class="bi bi-pencil"></i>
              </a>
              <button class="cms-action-btn cms-action-danger" title="Sil">
                <i class="bi bi-trash"></i>
              </button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</div>
```

### Form Layout
```html
<!-- Tek sütun (dar form) -->
<div class="row justify-content-center">
  <div class="col-lg-8">
    <div class="card cms-card">
      <div class="card-body">
        <form method="post">
          @Html.AntiForgeryToken()
          <div class="cms-form-group">
            <label class="cms-label">Alan Adı <span class="text-danger">*</span></label>
            <input type="text" class="form-control" name="...">
            <div class="cms-hint">Yardım metni buraya</div>
          </div>
          <div class="cms-form-footer">
            <a href="@Url.Action("Index")" class="btn btn-outline-secondary">İptal</a>
            <button type="submit" class="btn cms-btn-primary">Kaydet</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</div>

<!-- İki sütun (geniş form — field builder gibi) -->
<div class="row g-4">
  <div class="col-lg-8"><!-- Ana alan --></div>
  <div class="col-lg-4"><!-- Sidebar: ayarlar, meta --></div>
</div>
```

### Alert / Toast
```html
<!-- Inline alert -->
<div class="alert cms-alert-success" role="alert">
  <i class="bi bi-check-circle-fill me-2"></i>
  İşlem başarıyla tamamlandı.
</div>

<!-- JS Toast (TempData'dan) -->
<div class="toast-container position-fixed bottom-0 end-0 p-3">
  <div id="cmsToast" class="toast" role="alert">
    <div class="toast-body">
      <i class="bi bi-check-circle text-success me-2"></i>
      <span id="toastMessage"></span>
    </div>
  </div>
</div>
```

### Status Badge
```html
<span class="badge cms-badge-success">Yayında</span>
<span class="badge cms-badge-warning">Taslak</span>
<span class="badge cms-badge-secondary">Arşiv</span>
<span class="badge cms-badge-danger">Pasif</span>
```

### Empty State (liste boşsa)
```html
<div class="cms-empty-state">
  <i class="bi bi-inbox cms-empty-icon"></i>
  <h5>Henüz içerik yok</h5>
  <p class="text-muted">İlk kaydı oluşturmak için aşağıdaki butona tıklayın.</p>
  <a href="@Url.Action("Create")" class="btn cms-btn-primary">
    <i class="bi bi-plus-lg me-1"></i> Yeni Ekle
  </a>
</div>
```

---

## Sidebar Dinamik Menü Partial

```html
<!-- _SidebarNav.cshtml -->
<nav class="cms-sidebar-nav">
  <!-- Sabit üst menü -->
  <div class="cms-nav-section">
    <span class="cms-nav-label">İçerikler</span>
    @foreach (var ct in Model.ContentTypes) {
      <a href="@Url.Action("Index", "Content", new { slug = ct.Slug })"
         class="cms-nav-item @(currentSlug == ct.Slug ? "active" : "")">
        <i class="bi @ct.Icon me-2"></i>
        <span>@ct.Name</span>
      </a>
    }
  </div>

  <!-- Admin bölümü — sadece TenantAdmin+ -->
  @if (User.IsInRole("TenantAdmin") || User.IsInRole("SystemAdmin")) {
    <div class="cms-nav-section cms-nav-divider">
      <span class="cms-nav-label">Genel Ayarlar</span>
      <a href="/admin/content-types" class="cms-nav-item @(area == "content-types" ? "active" : "")">
        <i class="bi bi-grid me-2"></i><span>İçerik Yapıları</span>
      </a>
      <a href="/admin/dictionary" class="cms-nav-item @(area == "dictionary" ? "active" : "")">
        <i class="bi bi-translate me-2"></i><span>Sözlük</span>
      </a>
      <a href="/admin/users" class="cms-nav-item @(area == "users" ? "active" : "")">
        <i class="bi bi-people me-2"></i><span>Kullanıcılar</span>
      </a>
      <a href="/admin/api-management" class="cms-nav-item @(area == "api" ? "active" : "")">
        <i class="bi bi-code-slash me-2"></i><span>API Yönetimi</span>
      </a>
    </div>
  }
</nav>
```

---

## Field Builder UI Kuralları

- Her field kartı: `drag-handle | type-icon | field-name | slug (mono font) | required-badge | localized-badge | ⋮ menu`
- Sürükleme sırasında: kart `opacity: 0.5` + `border: 2px dashed var(--cms-primary)`
- Yeni field ekleme: sağdan kayan panel (offcanvas), önce type seçimi, sonra o tipe özel form
- Field type seçim grid'i: 5 sütun, icon + label, hover'da indigo border

## Media Uploader UI Kuralları

- Yükleme öncesi: dashed border drop zone, icon + "Sürükle veya tıkla" metni
- Yükleme sırası: progress bar, dosya adı, iptal butonu
- Yükleme sonrası: thumbnail + dosya adı + boyut + "Değiştir" / "Kaldır" butonları
- Image crop: yükleme sonrası modal içinde Cropper.js, aspect ratio varsa kilitli

---

## Yapma Listesi

- `style="..."` inline CSS yazma — class veya CSS variable kullan
- Bootstrap'in `primary` rengini override etme — `cms-btn-primary` class'ı yaz
- `jQuery` ekleme — vanilla JS yeterli
- Her yere animasyon koyma — sadece geçiş ve hover'da minimal
- Farklı sayfalarda farklı spacing mantığı — `cms-card`, `cms-form-group` class'larına uyu
- Koyu arka plan üzerine koyu metin — kontrast her zaman kontrol et (WCAG AA)

---

## Uygulama Notları (mevcut implementasyon — bunlara UY)

> Bu tasarım sistemi projeye uygulandı. Yeni view/partial yazarken aşağıdaki yerleşik
> konvansiyonları kullan; tekrar kurma.

### Nerede ne var
- **Tüm `cms-*` class'ları** `wwwroot/css/site.css` içinde tanımlı (renk paleti CSS variable olarak `:root`'ta). Yeni bir desen lazımsa önce site.css'e class ekle, view'da inline yazma.
- **Bootstrap Icons** (`bi-*`) ve **Google Fonts** (Inter + JetBrains Mono) `_Layout.cshtml` `<head>`'inde CDN'den yükleniyor. Ayrıca kurmaya gerek yok.
- **jQuery sadece** `_ValidationScriptsPartial.cshtml` içinde (unobtrusive validation bağımlılığı). Global değil. UI davranışı = `wwwroot/js/site.js` içinde vanilla JS.

### Layout kabuğu (`_Layout.cshtml`)
- **Authed** kullanıcı → tam kabuk: `.cms-sidebar` (koyu indigo) + `.cms-topbar` (kullanıcı/rol + change-password + logout) + `.cms-content` içinde **otomatik page-header**.
- **Anon** kullanıcı → çıplak `<body>`; ortalı sayfalar için `.cms-auth-shell` > `.cms-auth-card` kullan (Login, anon Home böyle).
- Sidebar dinamik içerik menüsü `NavMenu` ViewComponent'inden gelir; aktif item path'e göre `site.js` ile işaretlenir.

### Sayfa başlığı ve aksiyonlar (ZORUNLU desen)
- Sayfa başlığını **layout** `@ViewData["Title"]`'dan render eder. View içine **kendi `<h1>`'ini yazma**.
- Sağ üst CTA butonları için: `@section PageActions { ... }`.
- Geri/kırıntı navigasyonu için: `@section Breadcrumb { <a>…</a> <i class="bi bi-chevron-right"></i> … }`.
- `@section` bloğunu `@if` içine koyma (Razor patlar); koşulu section'ın **içine** al.

### Component kullanımı (yerleşik)
- Liste sayfası: `.cms-table-wrapper` > (`.cms-table-toolbar` ops.) > `.cms-table`; satır aksiyonları `.cms-row-actions` + `.cms-action-btn` (+`.cms-action-danger`). Boşsa `.cms-empty-state`.
- Form: `.card.cms-card` > `.card-body` > `.cms-form-group` (label `.cms-label`, yardım `.cms-hint`) ; footer `.cms-form-footer` (İptal solda outline, Kaydet sağda `.cms-btn-primary`).
- Slug/JSON/icon input'larına `.slug-field` ya da `.cms-mono` ver (monospace).
- Durum rozetleri: `.cms-badge-success/warning/secondary/danger/info`; field tip etiketi `.cms-type-badge`.
- Inline uyarılar: `.cms-alert-success/danger/warning/info` (flex + ikonlu). "Bir kez gösterilen" kod blokları için `.cms-alert-* d-block`.
- Medya kütüphanesi: `.cms-media-grid` + `.cms-media-card`. Field builder: `<ul id="field-list" class="cms-field-list">` + `<li class="cms-field-card" data-id>` + `.drag-handle.cms-drag-handle` (SortableJS bu selektörlere bağlı — koru).
- Picker dropdown'ları (`.mp-results`/`.rp-results`) JS ile `display` toggle eder; başlangıç `style="display:none"` **fonksiyonel state**, dokunma. Relation/media chip class'ları (`mp-*`, `rp-*`, `mp-value`, `rp-value` …) JS'e bağlı — koru.

### İnce ayar
- Tablo kolon / dar input genişlikleri için inline `style="width:…"` kabul (skill'in tablo örneğiyle tutarlı). Bunun dışında inline CSS yazma.
- Toggle switch'ler: `.form-check.form-switch` (checkbox'ları switch yap).
