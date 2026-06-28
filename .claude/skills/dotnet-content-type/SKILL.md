---
name: dotnet-content-type
description: >
  Use when working with ContentType schema builder, ContentField definitions,
  field drag-drop ordering, or the dynamic form renderer for ContentItems.
  Triggers on: "content type", "içerik yapısı", "field ekle", "field builder",
  "drag drop", "sıralama", "içerik tipi", "form renderer", "dynamic form".
---

# Varyo CMS Content Type System

## Key Files
- Schema: @docs/database-schema.md (content_types, content_fields, content_field_values)
- Field types reference: @docs/content-types.md
- Architecture: @docs/architecture.md

## ContentType Repository Queries

### GetWithFields (most used query — always join fields)
```sql
SELECT
    ct.id, ct.name, ct.slug, ct.description, ct.icon, ct.is_published, ct.sort_order,
    cf.id AS FieldId, cf.name AS FieldName, cf.slug AS FieldSlug,
    cf.field_type, cf.is_required, cf.is_localized, cf.sort_order AS FieldSortOrder,
    cf.options_json
FROM content_types ct
LEFT JOIN content_fields cf
    ON cf.content_type_id = ct.id AND cf.is_deleted = 0
WHERE ct.id = @Id AND ct.tenant_id = @TenantId AND ct.is_deleted = 0
ORDER BY cf.sort_order ASC
```

Use Dapper multi-mapping to hydrate `ContentType` with `List<ContentField>`.

### Reorder Fields (drag-drop endpoint)
```sql
-- Called in a loop within a transaction
UPDATE content_fields
SET sort_order = @SortOrder, updated_at = GETUTCDATE()
WHERE id = @FieldId AND content_type_id = @ContentTypeId AND tenant_id = @TenantId
```

Controller endpoint:
```csharp
[HttpPatch("{id}/fields/reorder")]
public async Task<IActionResult> ReorderFields(int id, [FromBody] ReorderFieldsRequest request)
// request.FieldIds: int[] — ordered list of field IDs
```

## Dynamic Form Renderer

The content item edit view (`/Views/Content/Edit.cshtml`) must:

1. Load `ContentType` with all fields sorted by `sort_order`
2. For each field, render the correct partial view: `_Field_{FieldType}.cshtml`
3. Language tabs for localized fields:
```html
<ul class="nav nav-tabs" id="langTabs">
    @foreach (var lang in Model.Languages) {
        <li class="nav-item">
            <button class="nav-link @(lang.IsDefault ? "active" : "")"
                    data-lang="@lang.Code">@lang.Name</button>
        </li>
    }
</ul>
```

4. Field partials naming: `_Field_Text.cshtml`, `_Field_RichText.cshtml`, `_Field_Image.cshtml`, etc.

## Field Partial Templates

### _Field_Text.cshtml
```html
<div class="mb-3 field-group" data-field-id="@Model.FieldId" data-lang-field="@Model.IsLocalized">
    <label class="form-label">@Model.Label @if(Model.IsRequired){<span class="text-danger">*</span>}</label>
    <input type="text" class="form-control" name="fields[@Model.Slug][@Model.LangCode]"
           value="@Model.Value" maxlength="@Model.Options?.MaxLength" @(Model.IsRequired ? "required" : "")>
</div>
```

### _Field_DateRange.cshtml
```html
<div class="mb-3 field-group" data-field-id="@Model.FieldId">
    <label class="form-label">@Model.Label</label>
    <input type="text" class="form-control flatpickr-range"
           name="fields[@Model.Slug][start]" placeholder="Start date">
    <input type="hidden" name="fields[@Model.Slug][end]">
</div>
<script>
flatpickr(".flatpickr-range", { mode: "range", dateFormat: "Y-m-d",
    onChange: function(dates) {
        if(dates.length === 2) {
            this.element.nextElementSibling.value = dates[1].toISOString().split('T')[0];
        }
    }
});
</script>
```

### _Field_Relation.cshtml
```html
<div class="mb-3 field-group" data-field-id="@Model.FieldId">
    <label class="form-label">@Model.Label</label>
    <select class="form-select relation-search" multiple="@Model.IsMulti"
            name="relations[@Model.Slug][]"
            data-target-type="@Model.Options.TargetContentTypeId"
            data-display-field="@Model.Options.DisplayFieldSlug">
        @foreach(var selected in Model.SelectedItems) {
            <option value="@selected.Id" selected>@selected.DisplayValue</option>
        }
    </select>
</div>
<!-- initialized in content-form.js with AJAX search to /admin/content-types/{id}/items/search -->
```

### _Field_Image.cshtml
```html
<div class="mb-3 field-group" data-field-id="@Model.FieldId">
    <label class="form-label">@Model.Label</label>
    <div class="media-uploader" data-field-slug="@Model.Slug" data-aspect="@Model.Options?.AspectRatio">
        @if(Model.ExistingMedia != null) {
            <img src="@Model.ExistingMedia.ThumbnailUrl" class="img-thumbnail mb-2">
        }
        <input type="hidden" name="fields[@Model.Slug]" class="media-id-input" value="@Model.ExistingMedia?.Id">
        <button type="button" class="btn btn-outline-secondary open-media-library">
            <i class="bi bi-image"></i> Görsel Seç
        </button>
    </div>
</div>
```

## Relation AJAX Search Endpoint
```csharp
// GET /admin/content-types/{targetTypeId}/items/search?q={term}&lang={code}
[HttpGet("{id}/items/search")]
public async Task<IActionResult> SearchItems(int id, string q, string lang = "tr")
{
    var results = await _contentItemService.SearchAsync(id, q, lang);
    return Json(results.Select(r => new { id = r.Id, text = r.DisplayValue }));
}
```

## Field Builder JS (key interactions)
```javascript
// Initialize SortableJS on field list
const sortable = Sortable.create(document.getElementById('field-list'), {
    handle: '.drag-handle',
    animation: 150,
    onEnd: async function (evt) {
        const fieldIds = [...document.querySelectorAll('.field-item')]
            .map(el => parseInt(el.dataset.fieldId));
        await fetch(`/admin/content-types/${contentTypeId}/fields/reorder`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken },
            body: JSON.stringify({ fieldIds })
        });
    }
});
```
