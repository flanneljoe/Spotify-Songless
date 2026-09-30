using System.Net.Http.Headers;
using System.Text.Json;

public class SpotifyMusicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SpotifyAuthService _authService;

    public SpotifyMusicService(IHttpClientFactory httpClientFactory, SpotifyAuthService authService)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
    }

    private async Task<string?> GetUserMarketAsync(HttpClient client)
    {
        var response = await client.GetAsync("https://api.spotify.com/v1/me");
        if (!response.IsSuccessStatusCode) return null;

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("country", out var country)
            ? country.GetString()
            : null;
    }

    public async Task<(string TrackId, string TrackName, string ArtistName)?> GetRandomSavedTrackAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        if (token is null) return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var market = await GetUserMarketAsync(client);
        var marketParam = market is not null ? $"&market={market}" : "";

        // Step 1: Get total track count
        var countResponse = await client.GetAsync($"https://api.spotify.com/v1/me/tracks?limit=1{marketParam}");
        if (!countResponse.IsSuccessStatusCode) return null;

        var countJson = JsonDocument.Parse(await countResponse.Content.ReadAsStringAsync());
        var total = countJson.RootElement.GetProperty("total").GetInt32();

        if (total == 0) return null;

        // Step 2: Fetch a random track, retrying if not playable
        var random = new Random();
        const int maxAttempts = 5;

        for (int i = 0; i < maxAttempts; i++)
        {
            var offset = random.Next(0, total);
            var trackResponse = await client.GetAsync($"https://api.spotify.com/v1/me/tracks?limit=1&offset={offset}{marketParam}");
            if (!trackResponse.IsSuccessStatusCode) return null;

            var trackJson = JsonDocument.Parse(await trackResponse.Content.ReadAsStringAsync());
            var track = trackJson.RootElement
                .GetProperty("items")[0]
                .GetProperty("track");

            var isPlayable = track.TryGetProperty("is_playable", out var playableProp) &&
                             playableProp.ValueKind != JsonValueKind.Null &&
                             playableProp.GetBoolean();

            if (!isPlayable) continue;

            return (
                track.GetProperty("id").GetString()!,
                track.GetProperty("name").GetString()!,
                track.GetProperty("artists")[0].GetProperty("name").GetString()!
            );
        }

        return null;
    }
}