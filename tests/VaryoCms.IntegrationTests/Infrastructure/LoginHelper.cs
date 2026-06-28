using System.Net;
using System.Text.RegularExpressions;

namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// Authenticates an HttpClient against the in-process test server.
/// Returns an HttpClient whose CookieContainer holds a valid auth cookie.
/// </summary>
public static class LoginHelper
{
    private static readonly Regex TokenRegex =
        new(@"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Performs login flow (GET token → POST credentials) and returns an authenticated client.
    /// Requires EmailVerification:Enabled=false in test config (default dev seed state).
    /// </summary>
    public static async Task LoginAsync(HttpClient client, string email, string password)
    {
        // Step 1: GET login page → extract antiforgery token
        var loginPage = await client.GetAsync("/account/login");
        loginPage.EnsureSuccessStatusCode();
        string html = await loginPage.Content.ReadAsStringAsync();

        var match = TokenRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException("Antiforgery token not found on login page");

        string token = match.Groups[1].Value;

        // Step 2: POST credentials
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email",    email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync("/account/login", form);

        // Successful login → 302 redirect to /
        if (response.StatusCode != HttpStatusCode.Redirect &&
            response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Login failed. Status: {response.StatusCode}. " +
                $"Check credentials or EmailVerification:Enabled setting.");
        }
    }

    /// <summary>
    /// Creates a pre-authenticated HttpClient for the TenantAdmin dev seed user.
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(
        CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
        });
        await LoginAsync(client, "admin@dev.local", "Admin123!");
        return client;
    }
}
