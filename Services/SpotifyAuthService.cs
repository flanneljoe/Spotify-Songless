using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class SpotifyAuthService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string SessionKeyAccessToken = "Spotify_AccessToken";
    private const string SessionKeyRefreshToken = "Spotify_RefreshToken";
    private const string SessionKeyExpiry = "Spotify_TokenExpiry";

    public SpotifyAuthService(IConfiguration config, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetAuthorizationUrl(string state)
    {
        var clientId = _config["Spotify:ClientId"];
        var redirectUri = Uri.EscapeDataString(_config["Spotify:RedirectUri"]!);
        var scopes = Uri.EscapeDataString(_config["Spotify:Scopes"]!);

        return $"https://accounts.spotify.com/authorize" +
               $"?response_type=code" +
               $"&client_id={clientId}" +
               $"&scope={scopes}" +
               $"&redirect_uri={redirectUri}" +
               $"&state={state}";
    }

    public async Task<bool> ExchangeCodeAsync(string code, string state)
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        var savedState = session.GetString("Spotify_State");

        if (savedState != state) 
            return false; // CSRF check

        var clientId = _config["Spotify:ClientId"];
        var clientSecret = _config["Spotify:ClientSecret"];
        var redirectUri = _config["Spotify:RedirectUri"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri!
        });

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) 
            return false;

        var json = await response.Content.ReadAsStringAsync();
        var tokens = JsonDocument.Parse(json).RootElement;

        session.SetString(SessionKeyAccessToken, tokens.GetProperty("access_token").GetString()!);
        session.SetString(SessionKeyRefreshToken, tokens.GetProperty("refresh_token").GetString()!);
        session.SetString(SessionKeyExpiry, DateTime.UtcNow.AddSeconds(tokens.GetProperty("expires_in").GetInt32()).ToString("O"));

        return true;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        var expiry = session.GetString(SessionKeyExpiry);

        if (expiry is null)
            return null;

        if (DateTime.Parse(expiry) <= DateTime.UtcNow.AddMinutes(1))
            await RefreshTokenAsync();

        return session.GetString(SessionKeyAccessToken);
    }

    private async Task RefreshTokenAsync()
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        var refreshToken = session.GetString(SessionKeyRefreshToken);
        if (refreshToken is null) return;

        var clientId = _config["Spotify:ClientId"];
        var clientSecret = _config["Spotify:ClientSecret"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return;

        var json = await response.Content.ReadAsStringAsync();
        var tokens = JsonDocument.Parse(json).RootElement;

        session.SetString(SessionKeyAccessToken, tokens.GetProperty("access_token").GetString()!);
        session.SetString(SessionKeyExpiry, DateTime.UtcNow.AddSeconds(tokens.GetProperty("expires_in").GetInt32()).ToString("O"));
    }

    public bool IsAuthenticated()
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        return session.GetString(SessionKeyAccessToken) is not null;
    }

    public void Logout()
    {
        _httpContextAccessor.HttpContext!.Session.Clear();
    }
}