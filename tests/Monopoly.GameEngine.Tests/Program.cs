using Monopoly.Server.Data;
using Monopoly.Server.Domain;
using Monopoly.Server.GameEngine;

var passed = 0;
var failed = 0;
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}
static void Equal<T>(T actual, T expected) { if (!Equals(actual, expected)) throw new Exception($"Expected {expected}, got {actual}"); }
static void Reject(Action action)
{
    try { action(); } catch (InvalidOperationException) { return; }
    throw new Exception("Expected the action to be rejected.");
}
static Command Cmd(string type, int square = 0, int amount = 0, bool accept = false) => new() { Id = Guid.NewGuid().ToString(), Type = type, SquareId = square, Amount = amount, Accept = accept };
static Engine Game(int count = 2, Settings? settings = null, int[]? dice = null, Card? firstCard = null)
{
    var values = new Queue<int>(dice ?? [1, 2]);
    var engine = new Engine(() => values.Count > 0 ? values.Dequeue() : 1,
        Cards.All.OrderBy(c => c.Id == (firstCard?.Id ?? "cc02") ? 0 : 1));
    if (settings != null) engine.Configure(settings);
    for (var i = 0; i < count; i++) { var p = engine.Join($"Player {i + 1}", Board.Tokens[i]); engine.Presence(p.Id, true); }
    engine.Start(); return engine;
}
static void Roll(Engine e)
{
    e.Execute(e.Active!.Id, Cmd("roll")); e.Tick(DateTimeOffset.UtcNow.AddSeconds(4));
}
static Deed D(Engine e, int id) => e.Deeds.Single(d => d.SquareId == id);

Test("Lobby validates names, tokens and player limit", () => {
    var e = new Engine(); var p = e.Join("Alice", "car");
    Reject(()=>e.Join("alice", "hat")); Reject(()=>e.Join("Bob", "car")); Reject(()=>e.Start());
    for(var i=1;i<6;i++) e.Join($"P{i}", Board.Tokens[i]);
    Reject(()=>e.Join("Overflow","hat"));
    Reject(()=>e.Start());
    foreach(var player in e.Players) e.Presence(player.Id,true);
    e.Start(); Equal(e.Phase,"ready"); Reject(()=>e.Configure(new()));
});
Test("Only the active player can roll, and duplicate commands are harmless", () => {
    var e=Game(); Reject(()=>e.Execute(e.Players[1].Id,Cmd("roll")));
    var c=Cmd("roll"); e.Execute(e.Active!.Id,c); var revision=e.Revision;
    e.Execute(e.Active.Id,c); Equal(e.Revision,revision); Equal(e.Phase,"rolling");
    Reject(()=>e.Execute(e.Active.Id,Cmd("roll")));
});
Test("Passing GO and card credit use server money", () => {
    var e=Game(); e.Active!.Position=39; Roll(e);
    Equal(e.Active.Cash,1900); Equal(e.Active.Position,2); Equal(e.Phase,"end");
});
Test("Purchase transfers cash and deed; declining leaves bank ownership", () => {
    var e=Game(); Roll(e); Equal(e.Phase,"purchase"); e.Execute(e.Active!.Id,Cmd("buy"));
    Equal(D(e,3).OwnerId,e.Active.Id); Equal(e.Active.Cash,1440);
    var other=Game(); other.Active!.Position=3; Roll(other); other.Execute(other.Active.Id,Cmd("decline")); Equal(D(other,6).OwnerId,null);
});
Test("Three doubles send a player to jail", () => {
    var e=Game(dice:[1,1,1,1,1,1]); Roll(e); e.Execute(e.Active!.Id,Cmd("endTurn"));
    Roll(e); e.Execute(e.Active!.Id,Cmd("endTurn")); Roll(e);
    Equal(e.Active!.Position,10); Equal(e.Active.InJail,true); Equal(e.Phase,"end");
});
Test("Disabled jail moves to its square without detention", () => {
    var e=Game(settings:new(){Jail=false},dice:[1,1]); e.Active!.Position=28; Roll(e);
    Equal(e.Active.Position,10); Equal(e.Active.InJail,false); e.Execute(e.Active.Id,Cmd("endTurn")); Equal(e.Active.Id,e.Players[1].Id);
});
Test("Three failed jail rolls require payment before moving", () => {
    var e=Game(dice:[1,2,1,2,1,2,1,2,1,2]); var jailed=e.Players[0]; jailed.InJail=true; jailed.Position=10;
    for(var i=0;i<2;i++) { Roll(e); Equal(jailed.InJail,true); e.Execute(jailed.Id,Cmd("endTurn")); Roll(e); if(e.Phase=="purchase") e.Execute(e.Active!.Id,Cmd("decline")); e.Execute(e.Active!.Id,Cmd("endTurn")); }
    Roll(e); Equal(jailed.InJail,false); Equal(jailed.Position,13); Equal(jailed.Cash,1450);
});

