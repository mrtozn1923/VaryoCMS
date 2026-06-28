namespace VaryoCms.Application.DTOs.Api;

// Parsed query parameters for a public API list request.
public class ApiListRequest
{
    public string? Lang { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }                 // e.g. "created_at:desc"
    public string? Fields { get; set; }               // projection, e.g. "title,slug"
    public Dictionary<string, string>? Filters { get; set; }   // filter[status] -> "published"
}
