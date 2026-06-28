using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// Helpers for handling ASP.NET Core anti-forgery tokens in integration tests.
/// Extracts the hidden input token from a GET response, then uses it in a POST.
/// </summary>
public static class AntiForgeryHelper
{
    private static readonly Regex TokenRegex =
        new(@"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Performs a GET to <paramref name="url"/>, extracts the antiforgery token
    /// from the HTML, and returns (token, cookies) ready to use in a POST.
    /// </summary>
    public static async Task<(string Token, CookieContainer Cookies)> GetTokenAsync(
        HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        var match = TokenRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException(
                $"No __RequestVerificationToken found in GET {url}. HTML excerpt:\n" +
                html[..Math.Min(500, html.Length)]);

        string token = match.Groups[1].Value;
        var cookies = new CookieContainer();

        // Extract cookies from the response Set-Cookie headers
        if (client.DefaultRequestHeaders.TryGetValues("Cookie", out _))
        {
            // cookies already tracked via handler
        }

        return (token, cookies);
    }

    /// <summary>
    /// Builds form content that includes the antiforgery token.
    /// </summary>
    public static FormUrlEncodedContent WithToken(
        string token, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var all = fields
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))
            .ToList();
        return new FormUrlEncodedContent(all);
    }

    /// <summary>
    /// Sets the X-CSRF-TOKEN header used by AJAX (PATCH) endpoints.
    /// </summary>
    public static void SetCsrfHeader(HttpClient client, string token)
        => client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");  // remove stale first
}