static Engine AuctionGame(int count=3)
{
    var e=Game(count); e.Active!.Position=18; D(e,21).OwnerId=e.Players[1].Id; Roll(e);
    e.Execute(e.Active.Id,Cmd("requestAuction")); return e;
}
Test("Owner refusal charges the original owner rent", () => {
    var e=AuctionGame(); e.Execute(e.Players[1].Id,Cmd("auctionConsent",accept:false));
    Equal(e.Players[0].Cash,1482); Equal(e.Players[1].Cash,1518); Equal(e.Auction,null);
});
Test("Visitor wins auction and pays no rent", () => {
    var e=AuctionGame(); e.Execute(e.Players[1].Id,Cmd("auctionConsent",amount:20,accept:true));
    e.Execute(e.Players[0].Id,Cmd("bid",amount:50)); e.Tick(DateTimeOffset.UtcNow.AddSeconds(30));
    Equal(D(e,21).OwnerId,e.Players[0].Id); Equal(e.Players[0].Cash,1450); Equal(e.Players[1].Cash,1550);
});
Test("Third party wins and receives visitor's rent after sale", () => {
    var e=AuctionGame(); e.Execute(e.Players[1].Id,Cmd("auctionConsent",amount:20,accept:true));
    e.Execute(e.Players[2].Id,Cmd("bid",amount:80)); e.Tick(DateTimeOffset.UtcNow.AddSeconds(30));
    Equal(D(e,21).OwnerId,e.Players[2].Id); Equal(e.Players[0].Cash,1482); Equal(e.Players[1].Cash,1580); Equal(e.Players[2].Cash,1438);
});
Test("No-bid auction retains original ownership and settles rent", () => {
    var e=AuctionGame(); e.Execute(e.Players[1].Id,Cmd("auctionConsent",amount:20,accept:true)); e.Tick(DateTimeOffset.UtcNow.AddSeconds(30));
    Equal(D(e,21).OwnerId,e.Players[1].Id); Equal(e.Players[0].Cash,1482);
});
Test("Disconnected bidder freezes the auction", () => {
    var e=AuctionGame(); e.Execute(e.Players[1].Id,Cmd("auctionConsent",amount:20,accept:true));
    e.Presence(e.Players[2].Id,false); Equal(e.Auction!.Paused,true); e.Tick(DateTimeOffset.UtcNow.AddMinutes(1)); Equal(e.Phase,"auction");
    e.Presence(e.Players[2].Id,true); Equal(e.Auction.Paused,false); Equal(e.Phase,"auction");
});
Test("Auctions cannot be requested on unowned property", () => {
    var e=Game(); Roll(e); Reject(()=>e.Execute(e.Active!.Id,Cmd("requestAuction")));
});
Test("Disconnected active player must reconnect before rolling", () => {
    var e=Game(); e.Presence(e.Active!.Id,false); Equal(e.WaitingFor,e.Active.Id); Reject(()=>e.Execute(e.Active.Id,Cmd("roll")));
    e.Presence(e.Active.Id,true); Roll(e); Equal(e.Phase,"purchase");
});
Test("Even construction and mortgage restrictions", () => {
    var e=Game(); var p=e.Active!; D(e,1).OwnerId=p.Id; D(e,3).OwnerId=p.Id;
    e.Execute(p.Id,Cmd("build",1)); Equal(e.HousesLeft,31);
    Reject(()=>e.Execute(p.Id,Cmd("build",1))); Reject(()=>e.Execute(p.Id,Cmd("mortgage",3)));
    e.Execute(p.Id,Cmd("build",3)); e.Execute(p.Id,Cmd("sellBuilding",1));
    Reject(()=>e.Execute(p.Id,Cmd("sellBuilding",1))); e.Execute(p.Id,Cmd("sellBuilding",3));
    e.Execute(p.Id,Cmd("mortgage",1)); Equal(D(e,1).Mortgaged,true);
    var cash=p.Cash; e.Execute(p.Id,Cmd("unmortgage",1)); Equal(p.Cash,cash-33);
});
Test("Bank house supply caps construction; whole-group sale releases hotels", () => {
    var e=Game(); var p=e.Active!; foreach(var d in e.Deeds.Where(d=>Board.Squares[d.SquareId].Type=="street")) d.OwnerId=p.Id;
    foreach(var d in e.Deeds.Where(d=>Board.Squares[d.SquareId].Type=="street").Take(8)) d.Buildings=4;
    Equal(e.HousesLeft,0); Reject(()=>e.Execute(p.Id,Cmd("build",16)));
    D(e,1).Buildings=5; D(e,3).Buildings=5; var cash=p.Cash;
    e.Execute(p.Id,Cmd("sellGroup",1)); Equal(D(e,1).Buildings,0); Equal(D(e,3).Buildings,0); Equal(p.Cash,cash+250);
});
Test("Optional rules reject disabled actions", () => {
    var e=Game(settings:new(){Buildings=false,Mortgages=false,Trading=false,Auctions=false}); D(e,1).OwnerId=e.Active!.Id;
    foreach(var type in new[]{"build","sellBuilding","mortgage","unmortgage","tradeOffer"}) Reject(()=>e.Execute(e.Active.Id,Cmd(type,1)));
});
Test("Trading is atomic and requires recipient consent", () => {
    var e=Game(); D(e,1).OwnerId=e.Players[0].Id; D(e,3).OwnerId=e.Players[1].Id;
    var offer=Cmd("tradeOffer"); offer.Trade=new(){To=e.Players[1].Id,OfferCash=10,OfferDeeds=[1],RequestDeeds=[3]};
    e.Execute(e.Players[0].Id,offer); Equal(D(e,1).OwnerId,e.Players[0].Id);
    Reject(()=>e.Execute(e.Players[0].Id,Cmd("tradeRespond",accept:true)));
    e.Execute(e.Players[1].Id,Cmd("tradeRespond",accept:true));
    Equal(D(e,1).OwnerId,e.Players[1].Id); Equal(D(e,3).OwnerId,e.Players[0].Id); Equal(e.Players[0].Cash,1490); Equal(e.Players[1].Cash,1510);
});
Test("Trade pays mortgage transfer interest", () => {
    var e=Game(); D(e,1).OwnerId=e.Players[0].Id; D(e,1).Mortgaged=true;
    var c=Cmd("tradeOffer"); c.Trade=new(){To=e.Players[1].Id,OfferDeeds=[1]}; e.Execute(e.Players[0].Id,c); e.Execute(e.Players[1].Id,Cmd("tradeRespond",accept:true));
    Equal(e.Players[1].Cash,1497); Equal(D(e,1).Mortgaged,true);
});
Test("Mortgage settles an outstanding payment without replaying it", () => {
    var e=Game(dice:[1,3]); var p=e.Active!; p.Cash=190; D(e,39).OwnerId=p.Id; Roll(e);
    Equal(e.Phase,"debt"); e.Execute(p.Id,Cmd("mortgage",39)); Equal(p.Cash,190); Equal(e.Phase,"end"); Equal(e.Debt,null);
});
Test("Bankruptcy transfers assets and detects a winner", () => {
    var e=Game(settings:new(){Mortgages=false}); D(e,3).OwnerId=e.Players[1].Id; D(e,1).OwnerId=e.Players[0].Id; e.Players[0].Cash=0;
    Roll(e); e.Execute(e.Players[0].Id,Cmd("payRent")); Equal(e.Phase,"debt");
    e.Execute(e.Players[0].Id,Cmd("bankrupt")); Equal(e.Phase,"finished"); Equal(e.WinnerId,e.Players[1].Id); Equal(D(e,1).OwnerId,e.Players[1].Id);
});
Test("Italian classic monetary scale applies to purchases", () => {
    var e=Game(settings:new(){Language="it-GBP",StartingCash=150000,GoPayment=20000}); Roll(e); e.Execute(e.Active!.Id,Cmd("buy")); Equal(e.Active.Cash,144000);
});
Test("Nearest utility card uses a fresh server roll for rent", () => {
    var e=Game(dice:[1,2,2,3],firstCard:Cards.All.Single(c=>c.Id=="ch07")); e.Active!.Position=4; D(e,12).OwnerId=e.Players[1].Id;
    Roll(e); Equal(e.Phase,"rolling"); Equal(e.Roll!.Utility,true); e.Tick(DateTimeOffset.UtcNow.AddSeconds(4)); Equal(e.Active.Cash,1450); Equal(e.Players[1].Cash,1550);
});
Test("Utility rent waits two seconds after resolution before advancing the turn", () => {
    var e=Game(dice:[1,2,2,3],firstCard:Cards.All.Single(c=>c.Id=="ch07"));
    var payer=e.Active!; payer.Position=4; D(e,12).OwnerId=e.Players[1].Id;
    Roll(e);
    var resolvedAt=DateTimeOffset.UtcNow.AddSeconds(4);
    e.Tick(resolvedAt);
    Equal(payer.Cash,1450); Equal(e.Players[1].Cash,1550); Equal(e.Active!.Id,payer.Id); Equal(e.Phase,"end");
    e.Tick(resolvedAt.AddMilliseconds(1999)); Equal(e.Active!.Id,payer.Id);
    e.Tick(resolvedAt.AddSeconds(2)); Equal(e.Active!.Id,e.Players[1].Id); Equal(e.Phase,"ready");
});
Test("Both complete decks have unique stable IDs", () => {
    Equal(Cards.All.Length,32); Equal(Cards.All.Select(c=>c.Id).Distinct().Count(),32);
    Equal(Cards.All.Count(c=>c.Deck=="chance"),16); Equal(Cards.All.Count(c=>c.Deck=="chest"),16);
    Equal(Board.Squares.Length,40); Equal(Board.Squares.Count(s=>s.Price>0&&s.Type!="tax"),28);
});
foreach(var card in Cards.All)
{
    Test($"Card {card.Id}: {card.Effect}", () => {
        var e=Game(firstCard:card); var p=e.Active!; p.Cash=10000; e.Players[1].Cash=10000;
        p.Position=card.Deck=="chance" ? 4 : 39;
        Roll(e);
        if(e.LastCard?.Id != card.Id) throw new Exception("Wrong card drawn");
        switch(card.Effect)
        {
            case "move": Equal(p.Position,card.Target); break;
            case "jail": Equal(p.Position,10); Equal(p.InJail,true); break;
            case "jailCard": Equal(p.JailCards.Single(),card.Id); break;
            case "receive": Equal(p.Cash,10000+card.Amount+(card.Deck=="chest"?200:0)); break;
            case "pay": Equal(p.Cash,10000-card.Amount+(card.Deck=="chest"?200:0)); break;
            case "payEach": Equal(p.Cash,10000-card.Amount); Equal(e.Players[1].Cash,10000+card.Amount); break;
            case "collectEach": Equal(p.Cash,10200+card.Amount); Equal(e.Players[1].Cash,10000-card.Amount); break;
            case "repairs": Equal(p.Cash,card.Deck=="chest"?10200:10000); break;
            case "rail": Equal(p.Position,15); break;
            case "utility": Equal(p.Position,12); break;
            case "back": Equal(p.Position,4); Equal(p.Cash,9800); break;
        }
    });
}

