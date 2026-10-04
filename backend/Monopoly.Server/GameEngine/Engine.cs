using System.Security.Cryptography;
using Monopoly.Server.Data;
using Monopoly.Server.Domain;

namespace Monopoly.Server.GameEngine;

// This class has no transport dependencies. MatchService serializes all calls.
public sealed partial class Engine
{
    public Settings Settings { get; private set; } = new();
    public string MatchId { get; } = Guid.NewGuid().ToString("N");
    public List<Player> Players { get; } = [];
    public List<Deed> Deeds { get; } = Board.Squares.Where(s => s.Type is "street" or "rail" or "utility").Select(s => new Deed { SquareId = s.Id }).ToList();
    public List<LogEntry> Log { get; } = [];
    public string Phase { get; private set; } = "lobby";
    public int Turn { get; private set; }
    public long Revision { get; private set; }
    public Roll? Roll { get; private set; }
    public Auction? Auction { get; private set; }
    public Trade? Trade { get; private set; }
    public Card? LastCard { get; private set; }
    private readonly List<(long Sequence, string PlayerId, Card Card, DateTimeOffset At)> cardDraws = [];
    private long cardSequence;
    public string? WinnerId { get; private set; }
    public int Doubles { get; private set; }
    public Payment? Debt => payments.TryPeek(out var p) && Find(p.From).Cash < p.Amount ? p : null;
    public Player? Active => Phase == "lobby" || Players.Count == 0 ? null : Players[Turn];
    public string? WaitingFor => RequiredPlayers().FirstOrDefault(p => !p.Connected)?.Id;
    public int HousesLeft => 32 - Deeds.Where(d => d.Buildings < 5).Sum(d => d.Buildings);
    public int HotelsLeft => 12 - Deeds.Count(d => d.Buildings == 5);

    private readonly Dictionary<string, Queue<Card>> decks = [];
    private readonly Queue<Payment> payments = new();
    private Action? afterPayments;
    private int rentMultiplier = 1;
    private bool extraRoll;
    private bool utilityRoll;
    private bool jailReleaseRoll;
    private readonly HashSet<string> processed = [];
    private readonly Queue<string> processedOrder = new();
    private readonly Func<int> nextDie;

    public Engine(Func<int>? dice = null, IEnumerable<Card>? cardOrder = null)
    {
        nextDie = dice ?? (() => RandomNumberGenerator.GetInt32(1, 7));
        foreach (var deck in new[] { "chance", "chest" })
        {
            var cards = (cardOrder ?? Cards.All).Where(c => c.Deck == deck).ToArray();
            if (cardOrder == null) RandomNumberGenerator.Shuffle(cards.AsSpan());
            decks[deck] = new(cards);
        }
    }

    public void Configure(Settings settings)
    {
        Require(Phase == "lobby", "Settings are locked after starting.");
        Require(settings.Language is "en" or "it-EUR" or "it-GBP", "Unknown language.");
        Require(settings.StartingCash is > 0 and <= 100_000_000 && settings.GoPayment is >= 0 and <= 1_000_000, "Invalid money settings.");
        Settings = settings;
        Changed();
    }

