using Microsoft.AspNetCore.SignalR;
using Monopoly.Server.Domain;
using Monopoly.Server.Services;

namespace Monopoly.Server.Hubs;

public sealed class GameHub(MatchService match) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var id = match.Identify(Context.GetHttpContext()?.Request.Cookies[match.PlayerCookie]);
        match.Connect(Context.ConnectionId, id);
        await Groups.AddToGroupAsync(Context.ConnectionId, match.Id);
        await Clients.Group(match.Id).SendAsync("state", match.Snapshot());
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        match.Disconnect(Context.ConnectionId);
        await Clients.Group(match.Id).SendAsync("state", match.Snapshot());
        await base.OnDisconnectedAsync(exception);
    }
    public object GetState()
    {
        var id = match.Identify(Context.GetHttpContext()?.Request.Cookies[match.PlayerCookie]);
        return match.Snapshot(id);
    }
    public async Task SendCommand(Command command)
    {
        var id = match.Identify(Context.GetHttpContext()?.Request.Cookies[match.PlayerCookie]);
        if (id == null) throw new HubException("Join the match first.");
        try { match.Execute(id, command); }
        catch (InvalidOperationException e) { throw new HubException(e.Message); }
        await Clients.Group(match.Id).SendAsync("state", match.Snapshot());
    }
}