Test("A complete automated match reaches a winner", () => {
    var random = new Random(571);
    var e = new Engine(()=>random.Next(1,7), Cards.All);
    // A finite cash pool makes this automated policy end without human trading.
    e.Configure(new(){StartingCash=100,GoPayment=0,Auctions=false,Trading=false,Buildings=false});
    foreach(var (name,token) in new[]{("A","car"),("B","hat"),("C","dog")}) { var p=e.Join(name,token); e.Presence(p.Id,true); }
    e.Start();
    for(var step=0;step<20000 && e.Phase!="finished";step++)
    {
        switch(e.Phase)
        {
            case "ready": Roll(e); break;
            case "rolling": e.Tick(DateTimeOffset.UtcNow.AddSeconds(4)); break;
            case "purchase": e.Execute(e.Active!.Id,Cmd(e.Active.Cash >= Board.Squares[e.Active.Position].Price ? "buy":"decline")); break;
            case "rent": e.Execute(e.Active!.Id,Cmd("payRent")); break;
            case "end": e.Execute(e.Active!.Id,Cmd("endTurn")); break;
            case "debt":
                var debtor=e.Find(e.Debt!.From);
                var deed=e.Deeds.FirstOrDefault(d=>d.OwnerId==debtor.Id&&!d.Mortgaged);
                e.Execute(debtor.Id, deed==null ? Cmd("bankrupt") : Cmd("mortgage",deed.SquareId)); break;
            default: throw new Exception($"Unexpected phase {e.Phase}");
        }
        if(e.Players.Any(p=>p.Cash<0)) throw new Exception("A cash balance became negative");
    }
    Equal(e.Phase,"finished"); Equal(e.Players.Count(p=>!p.Bankrupt),1);
});
Test("Birthday payment can bankrupt a non-active player and resume the card",()=>{
    var e=Game(3,settings:new(){Mortgages=false},firstCard:Cards.All.Single(c=>c.Id=="cc09"));
    e.Active!.Position=39; e.Players[1].Cash=0; Roll(e); Equal(e.Debt!.From,e.Players[1].Id);
    e.Execute(e.Players[1].Id,Cmd("bankrupt")); Equal(e.Active.Id,e.Players[0].Id); Equal(e.Phase,"end"); Equal(e.Players[2].Cash,1490);
});
Test("Repair cards charge per house and hotel",()=>{
    var e=Game(firstCard:Cards.All.Single(c=>c.Id=="ch12")); var p=e.Active!; p.Position=4;
    D(e,1).OwnerId=p.Id; D(e,1).Buildings=3; D(e,3).OwnerId=p.Id; D(e,3).Buildings=5;
    Roll(e); Equal(p.Cash,1325);
});
Test("An unresolved trade prevents another roll",()=>{
    var e=Game(); var c=Cmd("tradeOffer"); c.Trade=new(){To=e.Players[1].Id,OfferCash=10}; e.Execute(e.Active!.Id,c);
    Reject(()=>e.Execute(e.Active.Id,Cmd("roll"))); e.Execute(e.Active.Id,Cmd("tradeCancel")); Roll(e); Equal(e.Phase,"purchase");
});

Console.WriteLine($"\n{passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;
