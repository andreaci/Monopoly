using Monopoly.Server.Data;
using Monopoly.Server.Domain;

namespace Monopoly.Server.GameEngine;

public sealed partial class Engine
{
    private void RequestAuction(Player visitor)
    {
        var d = Deed(visitor.Position);
        Require(d.OwnerId != null && d.OwnerId != visitor.Id && Square(d).Type == "street" && ClearGroup(d), "This street cannot be auctioned.");
        Auction = new() { SquareId = d.SquareId, OwnerId = d.OwnerId!, VisitorId = visitor.Id };
        Phase = "consent";
        Note($"{visitor.Name} proposes an auction for {Square(d).En}.", $"{visitor.Name} propone un'asta per {Square(d).It}.");
    }

    private void Consent(Player owner, Command command)
    {
        if (!command.Accept)
        {
            Note($"{owner.Name} refuses the auction.", $"{owner.Name} rifiuta l'asta.");
            Auction = null; SettleRent(); return;
        }
        Require(command.Amount > 0 && command.Amount <= 100_000_000, "Choose a positive opening bid.");
        Auction!.OpeningBid = command.Amount;
        Auction.EndsAt = DateTimeOffset.UtcNow.AddSeconds(25);
        Phase = "auction";
        UpdateAuctionPause(DateTimeOffset.UtcNow);
        Note($"The auction opens at {Money(command.Amount)}.", $"L'asta parte da {Money(command.Amount)}.");
    }

    private void Bid(Player player, int amount)
    {
        var a = Auction!;
        Require(DateTimeOffset.UtcNow < a.EndsAt, "The auction has ended.");
        Require(amount >= a.OpeningBid && amount > a.HighestBid && amount <= player.Cash, "Bid must exceed the current bid and fit your available cash.");
        a.HighestBid = amount; a.BidderId = player.Id;
        if ((a.EndsAt - DateTimeOffset.UtcNow).TotalSeconds < 8) a.EndsAt = DateTimeOffset.UtcNow.AddSeconds(8);
        Note($"{player.Name} bids {Money(amount)}.", $"{player.Name} offre {Money(amount)}.");
    }

    private bool UpdateAuctionPause(DateTimeOffset now)
    {
        if (Phase != "auction" || Auction == null) return false;
        var disconnected = Players.Any(p => !p.Bankrupt && !p.Connected);
        if (disconnected == Auction.Paused) return false;
        if (disconnected) Auction.RemainingSeconds = Math.Max(0, (Auction.EndsAt - now).TotalSeconds);
        else Auction.EndsAt = now.AddSeconds(Auction.RemainingSeconds);
        Auction.Paused = disconnected;
        return true;
    }

    private void CompleteAuction()
    {
        var a = Auction!; var d = Deed(a.SquareId);
        var fees = new List<Payment>();
        if (a.BidderId != null && a.BidderId != a.OwnerId)
        {
            var buyer = Find(a.BidderId); var owner = Find(a.OwnerId);
            buyer.Cash -= a.HighestBid; owner.Cash = checked(owner.Cash + a.HighestBid);
            RecordPlayerPayment(buyer, owner, a.HighestBid);
            AddMoneyEvent(buyer.Id, -a.HighestBid); AddMoneyEvent(owner.Id, a.HighestBid); d.OwnerId = buyer.Id;
            if (d.Mortgaged) fees.Add(new(buyer.Id, null, Square(d).Price * Settings.Scale / 20, "Mortgage transfer / Trasferimento ipoteca"));
            Note($"{buyer.Name} wins {Square(d).En} for {Money(a.HighestBid)}.", $"{buyer.Name} vince {Square(d).It} per {Money(a.HighestBid)}.");
        }
        else Note("The auction ends without a change of owner.", "L'asta termina senza cambio di proprietario.");
        Auction = null;
        if (fees.Count > 0) Charge(fees, SettleRent); else SettleRent();
    }

    private void Building(Player player, int squareId, bool sell)
    {
        var d = Deed(squareId); var s = Square(d);
        Require(d.OwnerId == player.Id && s.Type == "street", "You must own this street.");
        Require(!Settings.BuildingsOnlyWhenPresent || (player.Id == Active?.Id && (Phase is "ready" or "end" or "debt") && player.Position == squareId), "You must be standing on this street during your turn to manage its buildings.");
        var group = Group(d).ToArray();
        if (sell)
        {
            Require(d.Buildings > 0 && d.Buildings == group.Max(x => x.Buildings), "Sell buildings evenly across the color group.");
            Require(d.Buildings != 5 || HousesLeft >= 4, "Four bank houses are required to downgrade a hotel.");
            d.Buildings--; var proceeds = s.BuildCost * Settings.Scale / 2; player.Cash = checked(player.Cash + proceeds); AddMoneyEvent(player.Id, proceeds);
        }
        else
        {
            Require(Phase != "debt", "Settle your debt before buying buildings.");
            Require(FullGroup(d) && group.All(x => !x.Mortgaged), "Own the entire unmortgaged color group first.");
            Require(d.Buildings < 5 && d.Buildings == group.Min(x => x.Buildings), "Build evenly across the color group.");
            Require(player.Cash >= s.BuildCost * Settings.Scale, "Not enough cash.");
            Require(d.Buildings == 4 ? HotelsLeft > 0 : HousesLeft > 0, "The bank has no available buildings.");
            var cost = s.BuildCost * Settings.Scale; player.Cash -= cost; AddMoneyEvent(player.Id, -cost); d.Buildings++;
        }
        Note($"{player.Name} {(sell ? "sells" : "adds")} a building on {s.En}.", $"{player.Name} {(sell ? "vende" : "costruisce")} un edificio su {s.It}.");
        if (Phase == "debt") ContinuePayments();
    }

