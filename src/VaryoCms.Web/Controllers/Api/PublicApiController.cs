using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Api;

// The public API has its own per-config auth (None/ApiKey); it must bypass the global cookie auth filter.
[AllowAnonymous]
[ApiController]
[Route("api/v1/{tenantSlug}/{contentTypeSlug}")]
public class PublicApiController : ControllerBase
{
    private readonly IPublicApiService _api;

    public PublicApiController(IPublicApiService api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> GetList(string tenantSlug, string contentTypeSlug, CancellationToken ct)
        => Map(await _api.GetListAsync(tenantSlug, contentTypeSlug, BuildListRequest(), Credentials(), ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        string tenantSlug, string contentTypeSlug, int id, [FromQuery] string? lang, CancellationToken ct)
        => Map(await _api.GetByIdAsync(tenantSlug, contentTypeSlug, id, lang, Credentials(), ct));

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(
        string tenantSlug, string contentTypeSlug, string slug, [FromQuery] string? lang, CancellationToken ct)
        => Map(await _api.GetBySlugAsync(tenantSlug, contentTypeSlug, slug, lang, Credentials(), ct));

    [HttpPost("")]
    public async Task<IActionResult> Create(
        string tenantSlug, string contentTypeSlug, [FromBody] ApiWriteRequest body, CancellationToken ct)
    {
        var result = await _api.CreateAsync(tenantSlug, contentTypeSlug, body, Credentials(), ct);
        if (!result.IsSuccess) return MapWriteError(result.Error);
        return CreatedAtAction(nameof(GetById), new { tenantSlug, contentTypeSlug, id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        string tenantSlug, string contentTypeSlug, int id, [FromBody] ApiWriteRequest body, CancellationToken ct)
    {
        var result = await _api.UpdateAsync(tenantSlug, contentTypeSlug, id, body, Credentials(), ct);
        if (!result.IsSuccess) return MapWriteError(result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        string tenantSlug, string contentTypeSlug, int id, CancellationToken ct)
    {
        var result = await _api.DeleteAsync(tenantSlug, contentTypeSlug, id, Credentials(), ct);
        if (!result.IsSuccess) return MapWriteError(result.Error);
        return NoContent();
    }

    // Uses explicit StatusCode() so cookie authentication does not redirect to login.
    private IActionResult MapWriteError(string? error) => error switch
    {
        "Unauthorized" => StatusCode(401, new { error = "Unauthorized" }),
        "Forbidden"    => StatusCode(403, new { error = "Forbidden" }),
        "NotFound"     => StatusCode(404, new { error = "NotFound" }),
        not null when error.StartsWith("Validation:", StringComparison.Ordinal) =>
            StatusCode(400, new { error = error["Validation:".Length..].Trim() }),
        _ => StatusCode(404, new { error = "NotFound" })
    };

    private ApiCredentials Credentials()
    {
        string? apiKey = Request.Headers.TryGetValue("X-API-Key", out var k) ? k.ToString() : null;

        string? bearer = null;
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            var value = auth.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                bearer = value["Bearer ".Length..].Trim();
        }

        return new ApiCredentials(apiKey, bearer);
    }

    private ApiListRequest BuildListRequest()
    {
        var q = Request.Query;
        var filters = new Dictionary<string, string>();
        foreach (var kv in q)
            if (kv.Key.StartsWith("filter[", StringComparison.Ordinal) && kv.Key.EndsWith(']'))
                filters[kv.Key[7..^1]] = kv.Value.ToString();

        return new ApiListRequest
        {
            Lang = q["lang"],
            Page = int.TryParse(q["page"], out var p) ? p : 1,
            PageSize = int.TryParse(q["pageSize"], out var ps) ? ps : 20,
            Sort = q["sort"],
            Fields = q["fields"],
            Filters = filters.Count > 0 ? filters : null
        };
    }

    private IActionResult Map<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return result.Error switch
        {
            "Unauthorized" => StatusCode(401, new { error = "Unauthorized" }),
            "Forbidden"    => StatusCode(403, new { error = "Forbidden" }),
            _              => StatusCode(404, new { error = "NotFound" })
        };
    }
}