    public Player Join(string name, string token)
    {
        Require(Phase == "lobby", "The match has already started.");
        Require(name != null && token != null, "A name and token are required.");
        name = name!.Trim();
        Require(name.Length is > 0 and <= 24 && !name.Any(char.IsControl), "Use a name of 1–24 characters.");
        Require(Players.Count < 6, "The match is full.");
        Require(!Players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)), "That name is already taken.");
        Require(Board.Tokens.Contains(token) && !Players.Any(p => p.Token == token), "Choose an available token.");
        var player = new Player { Name = name, Token = token };
        Players.Add(player);
        Changed();
        return player;
    }

    public void Presence(string id, bool connected)
    {
        var p = Find(id);
        if (p.Connected == connected) return;
        p.Connected = connected;
        UpdateAuctionPause(DateTimeOffset.UtcNow);
        Changed();
    }

    public void Start()
    {
        Require(Phase == "lobby" && Players.Count is >= 2 and <= 6, "Between two and six players are required.");
        Require(Players.All(p => p.Connected), "Wait for every player to connect.");
        foreach (var p in Players) p.Cash = Settings.StartingCash;
        Phase = "ready";
        Note("The match has started.", "La partita è iniziata.");
        Changed();
    }

    public object Snapshot(string? playerId = null) => new
    {
        matchId = MatchId, revision = Revision, serverTime = DateTimeOffset.UtcNow, settings = Settings, phase = Phase,
        activePlayerId = Active?.Id, players = Players, deeds = Deeds, dice = Roll,
        auction = Auction, trade = Trade, debt = Debt is { } debt ? new { debt.From, debt.To, debt.Amount, reason = PaymentReason(debt, Settings.Italian) } : null, waitingFor = WaitingFor,
        winnerId = WinnerId, housesLeft = HousesLeft, hotelsLeft = HotelsLeft,
        rentDue = Phase is "rent" or "consent" or "auction" ? Rent(Deed(Active!.Position)) : 0,
        lastCard = LastCard == null ? null : CardView(LastCard),
        cardDraws = cardDraws.Select(e => new { e.Sequence, e.PlayerId, e.At, card = CardView(e.Card) }),
        log = Log.TakeLast(60).Select(e => new { e.Sequence, text = Settings.Italian ? e.It : e.En, e.At }),
        board = Board.Squares.Select(s => new
        {
            s.Id, name = Name(s), s.Type, s.Group, s.Color,
            price = s.Price * Settings.Scale, rents = s.Rents?.Select(r => r * Settings.Scale), buildCost = s.BuildCost * Settings.Scale,
            mortgage = s.Price * Settings.Scale / 2
        }),
        cards = Cards.All.Select(CardView), tokens = Board.Tokens,
        me = playerId, allowedActions = playerId == null ? Array.Empty<string>() : Allowed(Find(playerId))
    };

    private object CardView(Card card) => new { card.Id, card.Deck, text = CardText(card) };
    private string CardText(Card c) => (Settings.Italian ? c.It : c.En).Replace("{amount}", Money(c.Amount * Settings.Scale)).Replace("{hotel}", Money(c.HotelAmount * Settings.Scale));
    private string Money(int value) => Settings.Currency + value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo(Settings.Italian ? "it-IT" : "en-US"));
    private static string PaymentReason(Payment payment, bool italian)
    {
        var parts = payment.Reason.Split(" / ");
        return parts[italian && parts.Length > 1 ? 1 : 0];
    }
    private string Name(Square s) => Settings.Italian ? s.It : s.En;
    public Player Find(string id) => Players.FirstOrDefault(p => p.Id == id) ?? throw new InvalidOperationException("Unknown player.");
    private Deed Deed(int square) => Deeds.FirstOrDefault(d => d.SquareId == square) ?? throw new InvalidOperationException("Not a purchasable space.");
    private Square Square(Deed d) => Board.Squares[d.SquareId];
    private IEnumerable<Deed> Group(Deed d) => Deeds.Where(x => Square(x).Group == Square(d).Group && Square(x).Type == "street");
    private bool FullGroup(Deed d) => d.OwnerId != null && Group(d).All(x => x.OwnerId == d.OwnerId);
    private bool ClearGroup(Deed d) => Square(d).Type != "street" || Group(d).All(x => x.Buildings == 0);

    public string[] Allowed(Player p)
    {
        if (p.Bankrupt || !p.Connected || Phase is "lobby" or "finished" || WaitingFor != null) return [];
        var actions = new List<string>();
        if (Trade != null)
            return Trade.To == p.Id ? ["tradeRespond"] : Trade.From == p.Id ? ["tradeCancel"] : [];
        var active = p.Id == Active?.Id;
        if (active && Phase == "ready")
        {
            actions.Add("roll");
            if (p.InJail) { actions.Add("jailPay"); if (p.JailCards.Count > 0) actions.Add("jailCard"); }
        }
        if (active && Phase == "purchase") { actions.Add("buy"); actions.Add("decline"); }
        if (active && Phase == "rent")
        {
            actions.Add("payRent");
            if (Settings.Auctions && ClearGroup(Deed(p.Position)) && Board.Squares[p.Position].Type == "street") actions.Add("requestAuction");
        }
        if (Phase == "consent" && Auction?.OwnerId == p.Id) actions.Add("auctionConsent");
        if (Phase == "auction" && !Auction!.Paused) actions.Add("bid");
        if (Trade?.To == p.Id) actions.Add("tradeRespond");
        if (Trade?.From == p.Id) actions.Add("tradeCancel");
        var manage = (active && Phase is "ready" or "end") || (Phase == "debt" && Debt?.From == p.Id);
        if (manage && Trade == null)
        {
            if (Settings.Buildings) { actions.Add("build"); actions.Add("sellBuilding"); actions.Add("sellGroup"); }
            if (Settings.Mortgages) { actions.Add("mortgage"); actions.Add("unmortgage"); }
            if (Settings.Trading) actions.Add("tradeOffer");
        }
        if (active && Phase == "end" && Trade == null) actions.Add("endTurn");
        if (Phase == "debt" && Debt?.From == p.Id && Trade == null && !CanRaiseCash(p)) actions.Add("bankrupt");
        return actions.ToArray();
    }

    public void Execute(string playerId, Command command)
    {
        Require(command.Id is { Length: > 0 and <= 80 }, "A command ID is required.");
        var key = playerId + ":" + command.Id;
        if (processed.Contains(key)) return;
        var p = Find(playerId);
        Require(Allowed(p).Contains(command.Type), "That action is not available now.");
        switch (command.Type)
        {
            case "roll": BeginRoll(p); break;
            case "jailPay":
                extraRoll = false;
                Charge([new(p.Id, null, 50 * Settings.Scale, "Jail / Prigione")], () => { p.InJail = false; p.JailAttempts = 0; Phase = "ready"; });
                break;
            case "jailCard":
                var id = p.JailCards.First(); p.JailCards.Remove(id); ReturnCard(id); p.InJail = false; p.JailAttempts = 0; break;
            case "buy": Buy(p); break;
            case "decline": FinishMove(); break;
            case "payRent": SettleRent(); break;
            case "requestAuction": RequestAuction(p); break;
            case "auctionConsent": Consent(p, command); break;
            case "bid": Bid(p, command.Amount); break;
            case "endTurn": EndTurn(); break;
            case "build": Building(p, command.SquareId, false); break;
            case "sellBuilding": Building(p, command.SquareId, true); break;
            case "sellGroup": SellGroup(p, command.SquareId); break;
            case "mortgage": Mortgage(p, command.SquareId, false); break;
            case "unmortgage": Mortgage(p, command.SquareId, true); break;
            case "tradeOffer": OfferTrade(p, command.Trade ?? throw new InvalidOperationException("Trade is required.")); break;
            case "tradeRespond": RespondTrade(p, command.Accept); break;
            case "tradeCancel": Trade = null; break;
            case "bankrupt": Bankruptcy(p); break;
        }
        processed.Add(key); processedOrder.Enqueue(key);
        if (processedOrder.Count > 10000) processed.Remove(processedOrder.Dequeue());
        Changed();
    }

    public bool Tick(DateTimeOffset now)
    {
        var changed = false;
        if (Phase == "rolling" && Roll != null && now >= Roll.EndsAt) { ResolveRoll(); changed = true; }
        if (Phase == "auction" && Auction != null)
        {
            changed |= UpdateAuctionPause(now);
            if (!Auction.Paused && now >= Auction.EndsAt) { CompleteAuction(); changed = true; }
        }
        if (changed) Changed();
        return changed;
    }

    private IEnumerable<Player> RequiredPlayers()
    {
        if (Phase is "lobby" or "finished") return [];
        if (Phase == "auction") return Players.Where(p => !p.Bankrupt);
        if (Trade != null) return [Find(Trade.From), Find(Trade.To)];
        if (Phase == "consent") return [Active!, Find(Auction!.OwnerId)];
        if (Phase == "debt" && Debt != null) return [Find(Debt.From)];
        return [Active!];
    }

    private void BeginRoll(Player p, bool utility = false)
    {
        utilityRoll = utility;
        jailReleaseRoll = !utility && p.InJail;
        var now = DateTimeOffset.UtcNow;
        Roll = new(p.Id, nextDie(), nextDie(), now, now.AddSeconds(2.5), utility);
        Phase = "rolling";
        if (!utility) LastCard = null;
        Note($"{p.Name} rolls the dice.", $"{p.Name} tira i dadi.");
    }

    private void ResolveRoll()
    {
        var p = Active!;
        var total = Roll!.Die1 + Roll.Die2;
        if (utilityRoll) { utilityRoll = false; rentMultiplier = 10; SettleRent(); return; }
        var doubles = Roll.Die1 == Roll.Die2;
        extraRoll = doubles && !jailReleaseRoll;
        if (jailReleaseRoll)
        {
            if (doubles) { p.InJail = false; p.JailAttempts = 0; }
            else if (++p.JailAttempts < 3) { Phase = "end"; return; }
            else
            {
                Charge([new(p.Id, null, 50 * Settings.Scale, "Jail / Prigione")], () => { p.InJail = false; p.JailAttempts = 0; Advance(p, total); });
                return;
            }
        }
        else
        {
            Doubles = doubles ? Doubles + 1 : 0;
            if (Doubles == 3) { SendToJail(p); return; }
        }
        Advance(p, total);
    }

    private void Advance(Player p, int steps, bool forward = true)
    {
        var old = p.Position;
        p.Position = (old + steps + 40) % 40;
        if (forward && old + steps >= 40) Credit(p, Settings.GoPayment, "GO", "VIA");
        rentMultiplier = 1;
        Land(p);
    }

    private void MoveTo(Player p, int target, int multiplier = 1)
    {
        if (target <= p.Position) Credit(p, Settings.GoPayment, "GO", "VIA");
        p.Position = target;
        rentMultiplier = multiplier;
        Land(p);
    }

    private void Land(Player p)
    {
        var s = Board.Squares[p.Position];
        Note($"{p.Name} lands on {s.En}.", $"{p.Name} arriva su {s.It}.");
        switch (s.Type)
        {
            case "street": case "rail": case "utility":
                var d = Deed(s.Id);
                if (d.OwnerId == null) Phase = "purchase";
                else if (d.OwnerId == p.Id || (d.Mortgaged && !(s.Type == "street" && Settings.Auctions))) FinishMove();
                else if (s.Type == "utility" && rentMultiplier == 10) BeginRoll(p, true);
                else Phase = "rent";
                break;
            case "tax": Charge([new(p.Id, null, s.Price * Settings.Scale, "Tax / Tassa")], FinishMove); break;
            case "goToJail": SendToJail(p); break;
            case "chance": case "chest": Draw(p, s.Type); break;
            default: FinishMove(); break;
        }
    }

    private void SendToJail(Player p)
    {
        p.Position = 10; p.InJail = Settings.Jail; p.JailAttempts = 0; extraRoll = false; Doubles = 0;
        Note($"{p.Name} moves to jail.", $"{p.Name} va in prigione.");
        Phase = "end";
    }

    private void FinishMove() { rentMultiplier = 1; Phase = "end"; }
    private void EndTurn()
    {
        Trade = null;
        if (extraRoll && !Active!.Bankrupt) { Phase = "ready"; extraRoll = false; return; }
        Doubles = 0; extraRoll = false;
        do Turn = (Turn + 1) % Players.Count; while (Players[Turn].Bankrupt);
        Phase = "ready";
    }

    private void Buy(Player p)
    {
        var d = Deed(p.Position); var s = Square(d); var cost = s.Price * Settings.Scale;
        Require(d.OwnerId == null && p.Cash >= cost, "Not enough cash to buy this deed.");
        p.Cash -= cost; d.OwnerId = p.Id;
        Note($"{p.Name} buys {s.En} for {Money(cost)}.", $"{p.Name} compra {s.It} per {Money(cost)}.");
        FinishMove();
    }

    private int Rent(Deed d)
    {
        if (d.Mortgaged || d.OwnerId == null || d.OwnerId == Active!.Id) return 0;
        var s = Square(d);
        if (s.Type == "street") return s.Rents![d.Buildings] * Settings.Scale * (d.Buildings == 0 && FullGroup(d) ? 2 : 1);
        if (s.Type == "rail") return s.Rents![Deeds.Count(x => Square(x).Type == "rail" && x.OwnerId == d.OwnerId) - 1] * Settings.Scale * rentMultiplier;
        var owned = Deeds.Count(x => Square(x).Type == "utility" && x.OwnerId == d.OwnerId);
        return (Roll!.Die1 + Roll.Die2) * Settings.Scale * (rentMultiplier == 10 || owned == 2 ? 10 : 4);
    }

    private void SettleRent()
    {
        var d = Deed(Active!.Position);
        var amount = Rent(d);
        Charge(amount == 0 ? [] : [new(Active.Id, d.OwnerId, amount, "Rent / Affitto")], FinishMove);
    }

    private void Draw(Player p, string deck)
    {
        var c = decks[deck].Dequeue(); LastCard = c;
        cardDraws.Add((++cardSequence, p.Id, c, DateTimeOffset.UtcNow));
        if (cardDraws.Count > 32) cardDraws.RemoveAt(0);
        if (c.Effect != "jailCard") decks[deck].Enqueue(c);
        Note($"{p.Name}: {c.En.Replace("{amount}", Money(c.Amount * Settings.Scale)).Replace("{hotel}", Money(c.HotelAmount * Settings.Scale))}", $"{p.Name}: {c.It.Replace("{amount}", Money(c.Amount * Settings.Scale)).Replace("{hotel}", Money(c.HotelAmount * Settings.Scale))}");
        switch (c.Effect)
        {
            case "move": MoveTo(p, c.Target); break;
            case "back": Advance(p, -3, false); break;
            case "rail": case "utility":
                var next = Board.Squares.Where(s => s.Type == c.Effect).Select(s => s.Id).FirstOrDefault(i => i > p.Position, -1);
                if (next == -1) next = Board.Squares.First(s => s.Type == c.Effect).Id;
                MoveTo(p, next, c.Effect == "rail" ? 2 : 10); break;
            case "jail": SendToJail(p); break;
            case "jailCard": p.JailCards.Add(c.Id); FinishMove(); break;
            case "receive": Credit(p, c.Amount * Settings.Scale, "Card", "Carta"); FinishMove(); break;
            case "pay": Charge([new(p.Id, null, c.Amount * Settings.Scale, "Card / Carta")], FinishMove); break;
            case "payEach": Charge(Players.Where(x => x.Id != p.Id && !x.Bankrupt).Select(x => new Payment(p.Id, x.Id, c.Amount * Settings.Scale, "Card / Carta")), FinishMove); break;
            case "collectEach": Charge(Players.Where(x => x.Id != p.Id && !x.Bankrupt).Select(x => new Payment(x.Id, p.Id, c.Amount * Settings.Scale, "Card / Carta")), FinishMove); break;
            case "repairs":
                var cost = Deeds.Where(d => d.OwnerId == p.Id).Sum(d => d.Buildings == 5 ? c.HotelAmount : d.Buildings * c.Amount) * Settings.Scale;
                Charge(cost == 0 ? [] : [new(p.Id, null, cost, "Repairs / Riparazioni")], FinishMove); break;
        }
    }

    private void ReturnCard(string id) { var c = Cards.All.Single(x => x.Id == id); decks[c.Deck].Enqueue(c); }
    private void Credit(Player p, int amount, string en, string it)
    {
        p.Cash = checked(p.Cash + amount);
        Note($"{p.Name} receives {Money(amount)} ({en}).", $"{p.Name} riceve {Money(amount)} ({it}).");
    }

    private void Charge(IEnumerable<Payment> charges, Action continuation)
    {
        Require(payments.Count == 0, "Another payment is pending.");
        foreach (var charge in charges) payments.Enqueue(charge);
        afterPayments = continuation;
        ContinuePayments();
    }

    private void ContinuePayments()
    {
        while (payments.TryPeek(out var payment))
        {
            var p = Find(payment.From);
            if (p.Bankrupt) { payments.Dequeue(); continue; }
            if (p.Cash < payment.Amount) { Phase = "debt"; return; }
            p.Cash -= payment.Amount;
            if (payment.To != null && !Find(payment.To).Bankrupt) Find(payment.To).Cash = checked(Find(payment.To).Cash + payment.Amount);
            Note($"{p.Name} pays {Money(payment.Amount)} to {(payment.To == null ? "the bank" : Find(payment.To).Name)} ({PaymentReason(payment, false)}).", $"{p.Name} paga {Money(payment.Amount)} a {(payment.To == null ? "banca" : Find(payment.To).Name)} ({PaymentReason(payment, true)}).");
            payments.Dequeue();
        }
        var next = afterPayments; afterPayments = null; next?.Invoke();
    }

    private void Changed() => Revision++;
    private void Note(string en, string it)
    {
        Log.Add(new(Log.LastOrDefault()?.Sequence + 1 ?? 1, en, it, DateTimeOffset.UtcNow));
        if (Log.Count > 200) Log.RemoveAt(0);
    }
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
