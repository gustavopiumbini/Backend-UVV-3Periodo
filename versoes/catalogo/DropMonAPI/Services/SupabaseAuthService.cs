using System.Net.Http.Json;
using System.Text.Json;

namespace DropMonAPI.Services;

public enum LoginResult { Success, InvalidCredentials, NotConfigured, Unavailable }

public sealed record LoginOutcome(LoginResult Result, string? UserId = null, string? Email = null, int ExpiresIn = 3600);

public sealed class SupabaseAuthService(IHttpClientFactory clients, IConfiguration configuration,
    ILogger<SupabaseAuthService> logger)
{
    public bool Required => configuration.GetValue<bool>("Admin:RequireAuthentication");

    public bool Configured => Uri.TryCreate(configuration["Supabase:Url"], UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(configuration["Supabase:PublishableKey"]) &&
        Guid.TryParse(configuration["Admin:UserId"], out _);

    public async Task<LoginOutcome> LoginAsync(string email, string password, CancellationToken ct)
    {
        if (!Configured) return new(LoginResult.NotConfigured);

        var baseUrl = configuration["Supabase:Url"]!.TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/auth/v1/token?grant_type=password");
        request.Headers.TryAddWithoutValidation("apikey", configuration["Supabase:PublishableKey"]);
        request.Content = JsonContent.Create(new { email, password });

        try
        {
            using var response = await clients.CreateClient().SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new(LoginResult.InvalidCredentials);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var root = json.RootElement;
            if (!root.TryGetProperty("user", out var user) ||
                !user.TryGetProperty("id", out var idElement) ||
                !Guid.TryParse(idElement.GetString(), out var userId) ||
                !Guid.TryParse(configuration["Admin:UserId"], out var adminId) || userId != adminId)
                return new(LoginResult.InvalidCredentials);

            var userEmail = user.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : email;
            var expiresIn = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds)
                ? Math.Clamp(seconds, 300, 3600) : 3600;
            return new(LoginResult.Success, userId.ToString(), userEmail, expiresIn);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Não foi possível consultar o Supabase Auth.");
            return new(LoginResult.Unavailable);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "O Supabase Auth retornou uma resposta inesperada.");
            return new(LoginResult.Unavailable);
        }
    }
}
