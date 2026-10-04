using System.Net;
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
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost };
foreach (var address in (builder.Configuration["TRUSTED_PROXIES"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    forwarded.KnownProxies.Add(IPAddress.Parse(address));
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
    // Same-origin cookies protect both HTTP mutations and the WebSocket handshake.
    if (context.Request.Headers.TryGetValue("Origin", out var origin) &&
        (!Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri) || uri.Authority != context.Request.Host.Value))
    { context.Response.StatusCode = 403; return; }
    try { await next(); }
    catch (InvalidOperationException e) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = e.Message }); }
});
app.UseDefaultFiles();
app.UseStaticFiles();
// Route matching must run after UsePathBase strips the configured prefix.
// Otherwise WebApplication's implicit routing matches /monopoly/api/... to the SPA fallback.
app.UseRouting();

bool IsManager(HttpContext context, MatchService match) => context.Request.Cookies[match.ManagerCookie] == match.ManagerSecret;

app.MapGet("/api/session", (HttpContext context, MatchService match) =>
{
    var playerId = match.Identify(context.Request.Cookies[match.PlayerCookie]);
    return Results.Ok(new { playerId, manager = IsManager(context, match), state = match.Snapshot(playerId) });
});
app.MapPost("/api/manager", (HttpContext context, MatchService match) =>
{
    var local = builder.Configuration["ALLOW_LOCAL_MANAGER"] != "false" &&
        context.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote) &&
        (context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]") &&
        !context.Request.Headers.ContainsKey("X-Forwarded-For") && !context.Request.Headers.ContainsKey("X-Forwarded-Host");
    if (!local && context.Request.Headers["X-Manager-Key"] != match.ManagerSecret) return Results.StatusCode(403);
    context.Response.Cookies.Append(match.ManagerCookie, match.ManagerSecret, new() { HttpOnly = true, SameSite = SameSiteMode.Strict, IsEssential = true, Secure = context.Request.IsHttps, Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value + "/" : "/" });
    return Results.Ok();
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
app.Logger.LogInformation("Manager access code (Docker): {Code}", app.Services.GetRequiredService<MatchRegistry>().ManagerSecret);
app.Run();

public sealed record JoinRequest(string Name, string Token);
