using Monopoly.Server.Domain;
using Monopoly.Server.GameEngine;

namespace Monopoly.Server.Services;

// One lock protects commands, sessions, presence and timer transitions together.
public sealed class MatchService
{
    private readonly object gate = new();
    private readonly Engine engine = new();
    private readonly Dictionary<string, string> sessions = [];
    private readonly Dictionary<string, string?> connections = [];
    public string ManagerToken { get; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    public string Id => engine.MatchId;
    public string PlayerCookie => $"monopoly_player_{Id}";
    public string ManagerCookie => $"monopoly_manager_{Id}";

    public string? Identify(string? cookie) { lock (gate) return cookie != null && sessions.TryGetValue(cookie, out var id) ? id : null; }
    public object Snapshot(string? id = null) { lock (gate) return engine.Snapshot(id); }
    public (string Cookie, string PlayerId) Join(string name, string token)
    {
        lock (gate)
        {
            var p = engine.Join(name, token);
            var cookie = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            sessions[cookie] = p.Id;
            return (cookie, p.Id);
        }
    }
    public void Configure(Settings settings) { lock (gate) engine.Configure(settings); }
    public void Start() { lock (gate) engine.Start(); }
    public void Execute(string id, Command command) { lock (gate) engine.Execute(id, command); }
    public void Connect(string connectionId, string? playerId)
    {
        lock (gate) { connections[connectionId] = playerId; if (playerId != null) engine.Presence(playerId, true); }
    }
    public void Disconnect(string connectionId)
    {
        lock (gate)
        {
            if (connections.Remove(connectionId, out var id) && id != null && !connections.Values.Contains(id)) engine.Presence(id, false);
        }
    }
    public bool Tick() { lock (gate) return engine.Tick(DateTimeOffset.UtcNow); }
}
