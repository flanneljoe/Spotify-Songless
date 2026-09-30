export function initializePlayer(dotNetHelper, accessToken) {
    window.onSpotifyWebPlaybackSDKReady = () => {
        const player = new Spotify.Player({
            name: 'Spotify Songless',
            getOAuthToken: cb => cb(accessToken),
            volume: 0.5
        });

        // Store player reference for control functions
        window.spotifyPlayer = player;

        // Ready - fires when device is registered with Spotify
        player.addListener('ready', ({ device_id }) => {
            console.log('Player ready with device ID:', device_id);
            dotNetHelper.invokeMethodAsync('OnPlayerReady', device_id);
        });

        // Player state changes
        player.addListener('player_state_changed', (state) => {
            if (!state) return;
            dotNetHelper.invokeMethodAsync('OnPlayerStateChanged',
                state.paused,
                state.position,
                state.duration
            );
        });

        // Error handlers
        player.addListener('not_ready', ({ device_id }) => {
            console.warn('Device has gone offline:', device_id);
        });
        player.addListener('initialization_error', ({ message }) => {
            dotNetHelper.invokeMethodAsync('OnPlayerError', 'initialization_error: ' + message);
        });
        player.addListener('authentication_error', ({ message }) => {
            dotNetHelper.invokeMethodAsync('OnPlayerError', 'authentication_error: ' + message);
        });
        player.addListener('account_error', ({ message }) => {
            dotNetHelper.invokeMethodAsync('OnPlayerError', 'account_error: ' + message);
        });

        player.connect();
    };

    // Load the Spotify SDK script
    const script = document.createElement('script');
    script.src = 'https://sdk.scdn.co/spotify-player.js';
    script.async = true;
    document.head.appendChild(script);
}

export function play(accessToken, deviceId, trackUri, positionMs) {
    return fetch(`https://api.spotify.com/v1/me/player/play?device_id=${deviceId}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify({
            uris: [trackUri],
            position_ms: positionMs ?? 0
        })
    });
}

export function pause() {
    return window.spotifyPlayer?.pause();
}

export function resume() {
    return window.spotifyPlayer?.resume();
}

export function seek(positionMs) {
    return window.spotifyPlayer?.seek(positionMs);
}

export function disconnect() {
    window.spotifyPlayer?.disconnect();
}