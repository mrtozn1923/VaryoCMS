-- ================================================================
-- 002_v1_system_seed.sql
-- SYSTEM BOOTSTRAP -- no tenant, no content structure.
-- Safe to run in any environment (dev, staging, or a fresh production
-- clone). Change the seeded SystemAdmin password after first login.
--
-- Run AFTER 001_v1_schema.sql. This is the file to use when you clone
-- Varyo CMS and want to start clean: log in at /system/login, create
-- your own tenant(s), and build your own content types from scratch.
--
-- Contains ONLY:
--   - A SystemAdmin account (root@system.local / Admin123!) to sign
--     into the cross-tenant /system console and provision tenants.
--   - Global admin-UI localization: ui_cultures + ~300 ui_translations
--     keys (tr + en). Without these the admin panel shows raw resource
--     keys instead of translated labels — required for ANY install.
--   - The notify_on_create / inbox (read-unread) UI strings, plus a
--     no-op flag update for any 'contact-message' content type (only
--     takes effect once such a content type exists — harmless here).
--
-- No tenant, no languages, no content types/fields/items are created
-- by this file. For a worked example tenant with demo content, see
-- 003_v1_demo_seed.sql.
-- ================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- 012: SystemAdmin (cross-tenant platform console)
--   Email:    root@system.local
--   Password: Admin123!
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM system_admins WHERE email = 'root@system.local')
BEGIN
    INSERT INTO system_admins (email, password_hash, full_name, is_active)
    VALUES ('root@system.local',
            '$2a$12$4lCE3hHfiyShtwR7bC9CG.YVgp5pny2Qx.cSxGzCJkqvXw89r25EW',
            N'System Root', 1);
    PRINT 'Seeded dev system admin: root@system.local / Admin123! (SystemAdmin)';
END
ELSE
    PRINT 'Dev system admin already seeded.';
GO

-- ============================================================
-- 013: Global UI cultures + shell (layout/nav/topbar/banner) translations
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM ui_cultures WHERE code = 'tr')
    INSERT INTO ui_cultures (code, name, is_default, is_active) VALUES ('tr', N'Türkçe', 1, 1);
IF NOT EXISTS (SELECT 1 FROM ui_cultures WHERE code = 'en')
    INSERT INTO ui_cultures (code, name, is_default, is_active) VALUES ('en', N'English', 0, 1);
GO

