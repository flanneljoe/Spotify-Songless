using SpotifySongless.Components;

namespace SpotifySongless
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<SpotifyAuthService>();
            builder.Services.AddScoped<SpotifyMusicService>();
            builder.Services.AddScoped<SpotifyPlayerService>();

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(60);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseSession();

            app.MapGet("/auth/begin", (HttpContext context, SpotifyAuthService auth) =>
            {
                var state = Guid.NewGuid().ToString("N");
                context.Session.SetString("Spotify_State", state);

                Console.WriteLine($"[auth/begin] Session ID: {context.Session.Id}");
                Console.WriteLine($"[auth/begin] State set: {state}");
                Console.WriteLine($"[auth/begin] State readback: {context.Session.GetString("Spotify_State")}");


                var url = auth.GetAuthorizationUrl(state);
                return Results.Redirect(url);
            });


            app.MapGet("/callback/spotify", async (HttpContext context, SpotifyAuthService auth) =>
            {
                Console.WriteLine($"[callback] Session ID: {context.Session.Id}");
                Console.WriteLine($"[callback] Saved state: {context.Session.GetString("Spotify_State")}");

                var code = context.Request.Query["code"].ToString();
                var state = context.Request.Query["state"].ToString();
                var error = context.Request.Query["error"].ToString();
                var errorDescription = context.Request.Query["error_description"].ToString();

                Console.WriteLine($"Callback received — code: {code}, state: {state}, error: {error}");

                if (!string.IsNullOrEmpty(error))
                    return Results.Redirect($"/auth-error?error={Uri.EscapeDataString(error)}&description={Uri.EscapeDataString(errorDescription)}");

                if (!await auth.ExchangeCodeAsync(code, state))
                    return Results.Redirect("/auth-error?error=exchange_failed&description=Token+exchange+failed.+Check+your+Client+ID%2C+Secret%2C+and+Redirect+URI.");

                return Results.Redirect("/player");
            });

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();


            

            app.Run();
        }
    }
}
