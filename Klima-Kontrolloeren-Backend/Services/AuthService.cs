using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public sealed class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IDbConnectionFactory dbConnectionFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<AuthResult?> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var apiKey = _configuration["Firebase:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Firebase:ApiKey is not configured.");

        var client = _httpClientFactory.CreateClient(nameof(AuthService));
        var response = await client.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
            new FirebaseSignInRequest
            {
                Email = email,
                Password = password,
                ReturnSecureToken = true
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            return null;

        var users = await _dbConnectionFactory.GetKlimaDataUserAsync(payload.LocalId);
        if (users is null || users.Count == 0 || !users[0].Enabled)
            return null;

        return new AuthResult
        {
            IdToken = payload.IdToken,
            RefreshToken = payload.RefreshToken,
            ExpiresIn = int.TryParse(payload.ExpiresIn, out var expiresInSeconds) ? expiresInSeconds : 0
        };
    }
    public async Task<List<KlimaDataUser>?> GetUserSensorsAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _dbConnectionFactory.GetKlimaDataUserAsync(token);
    }

    public async Task<string?> GetUserUIDAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            var client = _httpClientFactory.CreateClient(nameof(AuthService));
            var response = await client.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}",
                new { idToken = token });

            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<FirebaseIdTokenResponse>();
            return payload?.Users?.FirstOrDefault()?.LocalId;
        }
        catch
        {
            return null;
        }
    }

    private sealed class FirebaseIdTokenResponse
    {
        [JsonPropertyName("users")]
        public List<FirebaseUser>? Users { get; set; }
    }

    private sealed class FirebaseUser
    {
        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;
    }

    private sealed class FirebaseSignInRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }

    private sealed class FirebaseSignInResponse
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;

        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public string ExpiresIn { get; set; } = string.Empty;
    }
}