using Microsoft.JSInterop;

public class SpotifyPlayerService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public SpotifyPlayerService(IJSRuntime js)
    {
        _js = js;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>(
            "import", "/js/spotify-player.js");
        return _module;
    }

    public async Task InitializeAsync<T>(DotNetObjectReference<T> dotNetHelper, string accessToken) where T : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initializePlayer", dotNetHelper, accessToken);
    }

    public async Task PlayTrackAsync(string accessToken, string deviceId, string trackUri, int positionMs = 0)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("play", accessToken, deviceId, trackUri, positionMs);
    }

    public async Task PauseAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("pause");
    }

    public async Task ResumeAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("resume");
    }

    public async Task SeekAsync(int positionMs)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("seek", positionMs);
    }

    public async Task DisconnectAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("disconnect");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}