MERGE ui_translations AS target
USING (VALUES
    ('en','Nav.Platform',            N'Platform'),
    ('tr','Nav.Platform',            N'Platform'),
    ('en','Nav.Content',             N'Content'),
    ('tr','Nav.Content',             N'İçerik'),
    ('en','Nav.Settings',            N'Settings'),
    ('tr','Nav.Settings',            N'Ayarlar'),
    ('en','Nav.Library',             N'Library'),
    ('tr','Nav.Library',             N'Kütüphane'),
    ('en','Nav.Dashboard',           N'Dashboard'),
    ('tr','Nav.Dashboard',           N'Pano'),
    ('en','Nav.Tenants',             N'Tenants'),
    ('tr','Nav.Tenants',             N'Kiracılar'),
    ('en','Nav.ContentTypes',        N'Content types'),
    ('tr','Nav.ContentTypes',        N'İçerik tipleri'),
    ('en','Nav.Languages',           N'Languages'),
    ('tr','Nav.Languages',           N'Diller'),
    ('en','Nav.Dictionary',          N'Dictionary'),
    ('tr','Nav.Dictionary',          N'Sözlük'),
    ('en','Nav.Users',               N'Users'),
    ('tr','Nav.Users',               N'Kullanıcılar'),
    ('en','Nav.ApiManagement',       N'API management'),
    ('tr','Nav.ApiManagement',       N'API yönetimi'),
    ('en','Nav.Media',               N'Media'),
    ('tr','Nav.Media',               N'Medya'),
    ('en','Nav.NoContent',           N'No content available'),
    ('tr','Nav.NoContent',           N'İçerik yok'),
    ('en','Topbar.ChangePassword',   N'Change password'),
    ('tr','Topbar.ChangePassword',   N'Şifre değiştir'),
    ('en','Topbar.Logout',           N'Logout'),
    ('tr','Topbar.Logout',           N'Çıkış'),
    ('en','Topbar.ToggleMenu',       N'Toggle menu'),
    ('tr','Topbar.ToggleMenu',       N'Menüyü aç/kapat'),
    ('en','Topbar.EditingLanguage',  N'Editing language'),
    ('tr','Topbar.EditingLanguage',  N'Düzenleme dili'),
    ('en','Impersonation.Banner',    N'Impersonating tenant'),
    ('tr','Impersonation.Banner',    N'Kiracı taklit ediliyor'),
    ('en','Impersonation.Exit',      N'Exit impersonation'),
    ('tr','Impersonation.Exit',      N'Taklidi bitir')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 014: Admin view strings (Common.*, Status.*, Account.*, Home.*, ContentType.*,
--       ContentField.*, ContentItem.*, Dictionary.*, Language.*, User.*, Api.*, Media.*)
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en', N'Common.Activate',      N'Activate'),
    ('tr', N'Common.Activate',      N'Aktifleştir'),
    ('en', N'Common.Active',        N'Active'),
    ('tr', N'Common.Active',        N'Aktif'),
    ('en', N'Common.BackToHome',    N'Back to home'),
    ('tr', N'Common.BackToHome',    N'Ana sayfaya dön'),
    ('en', N'Common.Cancel',        N'Cancel'),
    ('tr', N'Common.Cancel',        N'İptal'),
    ('en', N'Common.Clear',         N'Clear'),
    ('tr', N'Common.Clear',         N'Temizle'),
    ('en', N'Common.Code',          N'Code'),
    ('tr', N'Common.Code',          N'Kod'),
    ('en', N'Common.ContentTypes',  N'Content types'),
    ('tr', N'Common.ContentTypes',  N'İçerik tipleri'),
    ('en', N'Common.Create',        N'Create'),
    ('tr', N'Common.Create',        N'Oluştur'),
    ('en', N'Common.Deactivate',    N'Deactivate'),
    ('tr', N'Common.Deactivate',    N'Pasifleştir'),
    ('en', N'Common.Default',       N'Default'),
    ('tr', N'Common.Default',       N'Varsayılan'),
    ('en', N'Common.Delete',        N'Delete'),
    ('tr', N'Common.Delete',        N'Sil'),
    ('en', N'Common.Edit',          N'Edit'),
    ('tr', N'Common.Edit',          N'Düzenle'),
    ('en', N'Common.Email',         N'Email'),
    ('tr', N'Common.Email',         N'E-posta'),
    ('en', N'Common.Filter',        N'Filter'),
    ('tr', N'Common.Filter',        N'Filtrele'),
    ('en', N'Common.Inactive',      N'Inactive'),
    ('tr', N'Common.Inactive',      N'Pasif'),
    ('en', N'Common.Name',          N'Name'),
    ('tr', N'Common.Name',          N'Ad'),
    ('en', N'Common.New',           N'New'),
    ('tr', N'Common.New',           N'Yeni'),
    ('en', N'Common.Password',      N'Password'),
    ('tr', N'Common.Password',      N'Parola'),
    ('en', N'Common.Role',          N'Role'),
    ('tr', N'Common.Role',          N'Rol'),
    ('en', N'Common.Save',          N'Save'),
    ('tr', N'Common.Save',          N'Kaydet'),
    ('en', N'Common.SaveChanges',   N'Save changes'),
    ('tr', N'Common.SaveChanges',   N'Değişiklikleri kaydet'),
    ('en', N'Common.Slug',          N'Slug'),
    ('tr', N'Common.Slug',          N'Slug'),
    ('en', N'Common.Status',        N'Status'),
    ('tr', N'Common.Status',        N'Durum'),
    ('en', N'Common.Updated',       N'Updated'),
    ('tr', N'Common.Updated',       N'Güncellenme'),
    ('en', N'Common.Users',         N'Users'),
    ('tr', N'Common.Users',         N'Kullanıcılar'),
    ('en', N'Status.Published',     N'Published'),
    ('tr', N'Status.Published',     N'Yayında'),
    ('en', N'Status.Draft',         N'Draft'),
    ('tr', N'Status.Draft',         N'Taslak'),
    ('en', N'Status.Archived',      N'Archived'),
    ('tr', N'Status.Archived',      N'Arşivlendi'),
    ('en', N'Account.AccessDenied', N'Access denied'),
    ('tr', N'Account.AccessDenied', N'Erişim reddedildi'),
    ('en', N'Account.ChangePassword', N'Change password'),
    ('tr', N'Account.ChangePassword', N'Parola değiştir'),
    ('en', N'Account.NoAccessText', N'You don''t have permission to view this page. Contact a tenant administrator if you think this is a mistake.'),
    ('tr', N'Account.NoAccessText', N'Bu sayfayı görüntüleme izniniz yok. Bunun bir hata olduğunu düşünüyorsanız bir kiracı yöneticisiyle iletişime geçin.'),
    ('en', N'Account.NoAccessTitle', N'You don''t have access'),
    ('tr', N'Account.NoAccessTitle', N'Erişiminiz yok'),
    ('en', N'Account.PasswordChanged', N'Your password has been changed.'),
    ('tr', N'Account.PasswordChanged', N'Parolanız değiştirildi.'),
    ('en', N'Account.RememberMe',   N'Remember me'),
    ('tr', N'Account.RememberMe',   N'Beni hatırla'),
    ('en', N'Account.SignIn',       N'Sign in'),
    ('tr', N'Account.SignIn',       N'Giriş yap'),
    ('en', N'Account.SignInTo',     N'to'),
    ('tr', N'Account.SignInTo',     N'şuraya'),
    ('en', N'SystemAccount.Console',     N'Platform console'),
    ('tr', N'SystemAccount.Console',     N'Platform konsolu'),
    ('en', N'SystemAccount.ConsoleHint', N'System administrator sign in'),
    ('tr', N'SystemAccount.ConsoleHint', N'Sistem yöneticisi girişi'),
    ('en', N'SystemAccount.SignIn',      N'Platform sign in'),
    ('tr', N'SystemAccount.SignIn',      N'Platform girişi'),
    ('en', N'Home.Api',             N'API'),
    ('tr', N'Home.Api',             N'API'),
    ('en', N'Home.ApiDesc',         N'Expose content as REST'),
    ('tr', N'Home.ApiDesc',         N'İçeriği REST olarak yayınlayın'),
    ('en', N'Home.ContentTypesDesc', N'Design your schema'),
    ('tr', N'Home.ContentTypesDesc', N'Şemanızı tasarlayın'),
    ('en', N'Home.Dashboard',       N'Dashboard'),
    ('tr', N'Home.Dashboard',       N'Pano'),
    ('en', N'Home.Dictionary',      N'Dictionary'),
    ('tr', N'Home.Dictionary',      N'Sözlük'),
    ('en', N'Home.DictionaryDesc',  N'Manage translations'),
    ('tr', N'Home.DictionaryDesc',  N'Çevirileri yönetin'),
    ('en', N'Home.Media',           N'Media'),
    ('tr', N'Home.Media',           N'Medya'),
    ('en', N'Home.MediaDesc',       N'Upload & manage assets'),
    ('tr', N'Home.MediaDesc',       N'Dosya yükleyin ve yönetin'),
    ('en', N'Home.Privacy',         N'Privacy Policy'),
    ('tr', N'Home.Privacy',         N'Gizlilik Politikası'),
    ('en', N'Home.PrivacyText',     N'Use this page to detail your site''s privacy policy.'),
    ('tr', N'Home.PrivacyText',     N'Sitenizin gizlilik politikasını ayrıntılandırmak için bu sayfayı kullanın.'),
    ('en', N'Home.SidebarHint',     N'Use the sidebar to open a content type and start editing items.'),
    ('tr', N'Home.SidebarHint',     N'Bir içerik tipi açmak ve öğeleri düzenlemeye başlamak için kenar çubuğunu kullanın.'),
    ('en', N'Home.Tagline',         N'Multi-tenant, multi-language dynamic content management.'),
    ('tr', N'Home.Tagline',         N'Çok kiracılı, çok dilli dinamik içerik yönetimi.'),
    ('en', N'Home.UsersDesc',       N'Accounts & permissions'),
    ('tr', N'Home.UsersDesc',       N'Hesaplar ve izinler'),
    ('en', N'Home.WelcomeBack',     N'Welcome back. Pick up where you left off.'),
    ('tr', N'Home.WelcomeBack',     N'Tekrar hoş geldiniz. Kaldığınız yerden devam edin.'),
    ('en', N'Error.Heading',        N'An error occurred while processing your request.'),
    ('tr', N'Error.Heading',        N'İsteğiniz işlenirken bir hata oluştu.'),
    ('en', N'Error.RequestId',      N'Request ID'),
    ('tr', N'Error.RequestId',      N'İstek No'),
    ('en', N'Error.Title',          N'Error'),
    ('tr', N'Error.Title',          N'Hata'),
    ('en', N'Dashboard.ActiveTenants', N'Active tenants'),
    ('tr', N'Dashboard.ActiveTenants', N'Aktif kiracılar'),
    ('en', N'Dashboard.Impersonate',   N'Impersonate'),
    ('tr', N'Dashboard.Impersonate',   N'Kimliğine bürün'),
    ('en', N'Dashboard.Title',         N'Platform dashboard'),
    ('tr', N'Dashboard.Title',         N'Platform panosu'),
    ('en', N'Tenant.Create',           N'Create tenant'),
    ('tr', N'Tenant.Create',           N'Kiracı oluştur'),
    ('en', N'Tenant.DeleteConfirm',    N'Delete tenant {0}? Its data stays but the tenant is removed.'),
    ('tr', N'Tenant.DeleteConfirm',    N'{0} kiracısı silinsin mi? Verileri kalır ancak kiracı kaldırılır.'),
    ('en', N'Tenant.Edit',             N'Edit tenant'),
    ('tr', N'Tenant.Edit',             N'Kiracı düzenle'),
    ('en', N'Tenant.EmptyText',        N'Create the first tenant to get started.'),
    ('tr', N'Tenant.EmptyText',        N'Başlamak için ilk kiracıyı oluşturun.'),
    ('en', N'Tenant.EmptyTitle',       N'No tenants yet'),
    ('tr', N'Tenant.EmptyTitle',       N'Henüz kiracı yok'),
    ('en', N'Tenant.FirstAdmin',       N'First tenant administrator'),
    ('tr', N'Tenant.FirstAdmin',       N'İlk kiracı yöneticisi'),
    ('en', N'Tenant.LangCodePlaceholder', N'tr'),
    ('tr', N'Tenant.LangCodePlaceholder', N'tr'),
    ('en', N'Tenant.LangNamePlaceholder', N'Türkçe'),
    ('tr', N'Tenant.LangNamePlaceholder', N'Türkçe'),
    ('en', N'Tenant.New',              N'New tenant'),
    ('tr', N'Tenant.New',              N'Yeni kiracı'),
    ('en', N'Tenant.SlugHint',         N'Subdomain key. Lowercase, kebab-case. Immutable after creation.'),
    ('tr', N'Tenant.SlugHint',         N'Alt alan adı anahtarı. Küçük harf, kebab-case. Oluşturulduktan sonra değiştirilemez.'),
    ('en', N'Tenant.SlugImmutableHint', N'Immutable — changing it would break subdomain routing.'),
    ('tr', N'Tenant.SlugImmutableHint', N'Değiştirilemez — değiştirmek alt alan adı yönlendirmesini bozar.'),
    ('en', N'Tenant.SlugPlaceholder',  N'e.g. acme'),
    ('tr', N'Tenant.SlugPlaceholder',  N'örn. acme'),
    ('en', N'Tenant.Tenant',           N'Tenant'),
    ('tr', N'Tenant.Tenant',           N'Kiracı'),
    ('en', N'Tenant.Tenants',          N'Tenants'),
    ('tr', N'Tenant.Tenants',          N'Kiracılar'),
    ('en', N'ContentType.Breadcrumb',  N'Content types'),
    ('tr', N'ContentType.Breadcrumb',  N'İçerik tipleri'),
    ('en', N'ContentType.DeleteConfirm', N'Delete this content type?'),
    ('tr', N'ContentType.DeleteConfirm', N'Bu içerik tipini sil?'),
    ('en', N'ContentType.Edit',        N'Edit content type'),
    ('tr', N'ContentType.Edit',        N'İçerik tipini düzenle'),
    ('en', N'ContentType.Empty.Text',  N'Define your first content schema to start building.'),
    ('tr', N'ContentType.Empty.Text',  N'Başlamak için ilk içerik şemanı tanımla.'),
    ('en', N'ContentType.Empty.Title', N'No content types yet'),
    ('tr', N'ContentType.Empty.Title', N'Henüz içerik tipi yok'),
    ('en', N'ContentType.Fields',      N'Fields'),
    ('tr', N'ContentType.Fields',      N'Alanlar'),
    ('en', N'ContentType.IconHint',    N'Pick an icon for the sidebar menu.'),
    ('tr', N'ContentType.IconHint',    N'Sol menüde görünecek ikonu seçin.'),
    ('en', N'ContentType.IconPlaceholder', N'e.g. bi-file-text'),
    ('tr', N'ContentType.IconPlaceholder', N'örn. bi-file-text'),
    ('en', N'ContentType.Items',       N'Items'),
    ('tr', N'ContentType.Items',       N'Kayıtlar'),
    ('en', N'ContentType.New',         N'New content type'),
    ('tr', N'ContentType.New',         N'Yeni içerik tipi'),
    ('en', N'ContentType.Order',       N'Order'),
    ('tr', N'ContentType.Order',       N'Sıra'),
    ('en', N'ContentType.SlugHint',    N'Used in URLs and the public API. Lowercase, kebab-case.'),
    ('tr', N'ContentType.SlugHint',    N'URL ve genel API''de kullanılır. Küçük harf, kebab-case.'),
    ('en', N'ContentType.SlugPlaceholder', N'e.g. blog-posts'),
    ('tr', N'ContentType.SlugPlaceholder', N'örn. blog-posts'),
    ('en', N'ContentType.Title',       N'Content types'),
    ('tr', N'ContentType.Title',       N'İçerik tipleri'),
    ('en', N'ContentField.Add',        N'Add field'),
    ('tr', N'ContentField.Add',        N'Alan ekle'),
    ('en', N'ContentField.DeleteConfirm', N'Delete this field?'),
    ('tr', N'ContentField.DeleteConfirm', N'Bu alanı sil?'),
    ('en', N'ContentField.Edit',       N'Edit field'),
    ('tr', N'ContentField.Edit',       N'Alanı düzenle'),
    ('en', N'ContentField.Empty.Text', N'Add the first field using the panel on the right.'),
    ('tr', N'ContentField.Empty.Text', N'İlk alanı sağdaki panelden ekle.'),
    ('en', N'ContentField.Empty.Title', N'No fields yet'),
    ('tr', N'ContentField.Empty.Title', N'Henüz alan yok'),
    ('en', N'ContentField.Localized',  N'localized'),
    ('tr', N'ContentField.Localized',  N'çok dilli'),
    ('en', N'ContentField.OptionsHint', N'Type-specific options as JSON (choices, validation, relation config…).'),
    ('tr', N'ContentField.OptionsHint', N'Tipe özel ayarlar JSON olarak (seçenekler, doğrulama, ilişki yapılandırması…).'),
    ('en', N'ContentField.ReorderHint', N'Drag the handle to reorder. Order is saved automatically.'),
    ('tr', N'ContentField.ReorderHint', N'Sıralamak için tutamacı sürükle. Sıra otomatik kaydedilir.'),
    ('en', N'ContentField.Required',   N'required'),
    ('tr', N'ContentField.Required',   N'zorunlu'),
    ('en', N'ContentField.SlugPlaceholder', N'e.g. title'),
    ('tr', N'ContentField.SlugPlaceholder', N'örn. title'),
    ('en', N'ContentField.Title',      N'Fields'),
    ('tr', N'ContentField.Title',      N'Alanlar'),
    ('en', N'ContentField.TitleFor',   N'Fields — {0}'),
    ('tr', N'ContentField.TitleFor',   N'Alanlar — {0}'),
    ('en', N'ContentItem.DeleteConfirm', N'Delete this item?'),
    ('tr', N'ContentItem.DeleteConfirm', N'Bu kaydı sil?'),
    ('en', N'ContentItem.EditFor',     N'Edit {0} #{1}'),
    ('tr', N'ContentItem.EditFor',     N'{0} #{1} düzenle'),
    ('en', N'ContentItem.Empty.Text',  N'Create the first {0} item.'),
    ('tr', N'ContentItem.Empty.Text',  N'İlk {0} kaydını oluştur.'),
    ('en', N'ContentItem.Language',    N'Language'),
    ('tr', N'ContentItem.Language',    N'Dil'),
    ('en', N'ContentItem.MediaHintMultiple', N'Pick one or more from the media library.'),
    ('tr', N'ContentItem.MediaHintMultiple', N'Medya kütüphanesinden bir veya daha fazla seç.'),
    ('en', N'ContentItem.MediaHintSingle', N'Pick a media item.'),
    ('tr', N'ContentItem.MediaHintSingle', N'Bir medya öğesi seç.'),
    ('en', N'ContentItem.MediaSearch',           N'Search media…'),
    ('tr', N'ContentItem.MediaSearch',           N'Medya ara…'),
    ('en', N'ContentItem.MediaSelectButton',     N'Select file'),
    ('tr', N'ContentItem.MediaSelectButton',     N'Dosya Seç'),
    ('en', N'ContentItem.MediaModalTitle',       N'Select media'),
    ('tr', N'ContentItem.MediaModalTitle',       N'Medya seç'),
    ('en', N'ContentItem.MediaTabLibrary',       N'Library'),
    ('tr', N'ContentItem.MediaTabLibrary',       N'Kütüphane'),
    ('en', N'ContentItem.MediaTabUpload',        N'Upload from computer'),
    ('tr', N'ContentItem.MediaTabUpload',        N'Bilgisayardan Yükle'),
    ('en', N'ContentItem.MediaModalDone',        N'Add selected'),
    ('tr', N'ContentItem.MediaModalDone',        N'Seçilenleri ekle'),
    ('en', N'ContentItem.MediaModalEmpty',       N'No media found.'),
    ('tr', N'ContentItem.MediaModalEmpty',       N'Medya bulunamadı.'),
    ('en', N'ContentItem.MediaTooLarge',         N'File is too large.'),
    ('tr', N'ContentItem.MediaTooLarge',         N'Dosya çok büyük.'),
    ('en', N'ContentItem.MediaFormatNotAllowed', N'This file type is not allowed.'),
    ('tr', N'ContentItem.MediaFormatNotAllowed', N'Bu dosya türüne izin verilmiyor.'),
    ('en', N'ContentItem.New',         N'New item'),
    ('tr', N'ContentItem.New',         N'Yeni kayıt'),
    ('en', N'ContentItem.NewFor',      N'New {0}'),
    ('tr', N'ContentItem.NewFor',      N'Yeni {0}'),
    ('en', N'ContentItem.NoFields',    N'This content type has no fields yet. Add fields first.'),
    ('tr', N'ContentItem.NoFields',    N'Bu içerik tipinin henüz alanı yok. Önce alan ekle.'),
    ('en', N'ContentItem.NoItems',     N'No items yet'),
    ('tr', N'ContentItem.NoItems',     N'Henüz kayıt yok'),
    ('en', N'ContentItem.RangeEnd',    N'End'),
    ('tr', N'ContentItem.RangeEnd',    N'Bitiş'),
    ('en', N'ContentItem.RangeStart',  N'Start'),
    ('tr', N'ContentItem.RangeStart',  N'Başlangıç'),
    ('en', N'ContentItem.RelationItems', N'items'),
    ('tr', N'ContentItem.RelationItems', N'öğe'),
    ('en', N'ContentItem.RelationMax', N'max'),
    ('tr', N'ContentItem.RelationMax', N'en fazla'),
    ('en', N'ContentItem.RelationMin', N'min'),
    ('tr', N'ContentItem.RelationMin', N'en az'),
    ('en', N'ContentItem.RelationNotConfigured', N'Relation target not configured (set target_content_type_id in options).'),
    ('tr', N'ContentItem.RelationNotConfigured', N'İlişki hedefi yapılandırılmamış (ayarlarda target_content_type_id belirt).'),
    ('en', N'ContentItem.RelationSearch', N'Search to add…'),
    ('tr', N'ContentItem.RelationSearch', N'Eklemek için ara…'),
    ('en', N'ContentItem.SelectPlaceholder', N'— select —'),
    ('tr', N'ContentItem.SelectPlaceholder', N'— seç —'),
    ('en', N'ContentItem.SlugPlaceholder', N'optional, e.g. my-post'),
    ('tr', N'ContentItem.SlugPlaceholder', N'isteğe bağlı, örn. my-post'),
    ('en', N'Dictionary.Category',     N'Category'),
    ('tr', N'Dictionary.Category',     N'Kategori'),
    ('en', N'Dictionary.DeleteConfirm', N'Delete this key?'),
    ('tr', N'Dictionary.DeleteConfirm', N'Bu anahtar silinsin mi?'),
    ('en', N'Dictionary.EditTitle',    N'Edit dictionary key'),
    ('tr', N'Dictionary.EditTitle',    N'Sözlük anahtarını düzenle'),
    ('en', N'Dictionary.EmptyHint',    N'Add i18n keys and their translations.'),
    ('tr', N'Dictionary.EmptyHint',    N'i18n anahtarlarını ve çevirilerini ekleyin.'),
    ('en', N'Dictionary.EmptyTitle',   N'No dictionary keys'),
    ('tr', N'Dictionary.EmptyTitle',   N'Sözlük anahtarı yok'),
    ('en', N'Dictionary.Key',          N'Key'),
    ('tr', N'Dictionary.Key',          N'Anahtar'),
    ('en', N'Dictionary.NewKey',       N'New key'),
    ('tr', N'Dictionary.NewKey',       N'Yeni anahtar'),
    ('en', N'Dictionary.NewTitle',     N'New dictionary key'),
    ('tr', N'Dictionary.NewTitle',     N'Yeni sözlük anahtarı'),
    ('en', N'Dictionary.NoLanguages',  N'No active languages. Add a language before entering translations.'),
    ('tr', N'Dictionary.NoLanguages',  N'Aktif dil yok. Çeviri girmeden önce bir dil ekleyin.'),
    ('en', N'Dictionary.SearchKey',    N'Search key…'),
    ('tr', N'Dictionary.SearchKey',    N'Anahtar ara…'),
    ('en', N'Dictionary.Title',        N'Dictionary'),
    ('tr', N'Dictionary.Title',        N'Sözlük'),
    ('en', N'Dictionary.Translations', N'Translations'),
    ('tr', N'Dictionary.Translations', N'Çeviriler'),
    ('en', N'Language.CodeHint',       N'2-5 letters (e.g. tr, en, de). Lowercase.'),
    ('tr', N'Language.CodeHint',       N'2-5 harf (örn. tr, en, de). Küçük harf.'),
    ('en', N'Language.CodeImmutableHint', N'Immutable — content values reference this code.'),
    ('tr', N'Language.CodeImmutableHint', N'Değiştirilemez — içerik değerleri bu kodu kullanır.'),
    ('en', N'Language.CodePlaceholder', N'e.g. de'),
    ('tr', N'Language.CodePlaceholder', N'örn. de'),
    ('en', N'Language.DefaultHint',    N'Making this the default unsets the previous default and forces it active.'),
    ('tr', N'Language.DefaultHint',    N'Bunu varsayılan yapmak önceki varsayılanı kaldırır ve bu dili aktif eder.'),
    ('en', N'Language.EditTitle',      N'Edit language'),
    ('tr', N'Language.EditTitle',      N'Dili düzenle'),
    ('en', N'Language.EmptyTitle',     N'No languages yet'),
    ('tr', N'Language.EmptyTitle',     N'Henüz dil yok'),
    ('en', N'Language.FlagHint',       N'Optional Bootstrap Icons class.'),
    ('tr', N'Language.FlagHint',       N'İsteğe bağlı Bootstrap Icons sınıfı.'),
    ('en', N'Language.FlagPlaceholder', N'e.g. bi-flag'),
    ('tr', N'Language.FlagPlaceholder', N'örn. bi-flag'),
    ('en', N'Language.NamePlaceholder', N'e.g. Deutsch'),
    ('tr', N'Language.NamePlaceholder', N'örn. Deutsch'),
    ('en', N'Language.New',            N'New language'),
    ('tr', N'Language.New',            N'Yeni dil'),
    ('en', N'Language.NewTitle',       N'New language'),
    ('tr', N'Language.NewTitle',       N'Yeni dil'),
    ('en', N'Language.Title',          N'Languages'),
    ('tr', N'Language.Title',          N'Diller'),
    ('en', N'User.ContentType',        N'Content type'),
    ('tr', N'User.ContentType',        N'İçerik tipi'),
    ('en', N'User.DeleteConfirm',      N'Delete this user?'),
    ('tr', N'User.DeleteConfirm',      N'Bu kullanıcı silinsin mi?'),
    ('en', N'User.EditTitle',          N'Edit user'),
    ('tr', N'User.EditTitle',          N'Kullanıcıyı düzenle'),
    ('en', N'User.EmptyTitle',         N'No users yet'),
    ('tr', N'User.EmptyTitle',         N'Henüz kullanıcı yok'),
    ('en', N'User.New',                N'New user'),
    ('tr', N'User.New',                N'Yeni kullanıcı'),
    ('en', N'User.NewPasswordKeep',    N'New password (leave blank to keep)'),
    ('tr', N'User.NewPasswordKeep',    N'Yeni parola (boş bırakırsanız değişmez)'),
    ('en', N'User.NewTitle',           N'New user'),
    ('tr', N'User.NewTitle',           N'Yeni kullanıcı'),
    ('en', N'User.Password',           N'Password'),
    ('tr', N'User.Password',           N'Parola'),
    ('en', N'User.PermCreate',         N'Create'),
    ('tr', N'User.PermCreate',         N'Oluşturma'),
    ('en', N'User.PermDelete',         N'Delete'),
    ('tr', N'User.PermDelete',         N'Silme'),
    ('en', N'User.PermRead',           N'Read'),
    ('tr', N'User.PermRead',           N'Okuma'),
    ('en', N'User.PermUpdate',         N'Update'),
    ('tr', N'User.PermUpdate',         N'Güncelleme'),
    ('en', N'User.Permissions',        N'Permissions'),
    ('tr', N'User.Permissions',        N'İzinler'),
    ('en', N'User.PermissionsEmptyHint', N'There are no content types to grant permissions for yet.'),
    ('tr', N'User.PermissionsEmptyHint', N'Henüz izin verilecek içerik tipi yok.'),
    ('en', N'User.PermissionsEmptyTitle', N'Nothing to assign'),
    ('tr', N'User.PermissionsEmptyTitle', N'Atanacak bir şey yok'),
    ('en', N'User.PermissionsTitle',   N'Permissions — {0}'),
    ('tr', N'User.PermissionsTitle',   N'İzinler — {0}'),
    ('en', N'User.SavePermissions',    N'Save permissions'),
    ('tr', N'User.SavePermissions',    N'İzinleri kaydet'),
    ('en', N'User.Title',              N'Users'),
    ('tr', N'User.Title',              N'Kullanıcılar'),
    ('en', N'User.User',               N'User'),
    ('tr', N'User.User',               N'Kullanıcı'),
    ('en', N'Api.Api',                 N'API'),
    ('tr', N'Api.Api',                 N'API'),
    ('en', N'Api.ApiKey',              N'API key'),
    ('tr', N'Api.ApiKey',              N'API anahtarı'),
    ('en', N'Api.ApiKeyNone',          N'No key generated yet.'),
    ('tr', N'Api.ApiKeyNone',          N'Henüz anahtar oluşturulmadı.'),
    ('en', N'Api.ApiKeySet',           N'A key is set (hashed, not shown). Rotating replaces it.'),
    ('tr', N'Api.ApiKeySet',           N'Bir anahtar tanımlı (hash''li, gösterilmez). Yenilemek mevcut anahtarı değiştirir.'),
    ('en', N'Api.Auth',                N'Auth'),
    ('tr', N'Api.Auth',                N'Kimlik doğrulama'),
    ('en', N'Api.AuthHint',            N'ApiKey → header X-API-Key. JWT → header Authorization: Bearer &lt;token&gt;.'),
    ('tr', N'Api.AuthHint',            N'ApiKey → başlık X-API-Key. JWT → başlık Authorization: Bearer &lt;token&gt;.'),
    ('en', N'Api.Authentication',      N'Authentication'),
    ('tr', N'Api.Authentication',      N'Kimlik doğrulama'),
    ('en', N'Api.BearerToken',         N'Bearer token (JWT)'),
    ('tr', N'Api.BearerToken',         N'Bearer token (JWT)'),
    ('en', N'Api.BearerTokenHint',     N'CMS-signed, valid 365 days, scoped to this content type. Tokens are stateless — generate as many as needed.'),
    ('tr', N'Api.BearerTokenHint',     N'CMS tarafından imzalanır, 365 gün geçerli, bu içerik tipine özeldir. Token''lar durumsuzdur — ihtiyacınız kadar üretebilirsiniz.'),
    ('en', N'Api.CacheSeconds',        N'Cache seconds'),
    ('tr', N'Api.CacheSeconds',        N'Önbellek saniyesi'),
    ('en', N'Api.Configure',           N'Configure'),
    ('tr', N'Api.Configure',           N'Yapılandır'),
    ('en', N'Api.ViewDocs',            N'View API Docs'),
    ('tr', N'Api.ViewDocs',            N'API Dokümanları'),
    ('en', N'Api.ConfigureTitle',      N'API — {0}'),
    ('tr', N'Api.ConfigureTitle',      N'API — {0}'),
    ('en', N'Api.ContentType',         N'Content type'),
    ('tr', N'Api.ContentType',         N'İçerik tipi'),
    ('en', N'Api.Credentials',         N'Credentials'),
    ('tr', N'Api.Credentials',         N'Kimlik bilgileri'),
    ('en', N'Api.Disabled',            N'Disabled'),
    ('tr', N'Api.Disabled',            N'Devre dışı'),
    ('en', N'Api.EmptyHint',           N'Create a content type before exposing it as an API.'),
    ('tr', N'Api.EmptyHint',           N'API olarak yayınlamadan önce bir içerik tipi oluşturun.'),
    ('en', N'Api.EmptyTitle',          N'No content types yet'),
    ('tr', N'Api.EmptyTitle',          N'Henüz içerik tipi yok'),
    ('en', N'Api.Enabled',             N'Enabled'),
    ('tr', N'Api.Enabled',             N'Etkin'),
    ('en', N'Api.ExposeAsApi',         N'Expose as API'),
    ('tr', N'Api.ExposeAsApi',         N'API olarak yayınla'),
    ('en', N'Api.Field',               N'Field'),
    ('tr', N'Api.Field',               N'Alan'),
    ('en', N'Api.FieldVisibility',     N'Field visibility'),
    ('tr', N'Api.FieldVisibility',     N'Alan görünürlüğü'),
    ('en', N'Api.Filtering',           N'Filtering'),
    ('tr', N'Api.Filtering',           N'Filtreleme'),
    ('en', N'Api.GenerateApiKey',      N'Generate API key'),
    ('tr', N'Api.GenerateApiKey',      N'API anahtarı oluştur'),
    ('en', N'Api.GenerateToken',       N'Generate token'),
    ('tr', N'Api.GenerateToken',       N'Token oluştur'),
    ('en', N'Api.NewApiKey',           N'New API key (shown once — copy it now):'),
    ('tr', N'Api.NewApiKey',           N'Yeni API anahtarı (bir kez gösterilir — şimdi kopyalayın):'),
    ('en', N'Api.NewToken',            N'New Bearer token (valid 365 days — shown once, copy it now):'),
    ('tr', N'Api.NewToken',            N'Yeni Bearer token (365 gün geçerli — bir kez gösterilir, şimdi kopyalayın):'),
    ('en', N'Api.NoFields',            N'This content type has no fields yet.'),
    ('tr', N'Api.NoFields',            N'Bu içerik tipinin henüz alanı yok.'),
    ('en', N'Api.NotConfigured',       N'Not configured'),
    ('tr', N'Api.NotConfigured',       N'Yapılandırılmadı'),
    ('en', N'Api.Or',                  N'or'),
    ('tr', N'Api.Or',                  N'veya'),
    ('en', N'Api.Pagination',          N'Pagination'),
    ('tr', N'Api.Pagination',          N'Sayfalama'),
    ('en', N'Api.Preview',             N'Preview'),
    ('tr', N'Api.Preview',             N'Önizleme'),
    ('en', N'Api.RateLimit',           N'Rate limit / min'),
    ('tr', N'Api.RateLimit',           N'İstek limiti / dk'),
    ('en', N'Api.ResponseKeyAlias',    N'Response key alias'),
    ('tr', N'Api.ResponseKeyAlias',    N'Yanıt anahtarı takma adı'),
    ('en', N'Api.RotateApiKey',        N'Rotate API key'),
    ('tr', N'Api.RotateApiKey',        N'API anahtarını yenile'),
    ('en', N'Api.SaveConfiguration',   N'Save configuration'),
    ('tr', N'Api.SaveConfiguration',   N'Yapılandırmayı kaydet'),
    ('en', N'Api.Settings',            N'Settings'),
    ('tr', N'Api.Settings',            N'Ayarlar'),
    ('en', N'Api.SingleItem',          N'Single item:'),
    ('tr', N'Api.SingleItem',          N'Tekil öğe:'),
    ('en', N'Api.Sorting',             N'Sorting'),
    ('tr', N'Api.Sorting',             N'Sıralama'),
    ('en', N'Api.Title',               N'API management'),
    ('tr', N'Api.Title',               N'API yönetimi'),
    ('en', N'Api.Visible',             N'Visible'),
    ('tr', N'Api.Visible',             N'Görünür'),
    ('en', N'Media.ApplyCrop',         N'Apply crop'),
    ('tr', N'Media.ApplyCrop',         N'Kırpmayı uygula'),
    ('en', N'Media.Crop',              N'Crop image'),
    ('tr', N'Media.Crop',              N'Görseli kırp'),
    ('en', N'Media.DeleteConfirm',     N'Delete this media?'),
    ('tr', N'Media.DeleteConfirm',     N'Bu medya silinsin mi?'),
    ('en', N'Media.EmptyHint',         N'Upload your first asset above.'),
    ('tr', N'Media.EmptyHint',         N'İlk dosyanızı yukarıdan yükleyin.'),
    ('en', N'Media.EmptyTitle',        N'No media yet'),
    ('tr', N'Media.EmptyTitle',        N'Henüz medya yok'),
    ('en', N'Media.Title',             N'Media library'),
    ('tr', N'Media.Title',             N'Medya kütüphanesi'),
    ('en', N'Media.Upload',            N'Upload'),
    ('tr', N'Media.Upload',            N'Yükle'),
    ('en', N'Media.UploadFile',        N'Upload a file'),
    ('tr', N'Media.UploadFile',        N'Dosya yükle'),
    ('en', N'Media.UploadHint',        N'Images, video, audio or any file (max 50 MB). Dimensions are detected for images.'),
    ('tr', N'Media.UploadHint',        N'Görsel, video, ses veya herhangi bir dosya (en fazla 50 MB). Görsellerin boyutları otomatik algılanır.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 015: Field labels, validation messages, controller messages
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en', N'Field.CurrentPassword',    N'Current password'),
    ('tr', N'Field.CurrentPassword',    N'Mevcut parola'),
    ('en', N'Field.NewPassword',        N'New password'),
    ('tr', N'Field.NewPassword',        N'Yeni parola'),
    ('en', N'Field.ConfirmPassword',    N'Confirm new password'),
    ('tr', N'Field.ConfirmPassword',    N'Yeni parolayı onayla'),
    ('en', N'Field.FieldType',          N'Field type'),
    ('tr', N'Field.FieldType',          N'Alan tipi'),
    ('en', N'Field.Required',           N'Required'),
    ('tr', N'Field.Required',           N'Zorunlu'),
    ('en', N'Field.Localized',          N'Localized'),
    ('tr', N'Field.Localized',          N'Dile özel'),
    ('en', N'Field.Options',            N'Options (JSON)'),
    ('tr', N'Field.Options',            N'Seçenekler (JSON)'),
    ('en', N'Field.Description',        N'Description'),
    ('tr', N'Field.Description',        N'Açıklama'),
    ('en', N'Field.Icon',               N'Icon class'),
    ('tr', N'Field.Icon',               N'İkon sınıfı'),
    ('en', N'Field.Published',          N'Published'),
    ('tr', N'Field.Published',          N'Yayında'),
    ('en', N'Field.SortOrder',          N'Sort order'),
    ('tr', N'Field.SortOrder',          N'Sıralama'),
    ('en', N'Field.FlagIcon',           N'Flag icon'),
    ('tr', N'Field.FlagIcon',           N'Bayrak ikonu'),
    ('en', N'Field.IsDefaultLanguage',  N'Default language'),
    ('tr', N'Field.IsDefaultLanguage',  N'Varsayılan dil'),
    ('en', N'Field.FullName',           N'Full name'),
    ('tr', N'Field.FullName',           N'Ad soyad'),
    ('en', N'Field.DefaultLanguageCode', N'Default language code'),
    ('tr', N'Field.DefaultLanguageCode', N'Varsayılan dil kodu'),
    ('en', N'Field.DefaultLanguageName', N'Default language name'),
    ('tr', N'Field.DefaultLanguageName', N'Varsayılan dil adı'),
    ('en', N'Field.FirstAdminEmail',    N'First admin email'),
    ('tr', N'Field.FirstAdminEmail',    N'İlk yönetici e-postası'),
    ('en', N'Field.FirstAdminFullName', N'First admin full name'),
    ('tr', N'Field.FirstAdminFullName', N'İlk yönetici adı soyadı'),
    ('en', N'Field.FirstAdminPassword', N'First admin password'),
    ('tr', N'Field.FirstAdminPassword', N'İlk yönetici parolası'),
    ('en', N'Validation.Required',      N'{0} is required.'),
    ('tr', N'Validation.Required',      N'{0} gerekli.'),
    ('en', N'Validation.RequiredFv',    N'{PropertyName} is required.'),
    ('tr', N'Validation.RequiredFv',    N'{PropertyName} gerekli.'),
    ('en', N'Validation.Email',         N'Enter a valid email.'),
    ('tr', N'Validation.Email',         N'Geçerli bir e-posta girin.'),
    ('en', N'Validation.PasswordLength', N'Password must be at least 8 characters.'),
    ('tr', N'Validation.PasswordLength', N'Parola en az 8 karakter olmalı.'),
    ('en', N'Validation.PasswordsMatch', N'Passwords do not match.'),
    ('tr', N'Validation.PasswordsMatch', N'Parolalar eşleşmiyor.'),
    ('en', N'Validation.Slug',          N'Slug must be lowercase letters, numbers and hyphens only.'),
    ('tr', N'Validation.Slug',          N'Slug yalnızca küçük harf, rakam ve tire içerebilir.'),
    ('en', N'Validation.FieldType',     N'Invalid field type.'),
    ('tr', N'Validation.FieldType',     N'Geçersiz alan tipi.'),
    ('en', N'Validation.OptionsJson',   N'Options must be valid JSON.'),
    ('tr', N'Validation.OptionsJson',   N'Seçenekler geçerli JSON olmalı.'),
    ('en', N'Validation.Status',        N'Status must be draft, published or archived.'),
    ('tr', N'Validation.Status',        N'Durum draft, published veya archived olmalı.'),
    ('en', N'Validation.DictionaryKey', N'Key may contain letters, numbers and . _ - separators (e.g. nav.home).'),
    ('tr', N'Validation.DictionaryKey', N'Anahtar yalnızca harf, rakam ve . _ - ayraçları içerebilir (örn. nav.home).'),
    ('en', N'Validation.LanguageCode',  N'Code must be 2-5 letters (e.g. ''tr'', ''en'', ''de'').'),
    ('tr', N'Validation.LanguageCode',  N'Kod 2-5 harf olmalı (örn. ''tr'', ''en'', ''de'').'),
    ('en', N'Validation.LanguageCodeLower', N'Language code must be 2-5 lowercase letters (e.g. ''tr'').'),
    ('tr', N'Validation.LanguageCodeLower', N'Dil kodu 2-5 küçük harf olmalı (örn. ''tr'').'),
    ('en', N'Validation.TenantSlug',    N'Slug may contain only lowercase letters, digits and hyphens.'),
    ('tr', N'Validation.TenantSlug',    N'Slug yalnızca küçük harf, rakam ve tire içerebilir.'),
    ('en', N'Validation.Role',          N'Invalid role.'),
    ('tr', N'Validation.Role',          N'Geçersiz rol.'),
    ('en', N'Msg.ImageCropped',         N'Image cropped.'),
    ('tr', N'Msg.ImageCropped',         N'Görsel kırpıldı.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 016: SystemAdmin translation-management screen keys
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en','Nav.Translations',        N'Translations'),
    ('tr','Nav.Translations',        N'Çeviriler'),
    ('en','Translation.Title',       N'Translations'),
    ('tr','Translation.Title',       N'Çeviriler'),
    ('en','Translation.EditTitle',   N'Edit translation'),
    ('tr','Translation.EditTitle',   N'Çeviriyi düzenle'),
    ('en','Translation.Key',         N'Key'),
    ('tr','Translation.Key',         N'Anahtar'),
    ('en','Translation.SearchKey',   N'Search key…'),
    ('tr','Translation.SearchKey',   N'Anahtar ara…'),
    ('en','Translation.Languages',   N'Languages'),
    ('tr','Translation.Languages',   N'Diller'),
    ('en','Translation.AddLanguage', N'Add language'),
    ('tr','Translation.AddLanguage', N'Dil ekle'),
    ('en','Translation.ImportExport',N'Import / Export'),
    ('tr','Translation.ImportExport',N'İçe / Dışa aktar'),
    ('en','Translation.Import',      N'Import'),
    ('tr','Translation.Import',      N'İçe aktar'),
    ('en','Translation.ImportHint',  N'Paste a JSON object of "key": "value" pairs for the selected language.'),
    ('tr','Translation.ImportHint',  N'Seçili dil için "anahtar": "değer" çiftlerinden oluşan bir JSON yapıştırın.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 017: Translation-management service/controller messages
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en','Msg.Saved',                  N'Saved.'),
    ('tr','Msg.Saved',                  N'Kaydedildi.'),
    ('en','Msg.LanguageAdded',          N'Language added.'),
    ('tr','Msg.LanguageAdded',          N'Dil eklendi.'),
    ('en','Msg.Imported',               N'Imported {0} entries.'),
    ('tr','Msg.Imported',               N'{0} kayıt içe aktarıldı.'),
    ('en','Msg.CultureCodeInvalid',     N'Code must be 2-5 lowercase letters.'),
    ('tr','Msg.CultureCodeInvalid',     N'Kod 2-5 küçük harf olmalı.'),
    ('en','Msg.NameRequired',           N'Name is required.'),
    ('tr','Msg.NameRequired',           N'Ad gerekli.'),
    ('en','Msg.CultureExists',          N'Culture ''{0}'' already exists.'),
    ('tr','Msg.CultureExists',          N'''{0}'' kültürü zaten var.'),
    ('en','Msg.CultureRequired',        N'Culture is required.'),
    ('tr','Msg.CultureRequired',        N'Kültür gerekli.'),
    ('en','Msg.CultureMissing',         N'Culture ''{0}'' does not exist. Add it first.'),
    ('tr','Msg.CultureMissing',         N'''{0}'' kültürü yok. Önce ekleyin.'),
    ('en','Msg.InvalidJson',            N'Invalid JSON. Expected an object of key/value pairs.'),
    ('tr','Msg.InvalidJson',            N'Geçersiz JSON. Anahtar/değer çiftlerinden oluşan bir nesne bekleniyordu.'),
    ('en','Msg.NoEntries',              N'No entries to import.'),
    ('tr','Msg.NoEntries',              N'İçe aktarılacak kayıt yok.'),
    ('en','Msg.TranslationKeyNotFound', N'Translation key not found.'),
    ('tr','Msg.TranslationKeyNotFound', N'Çeviri anahtarı bulunamadı.'),
    ('en','Msg.KeyRequired',            N'Key is required.'),
    ('tr','Msg.KeyRequired',            N'Anahtar gerekli.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 018: Application service Result.Failure messages (Err.*)
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en', N'Err.ContentTypeNotFound',    N'Content type not found.'),
    ('tr', N'Err.ContentTypeNotFound',    N'İçerik tipi bulunamadı.'),
    ('en', N'Err.ContentFieldNotFound',   N'Field not found.'),
    ('tr', N'Err.ContentFieldNotFound',   N'Alan bulunamadı.'),
    ('en', N'Err.ContentItemNotFound',    N'Content item not found.'),
    ('tr', N'Err.ContentItemNotFound',    N'İçerik kaydı bulunamadı.'),
    ('en', N'Err.UserNotFound',           N'User not found.'),
    ('tr', N'Err.UserNotFound',           N'Kullanıcı bulunamadı.'),
    ('en', N'Err.TenantNotFound',         N'Tenant not found.'),
    ('tr', N'Err.TenantNotFound',         N'Kiracı bulunamadı.'),
    ('en', N'Err.LanguageNotFound',       N'Language not found.'),
    ('tr', N'Err.LanguageNotFound',       N'Dil bulunamadı.'),
    ('en', N'Err.MediaNotFound',          N'Media asset not found.'),
    ('tr', N'Err.MediaNotFound',          N'Medya bulunamadı.'),
    ('en', N'Err.DictionaryNotFound',     N'Dictionary entry not found.'),
    ('tr', N'Err.DictionaryNotFound',     N'Sözlük kaydı bulunamadı.'),
    ('en', N'Err.SystemAdminNotFound',    N'System admin not found.'),
    ('tr', N'Err.SystemAdminNotFound',    N'Sistem yöneticisi bulunamadı.'),
    ('en', N'Err.UpdateFailed',           N'Update failed.'),
    ('tr', N'Err.UpdateFailed',           N'Güncelleme başarısız.'),
    ('en', N'Err.NotAuthenticated',       N'Not authenticated.'),
    ('tr', N'Err.NotAuthenticated',       N'Oturum açılmamış.'),
    ('en', N'Err.PasswordLengthRange',    N'New password must be between 8 and 128 characters.'),
    ('tr', N'Err.PasswordLengthRange',    N'Yeni parola 8-128 karakter olmalı.'),
    ('en', N'Err.CurrentPasswordIncorrect', N'Current password is incorrect.'),
    ('tr', N'Err.CurrentPasswordIncorrect', N'Mevcut parola yanlış.'),
    ('en', N'Err.NoFieldOrder',           N'No field order provided.'),
    ('tr', N'Err.NoFieldOrder',           N'Alan sırası belirtilmedi.'),
    ('en', N'Err.RateLimitRange',         N'Rate limit must be between 1 and 100000.'),
    ('tr', N'Err.RateLimitRange',         N'İstek limiti 1 ile 100000 arasında olmalı.'),
    ('en', N'Err.CacheNegative',          N'Cache seconds cannot be negative.'),
    ('tr', N'Err.CacheNegative',          N'Önbellek saniyesi negatif olamaz.'),
    ('en', N'Err.SaveApiConfigFirst',     N'Save the API configuration before generating a key.'),
    ('tr', N'Err.SaveApiConfigFirst',     N'Anahtar üretmeden önce API yapılandırmasını kaydedin.'),
    ('en', N'Err.LanguageDefaultDeactivate', N'The default language cannot be deactivated.'),
    ('tr', N'Err.LanguageDefaultDeactivate', N'Varsayılan dil pasifleştirilemez.'),
    ('en', N'Err.LanguageLastActive',     N'At least one language must stay active.'),
    ('tr', N'Err.LanguageLastActive',     N'En az bir dil aktif kalmalı.'),
    ('en', N'Err.EmptyFile',              N'Empty file.'),
    ('tr', N'Err.EmptyFile',              N'Boş dosya.'),
    ('en', N'Err.MissingFileName',        N'Missing file name.'),
    ('tr', N'Err.MissingFileName',        N'Dosya adı eksik.'),
    ('en', N'Err.OnlyImagesCrop',         N'Only images can be cropped.'),
    ('tr', N'Err.OnlyImagesCrop',         N'Yalnızca görseller kırpılabilir.'),
    ('en', N'Err.SourceFileNotFound',     N'Source file not found.'),
    ('tr', N'Err.SourceFileNotFound',     N'Kaynak dosya bulunamadı.'),
    ('en', N'Err.CropFailed',             N'Could not crop the image.'),
    ('tr', N'Err.CropFailed',             N'Görsel kırpılamadı.'),
    ('en', N'Err.AuthInvalid',            N'Invalid email or password.'),
    ('tr', N'Err.AuthInvalid',            N'E-posta veya parola hatalı.'),
    ('en', N'Err.SlugInUse',              N'Slug ''{0}'' is already in use.'),
    ('tr', N'Err.SlugInUse',              N'''{0}'' slug''ı zaten kullanımda.'),
    ('en', N'Err.FieldSlugInUse',         N'Slug ''{0}'' is already used in this content type.'),
    ('tr', N'Err.FieldSlugInUse',         N'''{0}'' slug''ı bu içerik tipinde zaten kullanılıyor.'),
    ('en', N'Err.EmailInUse',             N'Email ''{0}'' is already in use.'),
    ('tr', N'Err.EmailInUse',             N'''{0}'' e-postası zaten kullanımda.'),
    ('en', N'Err.DictionaryKeyInUse',     N'Key ''{0}'' is already in use.'),
    ('tr', N'Err.DictionaryKeyInUse',     N'''{0}'' anahtarı zaten kullanımda.'),
    ('en', N'Err.LanguageCodeExists',     N'Language code ''{0}'' already exists.'),
    ('tr', N'Err.LanguageCodeExists',     N'''{0}'' dil kodu zaten var.'),
    ('en', N'Err.FileExceedsLimit',       N'File exceeds the {0} MB limit.'),
    ('tr', N'Err.FileExceedsLimit',       N'Dosya {0} MB sınırını aşıyor.'),
    ('en', N'Err.FieldRequired',          N'''{0}'' is required.'),
    ('tr', N'Err.FieldRequired',          N'''{0}'' zorunlu.'),
    ('en', N'Err.RelationMin',            N'''{0}'' requires at least {1} item(s).'),
    ('tr', N'Err.RelationMin',            N'''{0}'' en az {1} öğe gerektirir.'),
    ('en', N'Err.RelationMax',            N'''{0}'' allows at most {1} item(s).'),
    ('tr', N'Err.RelationMax',            N'''{0}'' en fazla {1} öğeye izin verir.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 019: Icon picker search label + updated icon hint
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en', N'ContentType.IconSearch', N'Search icons…'),
    ('tr', N'ContentType.IconSearch', N'İkon ara…'),
    ('en', N'ContentType.IconHint',   N'Pick an icon for the sidebar menu.'),
    ('tr', N'ContentType.IconHint',   N'Sol menüde görünecek ikonu seçin.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 020: Nav.Dashboard TR rename to "Pano"
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'Nav.Dashboard', N'Pano')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 021: Field-options modal UI labels + validation error messages
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'Field.Customize',              N'Özelleştir'),
    ('en', N'Field.Customize',              N'Customize'),
    ('tr', N'Field.Opt.Modal.Title',        N'Alan Seçeneklerini Özelleştir'),
    ('en', N'Field.Opt.Modal.Title',        N'Customize Field Options'),
    ('tr', N'Field.Opt.Apply',              N'Uygula'),
    ('en', N'Field.Opt.Apply',              N'Apply'),
    ('tr', N'Field.Opt.MaxLength',          N'Maksimum karakter'),
    ('en', N'Field.Opt.MaxLength',          N'Maximum length'),
    ('tr', N'Field.Opt.Placeholder',        N'Placeholder metin'),
    ('en', N'Field.Opt.Placeholder',        N'Placeholder text'),
    ('tr', N'Field.Opt.Min',                N'Minimum'),
    ('en', N'Field.Opt.Min',                N'Minimum'),
    ('tr', N'Field.Opt.Max',                N'Maksimum'),
    ('en', N'Field.Opt.Max',                N'Maximum'),
    ('tr', N'Field.Opt.Decimals',           N'Ondalık basamak'),
    ('en', N'Field.Opt.Decimals',           N'Decimal places'),
    ('tr', N'Field.Opt.RatingMax',          N'Ölçek (5 veya 10)'),
    ('en', N'Field.Opt.RatingMax',          N'Scale (5 or 10)'),
    ('tr', N'Field.Opt.Choices',            N'Seçenekler'),
    ('en', N'Field.Opt.Choices',            N'Choices'),
    ('tr', N'Field.Opt.AddChoice',          N'Seçenek ekle ve Enter''a bas'),
    ('en', N'Field.Opt.AddChoice',          N'Type a choice and press Enter'),
    ('tr', N'Field.Opt.ChoicesHint',        N'Her seçeneği ekledikten sonra Enter''a basın.'),
    ('en', N'Field.Opt.ChoicesHint',        N'Press Enter after each choice.'),
    ('tr', N'Field.Opt.MaxSizeMb',          N'Maksimum dosya boyutu'),
    ('en', N'Field.Opt.MaxSizeMb',          N'Maximum file size'),
    ('tr', N'Field.Opt.AllowedFormats',     N'İzin verilen formatlar'),
    ('en', N'Field.Opt.AllowedFormats',     N'Allowed formats'),
    ('tr', N'Field.Opt.AllowedFormatsHint', N'Virgülle ayırın: jpg, png, webp'),
    ('en', N'Field.Opt.AllowedFormatsHint', N'Comma-separated: jpg, png, webp'),
    ('tr', N'Field.Opt.Language',           N'Programlama dili'),
    ('en', N'Field.Opt.Language',           N'Programming language'),
    ('tr', N'Field.Opt.TargetType',         N'Hedef içerik tipi'),
    ('en', N'Field.Opt.TargetType',         N'Target content type'),
    ('tr', N'Field.Opt.DisplayField',       N'Etiket alanı'),
    ('en', N'Field.Opt.DisplayField',       N'Display field'),
    ('tr', N'Field.Opt.DisplayFieldDefault', N'Varsayılan (başlık/slug)'),
    ('en', N'Field.Opt.DisplayFieldDefault', N'Default (title/slug)'),
    ('tr', N'Field.Opt.MinItems',           N'Minimum seçim'),
    ('en', N'Field.Opt.MinItems',           N'Minimum items'),
    ('tr', N'Field.Opt.MaxItems',           N'Maksimum seçim'),
    ('en', N'Field.Opt.MaxItems',           N'Maximum items'),
    ('tr', N'Field.Opt.None',               N'Bu alan tipi için yapılandırılabilir seçenek bulunmuyor.'),
    ('en', N'Field.Opt.None',               N'No configurable options for this field type.'),
    ('tr', N'Err.Field.TooLong',            N'''{0}'' alanı en fazla {1} karakter olabilir.'),
    ('en', N'Err.Field.TooLong',            N'Field ''{0}'' must be at most {1} characters.'),
    ('tr', N'Err.Field.OutOfRange',         N'''{0}'' alanı {1} ile {2} arasında olmalıdır.'),
    ('en', N'Err.Field.OutOfRange',         N'Field ''{0}'' must be between {1} and {2}.'),
    ('tr', N'Err.Field.TooManyDecimals',    N'''{0}'' alanı en fazla {1} ondalık basamak içerebilir.'),
    ('en', N'Err.Field.TooManyDecimals',    N'Field ''{0}'' may have at most {1} decimal places.'),
    ('tr', N'Err.Field.InvalidChoice',      N'''{0}'' alanı için geçersiz seçenek.'),
    ('en', N'Err.Field.InvalidChoice',      N'Invalid choice for field ''{0}''.'),
    ('tr', N'Err.Field.MediaTooLarge',      N'''{0}'' alanındaki dosya {1} MB sınırını aşıyor.'),
    ('en', N'Err.Field.MediaTooLarge',      N'File in field ''{0}'' exceeds the {1} MB limit.'),
    ('tr', N'Err.Field.MediaFormat',        N'''{0}'' alanı için izin verilen formatlar: {1}.'),
    ('en', N'Err.Field.MediaFormat',        N'Allowed formats for field ''{0}'': {1}.'),
    ('tr', N'Err.Field.RatingRange',        N'''{0}'' alanı 1 ile {1} arasında olmalıdır.'),
    ('en', N'Err.Field.RatingRange',        N'Field ''{0}'' must be between 1 and {1}.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 023: Content-item title field, grid column headers
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'ContentItem.Title',            N'Başlık'),
    ('en', N'ContentItem.Title',            N'Title'),
    ('tr', N'ContentItem.TitlePlaceholder', N'İçeriğin başlığını girin...'),
    ('en', N'ContentItem.TitlePlaceholder', N'Enter the item title...'),
    ('tr', N'Err.Title.Required',           N'Başlık zorunludur.'),
    ('en', N'Err.Title.Required',           N'Title is required.'),
    ('tr', N'ContentItem.Languages',        N'Diller'),
    ('en', N'ContentItem.Languages',        N'Languages'),
    ('tr', N'Common.Created',               N'Oluşturulma'),
    ('en', N'Common.Created',               N'Created')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 025: Language activation toggle + content-item grid filters
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'ContentItem.LangActive',       N'Bu dilde aktif'),
    ('en', N'ContentItem.LangActive',       N'Active in this language'),
    ('tr', N'ContentItem.LangActive.Hint',  N'Aktif edilmeden bu dil yayında görünmez (durum = Yayında olsa bile).'),
    ('en', N'ContentItem.LangActive.Hint',  N'Even when status is Published, this language is hidden until activated.'),
    ('tr', N'ContentItem.Filter.Search',    N'Başlık veya slug ara...'),
    ('en', N'ContentItem.Filter.Search',    N'Search title or slug...'),
    ('tr', N'ContentItem.Filter.All',       N'Tümü'),
    ('en', N'ContentItem.Filter.All',       N'All'),
    ('tr', N'ContentItem.Filter.Language',  N'Dile göre filtrele'),
    ('en', N'ContentItem.Filter.Language',  N'Filter by language'),
    ('tr', N'ContentItem.Filter.Apply',     N'Filtrele'),
    ('en', N'ContentItem.Filter.Apply',     N'Filter'),
    ('tr', N'Common.Clear',                 N'Temizle'),
    ('en', N'Common.Clear',                 N'Clear')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 026: Two-level publish model hints, language badges, missing form labels
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'ContentItem.Status.Hint',       N'Tüm diller için geçerli içerik durumu'),
    ('en', N'ContentItem.Status.Hint',       N'Content status, applies to all languages'),
    ('tr', N'ContentItem.PublishHint',       N'Yayında görünmesi için: İçerik durumu = Yayında ve ilgili dil aktif olmalı.'),
    ('en', N'ContentItem.PublishHint',       N'To go live: status must be Published and the language must be active.'),
    ('tr', N'ContentItem.LangActive.Hint',   N'Aktif edilmeden bu dil yayında görünmez (durum = Yayında olsa bile).'),
    ('en', N'ContentItem.LangActive.Hint',   N'Even when status is Published, this language is hidden until activated.'),
    ('tr', N'ContentItem.LangActive.Badge',  N'{0} · Aktif'),
    ('en', N'ContentItem.LangActive.Badge',  N'{0} · Active'),
    ('tr', N'ContentItem.LangInactive.Badge', N'{0} · Pasif'),
    ('en', N'ContentItem.LangInactive.Badge', N'{0} · Inactive'),
    ('tr', N'ContentType.Description',       N'Açıklama'),
    ('en', N'ContentType.Description',       N'Description'),
    ('tr', N'ContentType.Icon',              N'İkon'),
    ('en', N'ContentType.Icon',              N'Icon'),
    ('tr', N'ContentType.IsPublished',       N'Menüde göster'),
    ('en', N'ContentType.IsPublished',       N'Show in menu'),
    ('tr', N'Field.Type',                    N'Alan Tipi'),
    ('en', N'Field.Type',                    N'Field Type')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 028: API credential-centric management screen keys
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', N'Api.ContentTypesTab',        N'İçerik Tipleri'),
    ('en', N'Api.ContentTypesTab',        N'Content Types'),
    ('tr', N'Api.CredentialsTab',         N'Kimlik Bilgileri'),
    ('en', N'Api.CredentialsTab',         N'Credentials'),
    ('tr', N'Api.Credentials',            N'Kimlik Bilgileri'),
    ('en', N'Api.Credentials',            N'Credentials'),
    ('tr', N'Api.NewCredential',          N'Yeni Kimlik Bilgisi'),
    ('en', N'Api.NewCredential',          N'New Credential'),
    ('tr', N'Api.NoCredentials',          N'Henüz kimlik bilgisi oluşturulmadı.'),
    ('en', N'Api.NoCredentials',          N'No credentials have been created yet.'),
    ('tr', N'Api.GrantedContentTypes',    N'Kapsanan İçerik Tipleri'),
    ('en', N'Api.GrantedContentTypes',    N'Covered Content Types'),
    ('tr', N'Api.GrantedCount',           N'{0} içerik tipi'),
    ('en', N'Api.GrantedCount',           N'{0} content type(s)'),
    ('tr', N'Api.CredentialName',         N'Kimlik Bilgisi Adı'),
    ('en', N'Api.CredentialName',         N'Credential Name'),
    ('tr', N'Api.CredentialNameHint',     N'Bu kimlik bilgisini tanımlayan açıklayıcı bir ad (örn. "Mobil Uygulama").'),
    ('en', N'Api.CredentialNameHint',     N'A descriptive name for this credential (e.g. "Mobile App").'),
    ('tr', N'Api.AuthType',               N'Kimlik Doğrulama Türü'),
    ('en', N'Api.AuthType',               N'Authentication Type'),
    ('tr', N'Api.SelectContentTypes',     N'Kapsanan İçerik Tipleri'),
    ('en', N'Api.SelectContentTypes',     N'Covered Content Types'),
    ('tr', N'Api.SelectContentTypesHint', N'Bu kimlik bilgisinin erişim sağlayacağı içerik tiplerini seçin.'),
    ('en', N'Api.SelectContentTypesHint', N'Select the content types this credential will grant access to.'),
    ('tr', N'Api.SelectAll',              N'Tümünü Seç'),
    ('en', N'Api.SelectAll',              N'Select All'),
    ('tr', N'Api.KeyShownOnce',           N'Bu anahtar yalnızca bir kez gösterilir. Güvenli bir yere kopyalayın.'),
    ('en', N'Api.KeyShownOnce',           N'This key is shown only once. Copy it to a safe place.'),
    ('tr', N'Api.TokenShownOnce',         N'Bu token yalnızca bir kez gösterilir. Güvenli bir yere kopyalayın.'),
    ('en', N'Api.TokenShownOnce',         N'This token is shown only once. Copy it to a safe place.'),
    ('tr', N'Api.RotateApiKey',           N'Anahtarı Yenile'),
    ('en', N'Api.RotateApiKey',           N'Rotate Key'),
    ('tr', N'Api.GenerateApiKey',         N'Anahtar Oluştur'),
    ('en', N'Api.GenerateApiKey',         N'Generate Key'),
    ('tr', N'Api.GenerateToken',          N'Token Oluştur'),
    ('en', N'Api.GenerateToken',          N'Generate Token'),
    ('tr', N'Api.DeleteCredential',       N'Kimlik Bilgisini Sil'),
    ('en', N'Api.DeleteCredential',       N'Delete Credential'),
    ('tr', N'Api.JwtStatelessNote',       N'JWT tokenlar stateless''tir. İçerik tipi kaldırılsa dahi mevcut tokenlar süresi dolana kadar geçerli kalır.'),
    ('en', N'Api.JwtStatelessNote',       N'JWT tokens are stateless. Existing tokens remain valid until expiry even if a content type is removed.'),
    ('tr', N'Api.Access',                 N'Erişim'),
    ('en', N'Api.Access',                 N'Access'),
    ('tr', N'Api.Public',                 N'Herkese Açık'),
    ('en', N'Api.Public',                 N'Public'),
    ('tr', N'Api.Protected',              N'Korumalı'),
    ('en', N'Api.Protected',              N'Protected'),
    ('tr', N'Api.PublicHint',             N'Açık: herhangi bir kimlik doğrulama gerekmez. Kapalı: bu içerik tipini kapsayan geçerli bir kimlik bilgisi (ApiKey veya JWT) gerekir.'),
    ('en', N'Api.PublicHint',             N'On: no authentication required. Off: a valid credential (ApiKey or JWT) covering this content type is required.'),
    ('tr', N'Api.IsPublicLabel',          N'Herkese açık (auth yok)'),
    ('en', N'Api.IsPublicLabel',          N'Public (no auth required)'),
    ('tr', N'Api.CurlPreviewTitle',       N'Örnek İstek'),
    ('en', N'Api.CurlPreviewTitle',       N'Example Request'),
    ('tr', N'Api.CurlPublicNote',         N'Bu içerik tipi herkese açık — kimlik doğrulaması gerekmez.'),
    ('en', N'Api.CurlPublicNote',         N'This content type is public — no authentication required.'),
    ('tr', N'Api.CurlProtectedNote',      N'Bu içerik tipi korumalı. Kimlik bilgisi sekmesinden anahtar veya token oluşturun.'),
    ('en', N'Api.CurlProtectedNote',      N'This content type is protected. Generate a key or token from the Credentials tab.'),
    ('tr', N'Err.CredentialNameRequired', N'Kimlik bilgisi adı zorunludur.'),
    ('en', N'Err.CredentialNameRequired', N'Credential name is required.'),
    ('tr', N'Err.CredentialAuthTypeRequired', N'Kimlik doğrulama türü seçilmelidir (ApiKey veya JWT).'),
    ('en', N'Err.CredentialAuthTypeRequired', N'Authentication type must be selected (ApiKey or JWT).'),
    ('tr', N'Err.CredentialNoContentTypes', N'En az bir içerik tipi seçilmelidir.'),
    ('en', N'Err.CredentialNoContentTypes', N'At least one content type must be selected.'),
    ('tr', N'Err.CredentialNotFound',     N'Kimlik bilgisi bulunamadı.'),
    ('en', N'Err.CredentialNotFound',     N'Credential not found.'),
    ('tr', N'Err.CredentialSaveFirst',    N'Önce kimlik bilgisini kaydedin.'),
    ('en', N'Err.CredentialSaveFirst',    N'Save the credential first.'),
    ('tr', N'Err.CredentialApiKeyOnly',   N'Anahtar yalnızca ApiKey türündeki kimlik bilgileri için oluşturulabilir.'),
    ('en', N'Err.CredentialApiKeyOnly',   N'A key can only be generated for ApiKey credentials.'),
    ('tr', N'Err.CredentialJwtOnly',      N'Token yalnızca JWT türündeki kimlik bilgileri için oluşturulabilir.'),
    ('en', N'Err.CredentialJwtOnly',      N'A token can only be generated for JWT credentials.')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 029: ApiCredential CRUD view strings
-- ============================================================
DECLARE @rows029 TABLE (culture NVARCHAR(10), resource_key NVARCHAR(300), value NVARCHAR(MAX));
INSERT INTO @rows029 VALUES
('tr', 'Api.EditCredential',              N'{0} — Kimlik Bilgisi Düzenle'),
('en', 'Api.EditCredential',              N'Edit Credential — {0}'),
('tr', 'Api.CreateCredential',            N'Kimlik Bilgisi Oluştur'),
('en', 'Api.CreateCredential',            N'Create Credential'),
('tr', 'Api.CredentialsEmptyTitle',       N'Henüz kimlik bilgisi oluşturulmadı'),
('en', 'Api.CredentialsEmptyTitle',       N'No credentials yet'),
('tr', 'Api.CredentialsEmptyHint',        N'API anahtarı veya JWT Bearer token oluşturmak için "Yeni Kimlik Bilgisi" butonuna tıklayın.'),
('en', 'Api.CredentialsEmptyHint',        N'Click "New Credential" to create an API key or JWT Bearer token.'),
('tr', 'Api.ContentTypes',                N'içerik tipi'),
('en', 'Api.ContentTypes',                N'content type(s)'),
('tr', 'Api.AuthTypeLabel',               N'Kimlik Doğrulama Türü'),
('en', 'Api.AuthTypeLabel',               N'Auth Type'),
('tr', 'Api.DeleteCredentialConfirm',     N'Bu kimlik bilgisini silmek istediğinizden emin misiniz? Bu anahtarı kullanan tüm entegrasyonlar başarısız olacaktır.'),
('en', 'Api.DeleteCredentialConfirm',     N'Are you sure you want to delete this credential? All integrations using this key will stop working.'),
('tr', 'Api.CredentialDetails',           N'Kimlik Bilgisi Detayları'),
('en', 'Api.CredentialDetails',           N'Credential Details'),
('tr', 'Api.CredentialNamePlaceholder',   N'örn. Mobil Uygulama, Web Sitesi'),
('en', 'Api.CredentialNamePlaceholder',   N'e.g. Mobile App, Website'),
('tr', 'Api.AuthTypeHint',                N'API Key: başlık X-API-Key ile gönderilir. JWT: Authorization: Bearer token ile kullanılır. Oluşturulduktan sonra değiştirilemez.'),
('en', 'Api.AuthTypeHint',                N'API Key: sent via X-API-Key header. JWT: used with Authorization: Bearer token. Cannot be changed after creation.'),
('tr', 'Api.AuthTypeImmutable',           N'(değiştirilemez)'),
('en', 'Api.AuthTypeImmutable',           N'(immutable)'),
('tr', 'Api.NoContentTypesAvailable',     N'Henüz yayınlanmış içerik tipi bulunmuyor. Önce bir içerik tipi ekleyin.'),
('en', 'Api.NoContentTypesAvailable',     N'No published content types available yet. Create a content type first.'),
('tr', 'Api.CredentialInfoTitle',         N'Nasıl çalışır?'),
('en', 'Api.CredentialInfoTitle',         N'How it works'),
('tr', 'Api.CredentialInfoBody',          N'Bir kimlik bilgisi oluşturup kapsamak istediğiniz içerik tiplerini seçin. Aynı anahtar seçilen tüm içerik tiplerine erişim sağlar. İçerik tipi ayarlarından "Herkese açık" seçeneğini kapatmayı unutmayın.'),
('en', 'Api.CredentialInfoBody',          N'Create a credential and select which content types it covers. The same key grants access to all selected types. Remember to uncheck "Public" on each content type settings.'),
('tr', 'Api.CredentialCoversAllGranted',  N'Bu kimlik bilgisi, seçilen tüm içerik tiplerine erişim sağlar.'),
('en', 'Api.CredentialCoversAllGranted',  N'This credential grants access to all selected content types.'),
('tr', 'Api.DangerZone',                  N'Tehlikeli Bölge'),
('en', 'Api.DangerZone',                  N'Danger Zone'),
('tr', 'Api.DeleteCredentialHint',        N'Bu kimlik bilgisini silmek geri alınamaz. Bunu kullanan tüm entegrasyonlar başarısız olacaktır.'),
('en', 'Api.DeleteCredentialHint',        N'Deleting this credential is irreversible. All integrations using it will stop working.'),
('tr', 'Common.SavedSuccessfully',        N'Değişiklikler kaydedildi.'),
('en', 'Common.SavedSuccessfully',        N'Changes saved successfully.');

MERGE ui_translations AS target
USING @rows029 AS source
    ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 032: Dashboard stats, Audit log screen, action display names
-- ============================================================
DECLARE @rows032 TABLE (culture NVARCHAR(10), resource_key NVARCHAR(300), value NVARCHAR(MAX));
INSERT INTO @rows032 VALUES
('tr', 'Dashboard.Title',                N'Pano'),
('en', 'Dashboard.Title',                N'Dashboard'),
('tr', 'Dashboard.ContentTypes',         N'İçerik Tipi'),
('en', 'Dashboard.ContentTypes',         N'Content Types'),
('tr', 'Dashboard.ContentItems',         N'İçerik Öğesi'),
('en', 'Dashboard.ContentItems',         N'Content Items'),
('tr', 'Dashboard.MediaAssets',          N'Medya'),
('en', 'Dashboard.MediaAssets',          N'Media Assets'),
('tr', 'Dashboard.Users',                N'Kullanıcı'),
('en', 'Dashboard.Users',                N'Users'),
('tr', 'Dashboard.Languages',            N'Dil'),
('en', 'Dashboard.Languages',            N'Languages'),
('tr', 'Dashboard.RecentActivity',       N'Son İşlemler'),
('en', 'Dashboard.RecentActivity',       N'Recent Activity'),
('tr', 'Dashboard.NoActivity',           N'Henüz kayıtlı işlem yok.'),
('en', 'Dashboard.NoActivity',           N'No activity recorded yet.'),
('tr', 'Log.Title',                      N'İşlem Günlüğü'),
('en', 'Log.Title',                      N'Audit Log'),
('tr', 'Log.FilterAction',               N'İşlem'),
('en', 'Log.FilterAction',               N'Action'),
('tr', 'Log.FilterUser',                 N'Kullanıcı'),
('en', 'Log.FilterUser',                 N'User'),
('tr', 'Log.FilterContentType',          N'İçerik Tipi'),
('en', 'Log.FilterContentType',          N'Content Type'),
('tr', 'Log.FilterDateFrom',             N'Başlangıç Tarihi'),
('en', 'Log.FilterDateFrom',             N'Date From'),
('tr', 'Log.FilterDateTo',               N'Bitiş Tarihi'),
('en', 'Log.FilterDateTo',               N'Date To'),
('tr', 'Log.ColAction',                  N'İşlem'),
('en', 'Log.ColAction',                  N'Action'),
('tr', 'Log.ColUser',                    N'Kullanıcı'),
('en', 'Log.ColUser',                    N'User'),
('tr', 'Log.ColEntity',                  N'Varlık'),
('en', 'Log.ColEntity',                  N'Entity'),
('tr', 'Log.ColDate',                    N'Tarih'),
('en', 'Log.ColDate',                    N'Date'),
('tr', 'Log.Empty',                      N'Henüz kayıtlı işlem bulunamadı.'),
('en', 'Log.Empty',                      N'No log entries found.'),
('tr', 'Log.ContentTypeHistory',         N'İşlem Geçmişi'),
('en', 'Log.ContentTypeHistory',         N'Activity History'),
('tr', 'Log.ContentTypeHistoryEmpty',    N'Bu içerik tipi için henüz işlem geçmişi yok.'),
('en', 'Log.ContentTypeHistoryEmpty',    N'No activity yet for this content type.'),
('tr', 'Action.Auth.LoginSuccess',       N'Giriş yapıldı'),
('en', 'Action.Auth.LoginSuccess',       N'Logged in'),
('tr', 'Action.Auth.LoginFailed',        N'Giriş başarısız'),
('en', 'Action.Auth.LoginFailed',        N'Login failed'),
('tr', 'Action.Auth.Logout',             N'Çıkış yapıldı'),
('en', 'Action.Auth.Logout',             N'Logged out'),
('tr', 'Action.Auth.PasswordChanged',    N'Şifre değiştirildi'),
('en', 'Action.Auth.PasswordChanged',    N'Password changed'),
('tr', 'Action.ContentItem.Created',     N'İçerik oluşturuldu'),
('en', 'Action.ContentItem.Created',     N'Content created'),
('tr', 'Action.ContentItem.Updated',     N'İçerik güncellendi'),
('en', 'Action.ContentItem.Updated',     N'Content updated'),
('tr', 'Action.ContentItem.Deleted',     N'İçerik silindi'),
('en', 'Action.ContentItem.Deleted',     N'Content deleted'),
('tr', 'Action.ContentType.Created',     N'İçerik tipi oluşturuldu'),
('en', 'Action.ContentType.Created',     N'Content type created'),
('tr', 'Action.ContentType.Updated',     N'İçerik tipi güncellendi'),
('en', 'Action.ContentType.Updated',     N'Content type updated'),
('tr', 'Action.ContentType.Deleted',     N'İçerik tipi silindi'),
('en', 'Action.ContentType.Deleted',     N'Content type deleted'),
('tr', 'Action.Media.Uploaded',          N'Medya yüklendi'),
('en', 'Action.Media.Uploaded',          N'Media uploaded'),
('tr', 'Action.Media.Deleted',           N'Medya silindi'),
('en', 'Action.Media.Deleted',           N'Media deleted'),
('tr', 'Action.Media.Cropped',           N'Medya kırpıldı'),
('en', 'Action.Media.Cropped',           N'Media cropped'),
('tr', 'Action.User.Created',            N'Kullanıcı oluşturuldu'),
('en', 'Action.User.Created',            N'User created'),
('tr', 'Action.User.Updated',            N'Kullanıcı güncellendi'),
('en', 'Action.User.Updated',            N'User updated'),
('tr', 'Action.User.Deleted',            N'Kullanıcı silindi'),
('en', 'Action.User.Deleted',            N'User deleted'),
('tr', 'Action.User.PermissionsUpdated', N'Kullanıcı izinleri güncellendi'),
('en', 'Action.User.PermissionsUpdated', N'User permissions updated'),
('tr', 'Action.Dictionary.Created',      N'Sözlük girişi oluşturuldu'),
('en', 'Action.Dictionary.Created',      N'Dictionary entry created'),
('tr', 'Action.Dictionary.Updated',      N'Sözlük girişi güncellendi'),
('en', 'Action.Dictionary.Updated',      N'Dictionary entry updated'),
('tr', 'Action.Dictionary.Deleted',      N'Sözlük girişi silindi'),
('en', 'Action.Dictionary.Deleted',      N'Dictionary entry deleted'),
('tr', 'Action.ApiCredential.Created',   N'API kimlik bilgisi oluşturuldu'),
('en', 'Action.ApiCredential.Created',   N'API credential created'),
('tr', 'Action.ApiCredential.Rotated',   N'API anahtarı yenilendi'),
('en', 'Action.ApiCredential.Rotated',   N'API key rotated'),
('tr', 'Action.ApiCredential.Deleted',   N'API kimlik bilgisi silindi'),
('en', 'Action.ApiCredential.Deleted',   N'API credential deleted'),
('tr', 'Action.System.TenantCreated',    N'Tenant oluşturuldu'),
('en', 'Action.System.TenantCreated',    N'Tenant created'),
('tr', 'Action.System.TenantUpdated',    N'Tenant güncellendi'),
('en', 'Action.System.TenantUpdated',    N'Tenant updated'),
('tr', 'Action.System.TenantDeleted',    N'Tenant silindi'),
('en', 'Action.System.TenantDeleted',    N'Tenant deleted'),
('tr', 'Action.System.ImpersonationStarted', N'Tenant taklit edildi'),
('en', 'Action.System.ImpersonationStarted', N'Impersonation started'),
('tr', 'Action.System.ImpersonationExited',  N'Taklit sona erdi'),
('en', 'Action.System.ImpersonationExited',  N'Impersonation exited'),
('tr', 'Nav.Logs',                       N'İşlem Günlüğü'),
('en', 'Nav.Logs',                       N'Audit Log'),
('tr', 'Log.ViewAll',                    N'Tümünü gör'),
('en', 'Log.ViewAll',                    N'View all');

MERGE ui_translations AS target
USING @rows032 AS source
    ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 033: Media library file management keys
-- ============================================================
DECLARE @rows033 TABLE (culture NVARCHAR(10), resource_key NVARCHAR(300), value NVARCHAR(MAX));
INSERT INTO @rows033 VALUES
('tr', 'Media.Filter.All',        N'Tümü'),
('en', 'Media.Filter.All',        N'All'),
('tr', 'Media.Filter.Image',      N'Görseller'),
('en', 'Media.Filter.Image',      N'Images'),
('tr', 'Media.Filter.Video',      N'Videolar'),
('en', 'Media.Filter.Video',      N'Videos'),
('tr', 'Media.Filter.Audio',      N'Ses Dosyaları'),
('en', 'Media.Filter.Audio',      N'Audio'),
('tr', 'Media.Filter.File',       N'Dosyalar'),
('en', 'Media.Filter.File',       N'Files'),
('tr', 'Media.Files',             N'dosya'),
('en', 'Media.Files',             N'files'),
('tr', 'Media.Rename',            N'Yeniden Adlandır'),
('en', 'Media.Rename',            N'Rename'),
('tr', 'Media.DisplayName',       N'Görünen Ad'),
('en', 'Media.DisplayName',       N'Display Name'),
('tr', 'Media.AltText',           N'Alt Metin'),
('en', 'Media.AltText',           N'Alt Text'),
('tr', 'Media.FileType',          N'Dosya Türü'),
('en', 'Media.FileType',          N'File Type'),
('tr', 'Media.FileSize',          N'Dosya Boyutu'),
('en', 'Media.FileSize',          N'File Size'),
('tr', 'Media.Dimensions',        N'Boyutlar'),
('en', 'Media.Dimensions',        N'Dimensions'),
('tr', 'Media.UploadedAt',        N'Yükleme Tarihi'),
('en', 'Media.UploadedAt',        N'Upload Date'),
('tr', 'Action.Media.Renamed',    N'Medya dosyası yeniden adlandırıldı'),
('en', 'Action.Media.Renamed',    N'Media file renamed'),
('tr', 'Err.FileTypeNotAllowed',  N'Bu dosya türü kabul edilmiyor. Desteklenen türler: görsel, video, ses, PDF, Office belgeleri ve metin dosyaları.'),
('en', 'Err.FileTypeNotAllowed',  N'File type not allowed. Supported: images, video, audio, PDF, Office documents, and text files.'),
('tr', 'Err.InvalidBaseName',     N'Geçersiz dosya adı.'),
('en', 'Err.InvalidBaseName',     N'Invalid file name.'),
('tr', 'Err.UploadFailed',        N'Yükleme başarısız.'),
('en', 'Err.UploadFailed',        N'Upload failed.'),
('tr', 'Err.ConnectionError',     N'Bağlantı hatası.'),
('en', 'Err.ConnectionError',     N'Connection error.');

MERGE ui_translations AS target
USING @rows033 AS source
    ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 035: API CRUD permissions and model preview keys
-- ============================================================
DECLARE @rows035 TABLE (culture NVARCHAR(10), resource_key NVARCHAR(300), value NVARCHAR(MAX));
INSERT INTO @rows035 VALUES
('tr', 'Api.Permissions',       N'İşlem Yetkileri'),
('en', 'Api.Permissions',       N'Operation Permissions'),
('tr', 'Api.AllowRead',         N'Okuma (GET)'),
('en', 'Api.AllowRead',         N'Read (GET)'),
('tr', 'Api.AllowCreate',       N'Ekleme (POST)'),
('en', 'Api.AllowCreate',       N'Create (POST)'),
('tr', 'Api.AllowUpdate',       N'Güncelleme (PUT)'),
('en', 'Api.AllowUpdate',       N'Update (PUT)'),
('tr', 'Api.AllowDelete',       N'Silme (DELETE)'),
('en', 'Api.AllowDelete',       N'Delete (DELETE)'),
('tr', 'Api.WriteAuthHint',     N'Ekleme, güncelleme ve silme işlemleri her zaman geçerli bir kimlik bilgisi (ApiKey veya JWT) gerektirir; içerik "Herkese açık" olsa bile.'),
('en', 'Api.WriteAuthHint',     N'Create, update, and delete operations always require a valid credential (ApiKey or JWT), even when the content type is public.'),
('tr', 'Api.ModelPreview',      N'Model Önizleme'),
('en', 'Api.ModelPreview',      N'Model Preview'),
('tr', 'Api.RequestModel',      N'İstek Gövdesi'),
('en', 'Api.RequestModel',      N'Request Body'),
('tr', 'Api.ResponseModel',     N'Yanıt Örneği'),
('en', 'Api.ResponseModel',     N'Response Example'),
('tr', 'Api.CopyExample',       N'Kopyala'),
('en', 'Api.CopyExample',       N'Copy');

MERGE ui_translations AS target
USING @rows035 AS source
    ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- 037: Email verification (two-factor login) keys
-- ============================================================
DECLARE @rows037 TABLE (culture NVARCHAR(10), resource_key NVARCHAR(300), value NVARCHAR(MAX));
INSERT INTO @rows037 VALUES
('tr', 'Login.VerifyCode.Title',       N'Doğrulama Kodu'),
('en', 'Login.VerifyCode.Title',       N'Verification Code'),
('tr', 'Login.VerifyCode.Description', N'E-posta adresinize 6 haneli bir kod gönderdik. Lütfen aşağıya girin.'),
('en', 'Login.VerifyCode.Description', N'We sent a 6-digit code to your email address. Please enter it below.'),
('tr', 'Login.VerifyCode.CodeLabel',   N'Doğrulama Kodu'),
('en', 'Login.VerifyCode.CodeLabel',   N'Verification Code'),
('tr', 'Login.VerifyCode.Submit',      N'Doğrula'),
('en', 'Login.VerifyCode.Submit',      N'Verify'),
('tr', 'Login.VerifyCode.BackToLogin', N'Giriş sayfasına dön'),
('en', 'Login.VerifyCode.BackToLogin', N'Back to login'),
('tr', 'Login.VerifyCode.ExpiryHint',  N'Kod {0} dakika içinde geçerlidir.'),
('en', 'Login.VerifyCode.ExpiryHint',  N'The code is valid for {0} minutes.'),
('tr', 'Login.CodeSent',               N'{0} adresine doğrulama kodu gönderildi.'),
('en', 'Login.CodeSent',               N'A verification code was sent to {0}.'),
('tr', 'Login.EmailSendError',         N'Doğrulama kodu gönderilemedi. Lütfen tekrar deneyin.'),
('en', 'Login.EmailSendError',         N'Failed to send verification code. Please try again.'),
('tr', 'Login.ExpiredCode',            N'Doğrulama kodu süresi doldu. Lütfen tekrar giriş yapın.'),
('en', 'Login.ExpiredCode',            N'Verification code has expired. Please log in again.'),
('tr', 'Login.MaxAttemptsExceeded',    N'Çok fazla yanlış deneme. Lütfen tekrar giriş yapın.'),
('en', 'Login.MaxAttemptsExceeded',    N'Too many failed attempts. Please log in again.'),
('tr', 'Login.InvalidCode',            N'Geçersiz kod. Kalan deneme: {0}.'),
('en', 'Login.InvalidCode',            N'Invalid code. Attempts remaining: {0}.'),
('tr', 'Login.CodeNotFound',           N'Doğrulama kodu bulunamadı. Lütfen tekrar giriş yapın.'),
('en', 'Login.CodeNotFound',           N'Verification code not found. Please log in again.');

MERGE ui_translations AS target
USING @rows037 AS source
    ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET target.value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- Previously 003_missing_ui_translations.sql
--   Common.Reset, Common.Results, Log.FilterEntity
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('en', 'Common.Reset',      N'Reset'),
    ('tr', 'Common.Reset',      N'Sıfırla'),
    ('en', 'Common.Results',    N'results'),
    ('tr', 'Common.Results',    N'sonuç'),
    ('en', 'Log.FilterEntity',  N'Entity type'),
    ('tr', 'Log.FilterEntity',  N'Varlık tipi')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

-- ============================================================
-- Previously 004_audit_action_translations.sql
--   Human-readable labels for all audit action codes (tr + en)
-- ============================================================

MERGE ui_translations AS target
USING (VALUES
    ('tr', 'AuditAction.Auth.LoginSuccess',          N'Giriş Başarılı'),
    ('en', 'AuditAction.Auth.LoginSuccess',          N'Login Successful'),
    ('tr', 'AuditAction.Auth.LoginFailed',           N'Giriş Başarısız'),
    ('en', 'AuditAction.Auth.LoginFailed',           N'Login Failed'),
    ('tr', 'AuditAction.Auth.Logout',                N'Çıkış Yapıldı'),
    ('en', 'AuditAction.Auth.Logout',                N'Logged Out'),
    ('tr', 'AuditAction.Auth.PasswordChanged',       N'Şifre Değiştirildi'),
    ('en', 'AuditAction.Auth.PasswordChanged',       N'Password Changed'),
    ('tr', 'AuditAction.Auth.CodeSent',              N'Doğrulama Kodu Gönderildi'),
    ('en', 'AuditAction.Auth.CodeSent',              N'Verification Code Sent'),
    ('tr', 'AuditAction.Auth.CodeVerified',          N'Doğrulama Kodu Onaylandı'),
    ('en', 'AuditAction.Auth.CodeVerified',          N'Verification Code Confirmed'),
    ('tr', 'AuditAction.Auth.CodeFailed',            N'Doğrulama Kodu Başarısız'),
    ('en', 'AuditAction.Auth.CodeFailed',            N'Verification Code Failed'),
    ('tr', 'AuditAction.ContentItem.Created',        N'İçerik Oluşturuldu'),
    ('en', 'AuditAction.ContentItem.Created',        N'Content Created'),
    ('tr', 'AuditAction.ContentItem.Updated',        N'İçerik Güncellendi'),
    ('en', 'AuditAction.ContentItem.Updated',        N'Content Updated'),
    ('tr', 'AuditAction.ContentItem.Deleted',        N'İçerik Silindi'),
    ('en', 'AuditAction.ContentItem.Deleted',        N'Content Deleted'),
    ('tr', 'AuditAction.ContentType.Created',        N'İçerik Tipi Oluşturuldu'),
    ('en', 'AuditAction.ContentType.Created',        N'Content Type Created'),
    ('tr', 'AuditAction.ContentType.Updated',        N'İçerik Tipi Güncellendi'),
    ('en', 'AuditAction.ContentType.Updated',        N'Content Type Updated'),
    ('tr', 'AuditAction.ContentType.Deleted',        N'İçerik Tipi Silindi'),
    ('en', 'AuditAction.ContentType.Deleted',        N'Content Type Deleted'),
    ('tr', 'AuditAction.ContentType.PublishToggled', N'Yayın Durumu Değiştirildi'),
    ('en', 'AuditAction.ContentType.PublishToggled', N'Publish Status Toggled'),
    ('tr', 'AuditAction.ContentField.Created',       N'Alan Oluşturuldu'),
    ('en', 'AuditAction.ContentField.Created',       N'Field Created'),
    ('tr', 'AuditAction.ContentField.Updated',       N'Alan Güncellendi'),
    ('en', 'AuditAction.ContentField.Updated',       N'Field Updated'),
    ('tr', 'AuditAction.ContentField.Deleted',       N'Alan Silindi'),
    ('en', 'AuditAction.ContentField.Deleted',       N'Field Deleted'),
    ('tr', 'AuditAction.ContentField.Reordered',     N'Alanlar Yeniden Sıralandı'),
    ('en', 'AuditAction.ContentField.Reordered',     N'Fields Reordered'),
    ('tr', 'AuditAction.Media.Uploaded',             N'Medya Yüklendi'),
    ('en', 'AuditAction.Media.Uploaded',             N'Media Uploaded'),
    ('tr', 'AuditAction.Media.Renamed',              N'Medya Yeniden Adlandırıldı'),
    ('en', 'AuditAction.Media.Renamed',              N'Media Renamed'),
    ('tr', 'AuditAction.Media.Deleted',              N'Medya Silindi'),
    ('en', 'AuditAction.Media.Deleted',              N'Media Deleted'),
    ('tr', 'AuditAction.Media.Cropped',              N'Medya Kırpıldı'),
    ('en', 'AuditAction.Media.Cropped',              N'Media Cropped'),
    ('tr', 'AuditAction.User.Created',               N'Kullanıcı Oluşturuldu'),
    ('en', 'AuditAction.User.Created',               N'User Created'),
    ('tr', 'AuditAction.User.Updated',               N'Kullanıcı Güncellendi'),
    ('en', 'AuditAction.User.Updated',               N'User Updated'),
    ('tr', 'AuditAction.User.Deleted',               N'Kullanıcı Silindi'),
    ('en', 'AuditAction.User.Deleted',               N'User Deleted'),
    ('tr', 'AuditAction.User.PermissionsUpdated',    N'Kullanıcı İzinleri Güncellendi'),
    ('en', 'AuditAction.User.PermissionsUpdated',    N'User Permissions Updated'),
    ('tr', 'AuditAction.Dictionary.Created',         N'Sözlük Girdisi Oluşturuldu'),
    ('en', 'AuditAction.Dictionary.Created',         N'Dictionary Entry Created'),
    ('tr', 'AuditAction.Dictionary.Updated',         N'Sözlük Girdisi Güncellendi'),
    ('en', 'AuditAction.Dictionary.Updated',         N'Dictionary Entry Updated'),
    ('tr', 'AuditAction.Dictionary.Deleted',         N'Sözlük Girdisi Silindi'),
    ('en', 'AuditAction.Dictionary.Deleted',         N'Dictionary Entry Deleted'),
    ('tr', 'AuditAction.ApiCredential.Created',      N'API Kimliği Oluşturuldu'),
    ('en', 'AuditAction.ApiCredential.Created',      N'API Credential Created'),
    ('tr', 'AuditAction.ApiCredential.Updated',      N'API Kimliği Güncellendi'),
    ('en', 'AuditAction.ApiCredential.Updated',      N'API Credential Updated'),
    ('tr', 'AuditAction.ApiCredential.Rotated',      N'API Anahtarı Yenilendi'),
    ('en', 'AuditAction.ApiCredential.Rotated',      N'API Key Rotated'),
    ('tr', 'AuditAction.ApiCredential.Deleted',      N'API Kimliği Silindi'),
    ('en', 'AuditAction.ApiCredential.Deleted',      N'API Credential Deleted'),
    ('tr', 'AuditAction.Api.AuthFailed',             N'API Kimlik Doğrulama Hatası'),
    ('en', 'AuditAction.Api.AuthFailed',             N'API Auth Failed'),
    ('tr', 'AuditAction.Api.VerbForbidden',          N'API Erişimi Reddedildi'),
    ('en', 'AuditAction.Api.VerbForbidden',          N'API Access Denied'),
    ('tr', 'AuditAction.System.TenantCreated',       N'Tenant Oluşturuldu'),
    ('en', 'AuditAction.System.TenantCreated',       N'Tenant Created'),
    ('tr', 'AuditAction.System.TenantUpdated',       N'Tenant Güncellendi'),
    ('en', 'AuditAction.System.TenantUpdated',       N'Tenant Updated'),
    ('tr', 'AuditAction.System.TenantDeleted',       N'Tenant Silindi'),
    ('en', 'AuditAction.System.TenantDeleted',       N'Tenant Deleted'),
    ('tr', 'AuditAction.System.ImpersonationStarted',N'Kimlik Bürünme Başlatıldı'),
    ('en', 'AuditAction.System.ImpersonationStarted',N'Impersonation Started'),
    ('tr', 'AuditAction.System.ImpersonationExited', N'Kimlik Bürünmeden Çıkıldı'),
    ('en', 'AuditAction.System.ImpersonationExited', N'Impersonation Exited')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED     THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO

PRINT 'SYSTEM SEED COMPLETE.';
PRINT '  SystemAdmin : root@system.local / Admin123!';
PRINT '  UI translations: tr + en (300+ keys, including audit action labels)';
PRINT '  No tenant/content structure created — see 003_v1_demo_seed.sql for a worked example.';
GO

-- ================================================================
-- SECTION 2: notify_on_create flag + inbox UI strings
-- (data-only portion of the former 005_inbox_and_notify.sql — the
-- schema changes from that migration, i.e. content_items.is_read,
-- content_types.notify_on_create, and IX_content_items_unread, now
-- live directly in 001_v1_schema.sql)
-- ================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- contact-message içerik tiplerini "form gönderim kutusu" olarak işaretle (tüm tenant'larda)
UPDATE content_types
SET notify_on_create = 1, updated_at = GETUTCDATE()
WHERE slug = N'contact-message' AND is_deleted = 0 AND notify_on_create = 0;
PRINT '=== contact-message notify_on_create=1 isaretlendi ===';
GO

-- Gelen-kutusu (okundu/okunmadı) UI çevirileri — DB-backed admin localizer (ui_translations)
MERGE ui_translations AS target
USING (VALUES
    ('tr', N'ContentItem.Unread',             N'Okunmadı'),
    ('en', N'ContentItem.Unread',             N'Unread'),
    ('tr', N'ContentItem.MarkRead',           N'Okundu işaretle'),
    ('en', N'ContentItem.MarkRead',           N'Mark as read'),
    ('tr', N'ContentItem.MarkUnread',         N'Okunmadı işaretle'),
    ('en', N'ContentItem.MarkUnread',         N'Mark as unread'),
    ('tr', N'ContentItem.Filter.UnreadOnly',  N'Yalnızca okunmamışlar'),
    ('en', N'ContentItem.Filter.UnreadOnly',  N'Unread only')
) AS source (culture, resource_key, value)
ON target.culture = source.culture AND target.resource_key = source.resource_key
WHEN MATCHED THEN UPDATE SET value = source.value
WHEN NOT MATCHED THEN INSERT (culture, resource_key, value)
    VALUES (source.culture, source.resource_key, source.value);
GO
