using Microsoft.AspNetCore.SignalR;
using Monopoly.Server.Hubs;

namespace Monopoly.Server.Services;

public sealed class GameTicker(MatchRegistry registry, IHubContext<GameHub> hub, ILogger<GameTicker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { foreach (var match in registry.Matches) if (match.Tick()) await hub.Clients.Group(match.Id).SendAsync("state", match.Snapshot(), stoppingToken); }
                catch (Exception e) { logger.LogError(e, "Game timer failed"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
