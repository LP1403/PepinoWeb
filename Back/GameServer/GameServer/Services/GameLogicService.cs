using GameServer.Models;

namespace GameServer.Services;

// Called while the room gate is held. All game transitions live here, never in a client.
public class GameLogicService
{
    public void StartGame(GameRoom room)
    {
        if (room.IsGameStarted) throw new InvalidOperationException("La partida ya está en curso.");
        if (room.Players.Count is < 2 or > 8 || room.Players.Any(p => !p.IsConnected))
            throw new InvalidOperationException("Se necesitan al menos 2 jugadores conectados.");
        if (room.GameMode == null) throw new InvalidOperationException("Elegí la cantidad de mazos.");
        room.GameMode = CardService.MakeMode(room.GameMode.DeckCount, room.Players.Count);
        ResetGame(room);
        var (hands, _) = CardService.DealAllCards(CardService.ShuffleDeck(CardService.CreateMultipleDecks(room.GameMode.DeckCount)), room.Players.Count);
        for (int i = 0; i < hands.Count; i++) room.Players[i].Hand = hands[i];
        room.IsGameStarted = true;
        room.GameStartedAt = DateTime.UtcNow;
        SetTurn(room, CardService.FindPepinoOroPlayer(hands));
        room.FreeLeadPlayerId = room.Players[room.CurrentTurnIndex].ConnectionId;
        room.Notice = "El Pepino de Oro abre la partida";
    }

    public PlayedCards Play(GameRoom room, string playerId, List<string> ids)
    {
        var player = RequireTurn(room, playerId);
        if (ids.Count == 0 || ids.Count != ids.Distinct().Count())
            throw new InvalidOperationException("Seleccioná cartas distintas de tu mano.");
        var cards = ids.Select(id => player.Hand.FirstOrDefault(c => c.Id == id)).ToList();
        if (cards.Any(c => c == null)) throw new InvalidOperationException("Una carta no pertenece a tu mano.");
        var actual = cards.Select(c => c!).ToList();
        var free = room.LastPlayedCards.Count == 0 || room.FreeLeadPlayerId == playerId;
        if (!CardService.ValidatePlay(actual, room.LastPlayedCards, free, free))
            throw new InvalidOperationException("Igualá la cantidad y jugá un valor igual o mayor, o un 2.");
        var play = new PlayedCards
        {
            Cards = actual, PlayerId = playerId, PlayerName = player.Name,
            IsWildcard = actual[0].Value == 2,
            IsPepineado = !free && CardService.IsPepineado(actual, room.LastPlayedCards),
            Sequence = ++room.PlaySequence
        };
        player.Hand.RemoveAll(c => ids.Contains(c.Id));
        room.TableCards.AddRange(actual);
        room.LastPlayedCards = actual;
        room.LastPlayerId = playerId;
        room.LastPlay = play;
        room.FreeLeadPlayerId = null;
        room.PassedPlayers.Clear();
        room.Notice = null;
        foreach (var p in room.Players) p.IsSkipped = false;
        if (player.Hand.Count == 0)
        {
            player.HasWon = true;
            room.Winners.Add(playerId);
        }
        if (room.Winners.Count >= room.GameMode!.MaxWinners || room.Players.All(p => p.HasWon))
        {
            room.IsGameStarted = false;
            room.IsGameFinished = true;
            foreach (var p in room.Players) p.IsCurrentTurn = false;
            return play;
        }
        if (play.IsWildcard)
        {
            // Confirmed Pepino rule: the author opens again; if finished, the next active seat opens.
            var next = player.HasWon ? NextActive(room, room.CurrentTurnIndex) : room.CurrentTurnIndex;
            OpenRound(room, next);
        }
        else
        {
            var next = NextActive(room, room.CurrentTurnIndex);
            if (play.IsPepineado)
            {
                var skipped = room.Players[next];
                skipped.IsSkipped = true;
                play.SkippedPlayerId = skipped.ConnectionId;
                play.SkippedPlayerName = skipped.Name;
                room.PassedPlayers.Add(skipped.ConnectionId);
                next = NextActive(room, next);
            }
            FinishAdvance(room, next);
        }
        return play;
    }

    public void Pass(GameRoom room, string playerId)
    {
        RequireTurn(room, playerId);
        if (room.LastPlayedCards.Count == 0 || room.FreeLeadPlayerId == playerId)
            throw new InvalidOperationException("Abrís una nueva ronda: tenés que jugar.");
        room.PassedPlayers.Add(playerId);
        room.Notice = null;
        foreach (var p in room.Players) p.IsSkipped = false;
        FinishAdvance(room, NextActive(room, room.CurrentTurnIndex));
    }

    private static Player RequireTurn(GameRoom room, string id)
    {
        if (!room.IsGameStarted) throw new InvalidOperationException("La partida no está en curso.");
        if (room.Players.Any(p => !p.IsConnected)) throw new InvalidOperationException("Esperando la reconexión de un jugador.");
        var player = room.Players[room.CurrentTurnIndex];
        if (player.ConnectionId != id || player.HasWon) throw new InvalidOperationException("No es tu turno.");
        return player;
    }
    private static int NextActive(GameRoom room, int from)
    {
        for (int step = 1; step <= room.Players.Count; step++)
        {
            var i = (from + step) % room.Players.Count;
            if (!room.Players[i].HasWon) return i;
        }
        throw new InvalidOperationException("No quedan jugadores activos.");
    }
    private static void FinishAdvance(GameRoom room, int next)
    {
        if (room.Players[next].ConnectionId == room.LastPlayerId ||
            room.Players.Where(p => !p.HasWon).All(p => room.PassedPlayers.Contains(p.ConnectionId)))
            OpenRound(room, next);
        else SetTurn(room, next);
    }
    private static void SetTurn(GameRoom room, int next)
    {
        foreach (var p in room.Players) p.IsCurrentTurn = false;
        room.CurrentTurnIndex = next;
        room.Players[next].IsCurrentTurn = true;
    }
    private static void OpenRound(GameRoom room, int next)
    {
        SetTurn(room, next);
        room.LastPlayedCards = new();
        room.FreeLeadPlayerId = room.Players[next].ConnectionId;
        room.PassedPlayers.Clear();
        room.RoundNumber++;
        room.Notice = "Nueva ronda · Juega libremente";
    }
    public void ResetGame(GameRoom room)
    {
        room.IsGameStarted = false;
        room.IsGameFinished = false;
        room.TableCards.Clear(); room.LastPlayedCards = new(); room.Deck.Clear();
        room.LastPlay = null; room.LastPlayerId = null; room.FreeLeadPlayerId = null;
        room.Winners.Clear(); room.PassedPlayers.Clear();
        room.CurrentTurnIndex = 0; room.RoundNumber = 1;
        room.Notice = null;
        foreach (var p in room.Players)
        {
            p.Hand.Clear(); p.HasWon = false; p.IsSkipped = false; p.IsCurrentTurn = false;
        }
    }
}
