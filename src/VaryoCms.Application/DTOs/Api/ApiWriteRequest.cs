using System.Text.Json;

namespace VaryoCms.Application.DTOs.Api;

// Request body for POST (create) and PUT (update) via the public API.
// Fields are keyed by field slug (not id) and represented as raw JSON values
// so the API can accept any scalar (string, number, bool).
public class ApiWriteRequest
{
    // Language code for localized fields (defaults to tenant default when absent).
    public string? Lang { get; set; }

    // Item status: "draft" | "published" | "archived". Defaults to "draft" when absent.
    public string? Status { get; set; }

    // Desired slug. Auto-generated from title when absent.
    public string? Slug { get; set; }

    // Per-language title (stored in content_item_titles). Also drives auto-slug when Slug is absent.
    public string? Title { get; set; }

    // Scalar field values keyed by field slug. JsonElement preserves the JSON type for type-safe mapping.
    public Dictionary<string, JsonElement>? Fields { get; set; }

    // Relation / MultiRelation target content item ids, keyed by field slug.
    public Dictionary<string, List<int>>? Relations { get; set; }
}
