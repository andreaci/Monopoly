using System.Collections.Concurrent;

namespace Monopoly.Server.Services;

public sealed class MatchRegistry
{
    private readonly ConcurrentDictionary<string, MatchService> matches = new();
    public string ManagerSecret { get; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    public IEnumerable<MatchService> Matches => matches.Values;
    public MatchService Resolve(HttpContext context)
    {
        var id = context.Request.Query["match"].ToString();
        if (id.Length > 0)
            return matches.TryGetValue(id, out var match) ? match : throw new InvalidOperationException("This match no longer exists. Ask the manager for a new QR code.");
        if (context.Request.Path != "/api/session") throw new InvalidOperationException("A match link is required.");
        var created = new MatchService(ManagerSecret);
        matches[created.Id] = created;
        return created;
    }
}