    private void SellGroup(Player p, int squareId)
    {
        var d = Deed(squareId);
        Require(Square(d).Type == "street" && d.OwnerId == p.Id, "Choose your own street.");
        Require(!Settings.BuildingsOnlyWhenPresent || (p.Id == Active?.Id && (Phase is "ready" or "end" or "debt") && p.Position == squareId), "You must be standing on this street during your turn to manage its buildings.");
        var group = Group(d).ToArray();
        Require(group.All(x => x.OwnerId == p.Id) && group.Any(x => x.Buildings > 0), "There are no buildings to sell in this group.");
        var value = group.Sum(x => x.Buildings * Square(x).BuildCost * Settings.Scale / 2);
        foreach (var property in group) property.Buildings = 0;
        p.Cash = checked(p.Cash + value);
        if (value != 0) AddMoneyEvent(p.Id, value);
        Note($"{p.Name} sells all buildings in the color group for {Money(value)}.", $"{p.Name} vende tutti gli edifici del gruppo per {Money(value)}.");
        if (Phase == "debt") ContinuePayments();
    }

    private void Mortgage(Player p, int squareId, bool redeem)
    {
        var d = Deed(squareId); var s = Square(d);
        Require(d.OwnerId == p.Id && ClearGroup(d), "Own this deed and sell the group's buildings first.");
        Require(d.Mortgaged == redeem, redeem ? "This deed is not mortgaged." : "This deed is already mortgaged.");
        var principal = s.Price * Settings.Scale / 2;
        if (redeem)
        {
            Require(Phase != "debt", "Settle your debt before redeeming mortgages.");
            var cost = principal + s.Price * Settings.Scale / 20;
            Require(p.Cash >= cost, "Not enough cash to redeem the mortgage."); p.Cash -= cost; AddMoneyEvent(p.Id, -cost);
        }
        else { p.Cash = checked(p.Cash + principal); AddMoneyEvent(p.Id, principal); }
        d.Mortgaged = !redeem;
        Note($"{p.Name} {(redeem ? "redeems" : "mortgages")} {s.En}.", $"{p.Name} {(redeem ? "riscatta" : "ipoteca")} {s.It}.");
        if (Phase == "debt") ContinuePayments();
    }

    private bool CanRaiseCash(Player p)
    {
        var owned = Deeds.Where(d => d.OwnerId == p.Id).ToArray();
        return owned.Any(d => d.Buildings > 0) || (Settings.Mortgages && owned.Any(d => !d.Mortgaged && ClearGroup(d)));
    }

    private void OfferTrade(Player p, Trade offer)
    {
        var recipient = Find(offer.To);
        Require(recipient.Id != p.Id && !recipient.Bankrupt, "Choose another active player.");
        Require(recipient.Connected, "Wait for the recipient to reconnect.");
        offer.From = p.Id;
        ValidateTrade(offer);
        Trade = offer;
        Note($"{p.Name} proposes a trade to {recipient.Name}.", $"{p.Name} propone uno scambio a {recipient.Name}.");
    }

    private int TransferFee(IEnumerable<int> ids) => ids.Select(Deed).Where(d => d.Mortgaged).Sum(d => Square(d).Price * Settings.Scale / 20);
    private void ValidateTrade(Trade t)
    {
        var from = Find(t.From); var to = Find(t.To);
        Require(t.OfferCash >= 0 && t.RequestCash >= 0 && t.OfferCash <= from.Cash && t.RequestCash <= to.Cash, "Invalid trade cash amounts.");
        Require(t.OfferDeeds.Length <= 28 && t.RequestDeeds.Length <= 28 && t.OfferDeeds.Distinct().Count() == t.OfferDeeds.Length && t.RequestDeeds.Distinct().Count() == t.RequestDeeds.Length, "Invalid deed selection.");
        Require(t.OfferJailCards >= 0 && t.OfferJailCards <= from.JailCards.Count && t.RequestJailCards >= 0 && t.RequestJailCards <= to.JailCards.Count, "Invalid jail card selection.");
        foreach (var id in t.OfferDeeds) Require(Deed(id).OwnerId == from.Id && ClearGroup(Deed(id)), "Offered deeds must be yours and have no buildings in their group.");
        foreach (var id in t.RequestDeeds) Require(Deed(id).OwnerId == to.Id && ClearGroup(Deed(id)), "Requested deeds must belong to the recipient and have no buildings in their group.");
        if (Settings.TradingOnlyWhenOccupied)
            foreach (var id in t.OfferDeeds.Concat(t.RequestDeeds))
                Require(Players.Any(player => !player.Bankrupt && player.Position == id), "A player must be standing on a deed before it can be traded.");
        Require(from.Cash - t.OfferCash + t.RequestCash >= TransferFee(t.RequestDeeds) && to.Cash - t.RequestCash + t.OfferCash >= TransferFee(t.OfferDeeds), "Each recipient must cover mortgage transfer interest.");
        Require(t.OfferCash + t.RequestCash + t.OfferDeeds.Length + t.RequestDeeds.Length + t.OfferJailCards + t.RequestJailCards > 0, "Choose something to trade.");
    }

