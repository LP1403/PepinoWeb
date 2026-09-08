using GameServer.Models;
using GameServer.Services;

var logic = new GameLogicService();
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine($"PASS {name}"); }
void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true, name); return; } throw new Exception("Accepted: " + name); }
Card C(int v, string suit = "♠") => new(suit, v);
GameRoom Room(params int[][] hands)
{
    var room = new GameRoom { IsGameStarted = true, GameMode = CardService.MakeMode(1, hands.Length) };
    for (int i = 0; i < hands.Length; i++) room.Players.Add(new Player { ConnectionId = $"p{i}", Name = $"P{i}", Hand = hands[i].Select(v => C(v)).ToList(), IsCurrentTurn = i == 0 });
    room.FreeLeadPlayerId = "p0";
    return room;
}
PlayedCards Play(GameRoom r, int player, int value, int count = 1) => logic.Play(r, $"p{player}", r.Players[player].Hand.Where(c => c.Value == value).Take(count).Select(c => c.Id).ToList());

Check(CardService.CreateSpanishDeck().Count == 48, "48 cards per deck");
var deck = CardService.CreateMultipleDecks(3);
Check(deck.Select(c => c.Id).Distinct().Count() == 144, "multi-deck IDs unique");
Check(CardService.DealAllCards(deck, 2).hands.All(h => h.Count == 72), "largest hand: 72");
Check(CardService.FindPepinoOroPlayer(new() { new() { C(4) }, new() { C(3,"♦") }, new() { C(3,"♦") } }) == 1, "first seated gold three starts");
Check(CardService.GetCardValue(C(1)) > CardService.GetCardValue(C(12)), "ace highest");
Check(!CardService.ValidatePlay(new() { C(4), C(4) }, new() { C(5), C(5) }, false), "double four cannot beat double five");
Check(!CardService.ValidatePlay(new() { C(6) }, new() { C(5), C(5) }, false), "normal quantity enforced");
Check(!CardService.ValidatePlay(new() { C(5), C(6) }, null, true), "mixed groups rejected");

var r = Room(new[] { 2, 7, 8 }, new[] { 5, 6 });
r.FreeLeadPlayerId = null; r.LastPlayedCards = new() { C(5), C(5), C(5) }; r.LastPlayerId = "p1";
var wildcard = Play(r, 0, 2);
Check(wildcard.IsWildcard && r.CurrentTurnIndex == 0 && r.LastPlayedCards.Count == 0 && r.FreeLeadPlayerId == "p0", "single two over triple grants author free lead");
Reject(() => logic.Pass(r,"p0"), "cannot pass after wildcard");
Play(r,0,7); Check(r.CurrentTurnIndex == 1, "normal turn follows wildcard opening");

r = Room(new[] { 2 }, new[] { 4, 5 });
Play(r,0,2); Check(r.Players[0].HasWon && r.CurrentTurnIndex == 1 && r.FreeLeadPlayerId == "p1", "last wildcard: next active player opens");
r = Room(new[] { 4, 8 }, new[] { 4, 9 }, new[] { 5, 10 }, new[] { 6, 11 });
Play(r,0,4); var skip = Play(r,1,4);
Check(skip.IsPepineado && skip.SkippedPlayerId == "p2" && r.CurrentTurnIndex == 3 && r.Players[2].IsSkipped, "pepined victim is not next turn owner");
logic.Pass(r,"p3"); logic.Pass(r,"p0");
Check(r.CurrentTurnIndex == 1 && r.FreeLeadPlayerId == "p1" && r.LastPlayedCards.Count == 0, "full circuit frees last author");
Reject(() => logic.Pass(r,"p1"), "cannot pass a free round");

r = Room(new[] { 1 }, new[] { 4, 5 }, new[] { 6, 7 });
Play(r,0,1); logic.Pass(r,"p1"); logic.Pass(r,"p2");
Check(r.CurrentTurnIndex == 1 && r.FreeLeadPlayerId == "p1", "round can end when last author already won");

r = Room(new[] { 4, 5 }, new[] { 6, 7 });
var original = r.Players[0].Hand.Count;
Reject(() => logic.Play(r,"p0",new() { "foreign" }), "foreign card rejected");
var id = r.Players[0].Hand[0].Id;
Reject(() => logic.Play(r,"p0",new() { id,id }), "duplicate ID rejected");
Reject(() => logic.Play(r,"p1",new() { r.Players[1].Hand[0].Id }), "wrong turn rejected");
Check(r.Players[0].Hand.Count == original, "rejected actions do not change hand");
r.Players[1].IsConnected = false;
Reject(() => Play(r,0,4), "pause preserves hand during reconnect");

r = Room(new[] { 4 }, new[] { 5 });
Play(r,0,4); var final = Play(r,1,5);
Check(r.IsGameFinished && !r.IsGameStarted && r.Winners.Count == 2 && r.LastPlay == final && r.LastPlayedCards[0].Value == 5, "final winning play committed before game ends");

r = Room(new[] { 3 }, new[] { 4 }); r.IsGameStarted = false;
logic.StartGame(r); Check(r.Players.Sum(p => p.Hand.Count) == 48 && r.Players.Any(p => p.IsCurrentTurn && p.Hand.Any(c => c.Value == 3 && c.Suit == "♦")), "actual deal and first turn");
Reject(() => logic.StartGame(r), "cannot restart an active game");
r = Room(new[] { 3 }); r.IsGameStarted = false;
Reject(() => logic.StartGame(r), "minimum two enforced by server");
Console.WriteLine($"{passed} rule checks passed.");
