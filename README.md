## Spotify Songless 
A replayable Songless clone that uses your liked songs on Spotify as the pool of possible mystery songs to guess at.

### How to build the project
Since Spotify now restricts API access to corporate entities with 250k MAU, you'll need to create a Spotify Developer account and build the project yourself to play it.

1. Create a Spotify developer account at: https://wwww.developer.spotify.com/
2. Navigate to your developer dashboard and create a new app.
3. Add the following redirect URI's to your app:
    https://[::1]:7200/callback/spotify
    http://[::1]:5185/callback/spotify
4. Copy your ClientID and ClientSecret for your app, you'll need these in a later step.
5. Clone this repository and open it in Visual Studio.
6. Open a new PowerShell terminal in your project directory and run the following commands:
     dotnet user-secrets init
     dotnet user-secrets set "Spotify:ClientId" "your_client_id_here"
     dotnet user-secrets set "Spotify:ClientSecret" "your_client_secret_here"
7. Build the project and login to Spotify to begin playing the game.
