# Varyo CMS — Content Type System

## Supported Field Types

| FieldType Enum | UI Component | Storage Column | Notes |
|---|---|---|---|
| `Text` | `<input type="text">` | value_text | max_length in options |
| `RichText` | TinyMCE / Quill | value_text | HTML string |
| `Markdown` | CodeMirror + preview | value_text | Markdown string |
| `Number` | `<input type="number">` | value_number | integer |
| `Decimal` | `<input type="number" step>` | value_number | decimal |
| `Boolean` | Toggle switch | value_bool | |
| `Date` | Flatpickr date | value_date | |
| `DateTime` | Flatpickr datetime | value_date | |
| `Time` | Flatpickr time | value_date | |
| `DateRange` | Flatpickr range | value_date + value_date_end | |
| `Email` | `<input type="email">` | value_text | validated |
| `URL` | `<input type="url">` | value_text | validated |
| `Phone` | `<input type="tel">` | value_text | |
| `Color` | Color picker input | value_text | hex string |
| `JSON` | Monaco editor (JSON mode) | value_text | validated JSON |
| `CodeSnippet` | Monaco editor | value_text | options: language |
| `Image` | Searchable media picker (+ Cropper.js crop in library) | value_media_id | |
| `Video` | Searchable media picker | value_media_id | |
| `Audio` | Searchable media picker | value_media_id | |
| `File` | Searchable media picker | value_media_id | |
| `Gallery` | Multi-select media picker | value_text (JSON array of media ids) | single row; `content_field_values` UNIQUE blocks multiple rows |
| `Select` | `<select>` dropdown | value_text | choices in options_json |
| `MultiSelect` | Checkbox group / tags | value_text (comma-sep) | |
| `Tags` | Tag input (free-form) | value_text (JSON array) | |
| `Relation` | Searchable dropdown | — | content_field_relations table |
| `MultiRelation` | Multi-select search | — | content_field_relations table |
| `Slug` | Auto-generated + editable | value_text | |
| `Password` | `<input type="password">` | value_text (BCrypt) | |
| `GeoLocation` | Map picker (Leaflet) | value_text (JSON: lat,lng) | |
| `Rating` | Star widget (1–5 or 1–10) | value_number | |

---

## Relation Field Config (options_json)

When a ContentField has type `Relation` or `MultiRelation`, options_json stores:

```json
{
  "target_content_type_id": 3,
  "display_field_slug": "title",
  "value_field_slug": "id",
  "allow_multiple": true,
  "min_items": 0,
  "max_items": 10
}
```

- `target_content_type_id`: which content type to search in
- `display_field_slug`: which field to show in the dropdown (e.g. "name", "title")
- `value_field_slug`: which field is the stored value (always "id")
- `min_items` / `max_items`: count constraints (only meaningful for `MultiRelation`). Enforced **server-side**
  in `ContentItemService` (min when the field has input or is required; max always) and reflected in the
  picker (it blocks adding past `max` and shows a min/max hint). Parsed by `RelationOptions`.

UI renders an AJAX-powered searchable picker (`relation-picker.js`) that queries:
`GET /admin/content-types/{target_id}/items/search?q={term}&displayField={slug}&lang={code}`
The `lang` is the form's active language, so result labels resolve in the language being edited.

---

## Media Field Behavior (as implemented)

Uploading (Media library, `/admin/media`):
1. File POSTs to `POST /admin/media/upload`
2. Server validates type + size, saves to `/wwwroot/uploads/{tenant_id}/` (GUID file name)
3. Returns `MediaAssetDto` (`id`, `url`, `mediaType`, ...); `media_assets` row created

Selecting in a content form — all media fields share one **searchable picker** (`media-picker.js`):
- It queries `GET /admin/media/search?q=&type=` and lists results with thumbnails.
- `Image`/`Video`/`Audio`/`File` = single select → stored as `value_media_id`.
- `Gallery` = multi-select → stored as an ordered JSON array of media ids in `value_text`
  (the `content_field_values` UNIQUE(item,field,lang) constraint rules out multiple rows).
- The picker filters by media type (Image/Gallery → image, Video → video, Audio → audio, File → any).

Cropping: in the Media library each image has a **Crop** button that opens a Cropper.js modal; the crop
rectangle (natural pixel coords) POSTs to `POST /admin/media/{id}/crop` → SixLabors.ImageSharp crops
server-side **in place** (new file written, dimensions updated, old file removed; media id stays stable).

Public API: media fields expand to `{ id, url }` (single) or an array of `{ id, url }` (Gallery).

---

## Field Builder UI Rules

- Drag-and-drop reordering via **SortableJS**
- Each field card shows: type icon | field name | slug | required badge | localized badge | ⋮ menu
- On drag end: `PATCH /admin/content-types/{id}/fields/reorder` with body `{ "fieldIds": [3,1,5,2,4] }`
- Adding a new field: side panel slides in, select type first → form adapts to that type's options

---

## Localization Rules for Fields

- `is_localized = true`: value stored per `language_code` in `content_field_values`
- `is_localized = false`: value stored once with `language_code = 'all'`
- In the content edit form: language switcher tabs at top → changes active language → reloads field values.
  The tabs are built from the **tenant's active languages** (`languages` table via `ILanguageService`),
  not a hardcoded set; they fall back to the current code if none are configured.

---

## FieldType C# Enum

```csharp
public enum FieldType
{
    Text, RichText, Markdown, Number, Decimal, Boolean,
    Date, DateTime, Time, DateRange,
    Email, URL, Phone, Color, JSON, CodeSnippet,
    Image, Video, Audio, File, Gallery,
    Select, MultiSelect, Tags,
    Relation, MultiRelation,
    Slug, Password, GeoLocation, Rating
}
```
