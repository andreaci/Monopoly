namespace Monopoly.Server.Domain;

public sealed class Settings
{
    public string Language { get; set; } = "en";
    public int StartingCash { get; set; } = 1500;
    public int GoPayment { get; set; } = 200;
    public bool Auctions { get; set; } = true;
    public bool Trading { get; set; } = true;
    public bool Buildings { get; set; } = true;
    public bool Jail { get; set; } = true;
    public bool Mortgages { get; set; } = true;
    public bool BankRent { get; set; }
    public int Scale => Language == "it-GBP" ? 100 : 1;
    public string Currency => Language switch { "en" => "$", "it-GBP" => "£", _ => "€" };
    public bool Italian => Language.StartsWith("it");
}

public sealed class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Token { get; set; } = "";
    public int Cash { get; set; }
    public int Position { get; set; }
    public bool Connected { get; set; }
    public bool Bankrupt { get; set; }
    public bool InJail { get; set; }
    public int JailAttempts { get; set; }
    public List<string> JailCards { get; set; } = [];
}

public sealed record Square(int Id, string En, string It, string Type, string Group = "", string Color = "", int Price = 0, int[]? Rents = null, int BuildCost = 0);
public sealed class Deed
{
    public int SquareId { get; set; }
    public string? OwnerId { get; set; }
    public int Buildings { get; set; }
    public bool Mortgaged { get; set; }
}
public sealed record Card(string Id, string Deck, string En, string It, string Effect, int Amount = 0, int Target = 0, int HotelAmount = 0);
public sealed record Payment(string From, string? To, int Amount, string Reason);
public sealed record LogEntry(long Sequence, string En, string It, DateTimeOffset At);
public sealed record Roll(string PlayerId, int Die1, int Die2, DateTimeOffset StartedAt, DateTimeOffset EndsAt, bool Utility = false);
public sealed record Landing(string PlayerId, int SquareId, DateTimeOffset MovementStartsAt, int[] Path, DateTimeOffset StartedAt, DateTimeOffset LocationEndsAt, DateTimeOffset EndsAt, bool AutoAdvance, bool ExtraRoll);

public sealed class Auction
{
    public int SquareId { get; set; }
    public string OwnerId { get; set; } = "";
    public string VisitorId { get; set; } = "";
    public int OpeningBid { get; set; }
    public int HighestBid { get; set; }
    public string? BidderId { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool Paused { get; set; }
    public double RemainingSeconds { get; set; }
}

public sealed class Trade
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public int OfferCash { get; set; }
    public int RequestCash { get; set; }
    public int[] OfferDeeds { get; set; } = [];
    public int[] RequestDeeds { get; set; } = [];
    public int OfferJailCards { get; set; }
    public int RequestJailCards { get; set; }
}

public sealed class Command
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public int SquareId { get; set; }
    public int Amount { get; set; }
    public bool Accept { get; set; }
    public string? CardId { get; set; }
    public Trade? Trade { get; set; }
}