    private void RespondTrade(Player p, bool accept)
    {
        var t = Trade!;
        if (!accept) { Trade = null; Note("Trade declined.", "Scambio rifiutato."); return; }
        ValidateTrade(t);
        var from = Find(t.From); var to = Find(t.To);
        var fromChange = -t.OfferCash + t.RequestCash - TransferFee(t.RequestDeeds);
        var toChange = -t.RequestCash + t.OfferCash - TransferFee(t.OfferDeeds);
        from.Cash = checked(from.Cash + fromChange);
        to.Cash = checked(to.Cash + toChange);
        RecordPlayerPayment(from, to, t.OfferCash);
        RecordPlayerPayment(to, from, t.RequestCash);
        AddMoneyEvent(from.Id, -t.OfferCash);
        AddMoneyEvent(to.Id, t.OfferCash);
        AddMoneyEvent(to.Id, -t.RequestCash);
        AddMoneyEvent(from.Id, t.RequestCash);
        AddMoneyEvent(from.Id, -TransferFee(t.RequestDeeds));
        AddMoneyEvent(to.Id, -TransferFee(t.OfferDeeds));
        foreach (var id in t.OfferDeeds) Deed(id).OwnerId = to.Id;
        foreach (var id in t.RequestDeeds) Deed(id).OwnerId = from.Id;
        var offered = from.JailCards.Take(t.OfferJailCards).ToArray(); var requested = to.JailCards.Take(t.RequestJailCards).ToArray();
        foreach (var id in offered) from.JailCards.Remove(id);
        foreach (var id in requested) to.JailCards.Remove(id);
        from.JailCards.AddRange(requested); to.JailCards.AddRange(offered);
        Trade = null;
        Note($"{from.Name} and {to.Name} complete their trade.", $"{from.Name} e {to.Name} completano lo scambio.");
        if (Phase == "debt") ContinuePayments();
    }

    private void Bankruptcy(Player p)
    {
        var debt = Debt!;
        var creditor = debt.To == null ? null : Find(debt.To);
        var transferFees = new List<Payment>();
        var liquidation = 0;
        foreach (var d in Deeds.Where(d => d.OwnerId == p.Id))
        {
            if (d.Buildings > 0) { var proceeds = (d.Buildings == 5 ? 5 : d.Buildings) * Square(d).BuildCost * Settings.Scale / 2; p.Cash = checked(p.Cash + proceeds); liquidation += proceeds; }
            d.Buildings = 0;
            d.OwnerId = creditor?.Id;
            if (creditor == null) d.Mortgaged = false;
            else if (d.Mortgaged) transferFees.Add(new(creditor.Id, null, Square(d).Price * Settings.Scale / 20, "Mortgage transfer / Trasferimento ipoteca"));
        }
        if (liquidation != 0) AddMoneyEvent(p.Id, liquidation);
        if (creditor != null)
        {
            RecordPlayerPayment(p, creditor, p.Cash);
            if (p.Cash != 0) { AddMoneyEvent(p.Id, -p.Cash); AddMoneyEvent(creditor.Id, p.Cash); }
            creditor.Cash = checked(creditor.Cash + p.Cash); creditor.JailCards.AddRange(p.JailCards);
        }
        else foreach (var id in p.JailCards) ReturnCard(id);
        p.JailCards.Clear(); p.Cash = 0; p.Bankrupt = true; p.InJail = false;
        Note($"{p.Name} is bankrupt.", $"{p.Name} è in bancarotta.");
        payments.Dequeue();
        // A bankrupt player cannot pay later installments of a multi-player card.
        var remaining = payments.Where(x => x.From != p.Id).ToArray(); payments.Clear();
        foreach (var payment in transferFees.Concat(remaining)) payments.Enqueue(payment);
        var solvent = Players.Where(x => !x.Bankrupt).ToArray();
        if (solvent.Length == 1)
        {
            WinnerId = solvent[0].Id; Phase = "finished"; payments.Clear(); afterPayments = null;
            Note($"{solvent[0].Name} wins!", $"{solvent[0].Name} vince!"); return;
        }
        var wasActive = p.Id == Active!.Id;
        if (wasActive)
        {
            afterPayments = () => { extraRoll = false; EndTurn(); };
        }
        ContinuePayments();
    }
}
