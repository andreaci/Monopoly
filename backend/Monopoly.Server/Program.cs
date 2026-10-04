using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.HttpOverrides;
using Monopoly.Server.Domain;
using Monopoly.Server.Hubs;
using Monopoly.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5080");
builder.Services.AddSingleton<MatchRegistry>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MatchService>(services => services.GetRequiredService<MatchRegistry>().Resolve(services.GetRequiredService<IHttpContextAccessor>().HttpContext!));
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 16 * 1024);
builder.Services.AddHostedService<GameTicker>();
var app = builder.Build();
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    ForwardLimit = null
};
// Accept forwarded headers from any proxy, as requested for this deployment.
forwarded.KnownProxies.Clear();
forwarded.KnownIPNetworks.Clear();
app.UseForwardedHeaders(forwarded);
var basePath = builder.Configuration["APP_BASE_PATH"]?.Trim().Trim('/') ?? "";
if (basePath.Length > 0)
{
    app.UsePathBase($"/{basePath}");
    app.Use(async (context, next) =>
    {
        if (!context.Request.PathBase.HasValue) { context.Response.StatusCode = 404; return; }
        await next();
    });
}

app.Use(async (context, next) =>
{
    try { await next(); }
    catch (InvalidOperationException e) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = e.Message }); }
});
app.UseDefaultFiles();
app.UseStaticFiles();
// Route matching must run after UsePathBase strips the configured prefix.
// Otherwise WebApplication's implicit routing matches /monopoly/api/... to the SPA fallback.
app.UseRouting();

bool IsManager(HttpContext context, MatchService match) => context.Request.Cookies[match.ManagerCookie] == match.ManagerToken;

app.MapGet("/api/session", (HttpContext context, MatchService match) =>
{
    var playerId = match.Identify(context.Request.Cookies[match.PlayerCookie]);
    var created = string.IsNullOrEmpty(context.Request.Query["match"]);
    if (created)
        context.Response.Cookies.Append(match.ManagerCookie, match.ManagerToken, new() { HttpOnly = true, SameSite = SameSiteMode.Strict, IsEssential = true, Secure = context.Request.IsHttps, Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value + "/" : "/" });
    return Results.Ok(new { playerId, manager = created || IsManager(context, match), state = match.Snapshot(playerId) });
});
app.MapPost("/api/settings", async (Settings settings, HttpContext context, MatchService match, IHubContext<GameHub> hub) =>
{
    if (!IsManager(context, match)) return Results.StatusCode(403);
    match.Configure(settings); await hub.Clients.Group(match.Id).SendAsync("state", match.Snapshot()); return Results.Ok();
});
app.MapPost("/api/start", async (HttpContext context, MatchService match, IHubContext<GameHub> hub) =>
{
    if (!IsManager(context, match)) return Results.StatusCode(403);
    match.Start(); await hub.Clients.Group(match.Id).SendAsync("state", match.Snapshot()); return Results.Ok();
});
app.MapPost("/api/join", (JoinRequest request, HttpContext context, MatchService match) =>
{
    var existing = match.Identify(context.Request.Cookies[match.PlayerCookie]);
    if (existing != null) return Results.Ok(new { playerId = existing });
    var session = match.Join(request.Name, request.Token);
    context.Response.Cookies.Append(match.PlayerCookie, session.Cookie, new() { HttpOnly = true, SameSite = SameSiteMode.Strict, IsEssential = true, Secure = context.Request.IsHttps, Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value + "/" : "/", MaxAge = TimeSpan.FromDays(7) });
    return Results.Ok(new { playerId = session.PlayerId });
});
app.MapHub<GameHub>("/hubs/game", options => options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets);
app.MapFallbackToFile("index.html");
app.Run();

public sealed record JoinRequest(string Name, string Token);
