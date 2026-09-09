using GameServer.Models;
using GameServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameServer.Hubs;

public class GameHub(GameRoomManager manager, IHubContext<GameHub> hubContext) : Hub
{
    private readonly GameLogicService logic = new();

    private async Task InRoom(string id, Func<GameRoom, Task> action)
    {
        var room = manager.GetRoom(id);
        if (room == null) throw new HubException("Sala no encontrada.");
        await room.Gate.WaitAsync();
        try { await action(room); }
        catch (InvalidOperationException e) { throw new HubException(e.Message); }
        finally { room.Gate.Release(); }
    }
    private Player Member(GameRoom room) => room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)
        ?? throw new InvalidOperationException("No pertenecés a esta sala.");
    private void Owner(GameRoom room)
    {
        Member(room);
        if (room.CreatedBy != Context.ConnectionId) throw new InvalidOperationException("Solo el creador puede hacerlo.");
    }

    // Kept for Unity and older web clients. Web uses JoinSession for reconnectable seats.
    public Task JoinRoom(string roomId, string playerName) => Join(roomId, playerName, null);
    public Task JoinSession(string roomId, string playerName, string token) => Join(roomId, playerName, token);
    private async Task Join(string id, string name, string? token)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 32 || string.IsNullOrWhiteSpace(name) || name.Length > 24)
            throw new HubException("Nombre o sala inválidos.");
        if (token != null && (token.Length < 32 || token.Length > 128)) throw new HubException("Sesión inválida.");
        manager.GetOrCreateRoom(id);
        await InRoom(id, async room =>
        {
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null && token != null)
            {
                player = room.Players.FirstOrDefault(p => p.ResumeToken == token);
                if (player != null)
                {
                    var old = player.ConnectionId;
                    await Groups.RemoveFromGroupAsync(old, id);
                    player.ConnectionId = Context.ConnectionId;
                    player.IsConnected = true;
                    if (room.CreatedBy == old) room.CreatedBy = player.ConnectionId;
                    if (room.LastPlayerId == old) room.LastPlayerId = player.ConnectionId;
                    if (room.FreeLeadPlayerId == old) room.FreeLeadPlayerId = player.ConnectionId;
                    if (room.PassedPlayers.Remove(old)) room.PassedPlayers.Add(player.ConnectionId);
                    room.Winners = room.Winners.Select(x => x == old ? player.ConnectionId : x).ToList();
                    if (room.LastPlay?.PlayerId == old) room.LastPlay.PlayerId = player.ConnectionId;
                    if (room.LastPlay?.SkippedPlayerId == old) room.LastPlay.SkippedPlayerId = player.ConnectionId;
                }
            }
            if (player == null)
            {
                if (room.IsGameStarted) throw new InvalidOperationException("La partida ya empezó. Esperá la próxima.");
                if (room.IsFull) throw new InvalidOperationException("La sala está llena.");
                player = new Player { ConnectionId = Context.ConnectionId, Name = name.Trim(), ResumeToken = token };
                room.Players.Add(player);
                if (room.CreatedBy == null)
                {
                    room.CreatedBy = player.ConnectionId; room.CreatorName = player.Name;
                }
            }
            if (room.GameMode != null && !room.IsGameStarted)
                room.GameMode = CardService.MakeMode(room.GameMode.DeckCount, room.Players.Count);
            await Groups.AddToGroupAsync(Context.ConnectionId, id);
            await Clients.Group(id).SendAsync("PlayerJoined", player.Name, room.Players.Count);
            await Broadcast(room);
        });
    }
    public Task SelectGameMode(string roomId, int deckCount) => InRoom(roomId, async room =>
    {
        Owner(room);
        if (room.IsGameStarted) throw new InvalidOperationException("La partida está en curso.");
        if (deckCount is < 1 or > 3) throw new InvalidOperationException("Elegí de 1 a 3 mazos.");
        room.GameMode = CardService.MakeMode(deckCount, room.Players.Count);
        await Broadcast(room);
    });
    public Task StartGame(string roomId) => InRoom(roomId, async room =>
    {
        Owner(room); logic.StartGame(room);
        foreach (var p in room.Players) await Clients.Client(p.ConnectionId).SendAsync("CardsDealt", p.Hand);
        await Clients.Group(roomId).SendAsync("GameStarted", roomId);
        await Broadcast(room);
    });
    public Task PlayCards(string roomId, List<Card> cards) => PlayCardIds(roomId, cards.Select(c => c.Id).ToList());
    public Task PlayCardIds(string roomId, List<string> ids) => InRoom(roomId, async room =>
    {
        Member(room);
        var before = room.Winners.Count;
        var play = logic.Play(room, Context.ConnectionId, ids);
        await Clients.Group(roomId).SendAsync("CardsPlayed", play);
        if (play.SkippedPlayerName != null) await Clients.Group(roomId).SendAsync("PlayerSkipped", play.SkippedPlayerName);
        if (room.Winners.Count > before) await Clients.Group(roomId).SendAsync("PlayerWon", play.PlayerName);
        await Broadcast(room);
    });
    public Task PassTurn(string roomId) => InRoom(roomId, async room =>
    {
        Member(room); logic.Pass(room, Context.ConnectionId); await Broadcast(room);
    });
    public Task GetGameState(string roomId) => InRoom(roomId, room => SendState(room, Member(room)));
    public Task LeaveRoom(string roomId, string playerName) => InRoom(roomId, async room =>
    {
        var p = Member(room);
        await Groups.RemoveFromGroupAsync(p.ConnectionId, roomId);
        await Remove(room, p);
    });
    private async Task Remove(GameRoom room, Player player)
    {
        if (room.IsGameStarted)
        {
            logic.ResetGame(room);
            room.Notice = $"{player.Name} salió. Volvemos al lobby para una nueva partida.";
        }
        room.Players.Remove(player);
        if (room.CreatedBy == player.ConnectionId)
        {
            var owner = room.Players.FirstOrDefault(p => p.IsConnected) ?? room.Players.FirstOrDefault();
            room.CreatedBy = owner?.ConnectionId; room.CreatorName = owner?.Name;
        }
        if (room.GameMode != null) room.GameMode = CardService.MakeMode(room.GameMode.DeckCount, room.Players.Count);
        await Clients.Group(room.Id).SendAsync("PlayerLeft", player.Name, room.Players.Count);
        if (manager.RemoveIfEmpty(room)) return;
        await Broadcast(room);
    }
    private async Task Broadcast(GameRoom room)
    {
        room.Revision++;
        foreach (var p in room.Players.Where(p => p.IsConnected)) await SendState(room, p);
    }
    private Task SendState(GameRoom room, Player player) => Clients.Client(player.ConnectionId).SendAsync("GameStateUpdated", new
    {
        roomId = room.Id, yourPlayerId = player.ConnectionId, revision = room.Revision,
        players = room.Players.Select(p => new { name = p.Name, connectionId = p.ConnectionId,
            cardCount = p.Hand.Count, p.IsConnected, p.IsCurrentTurn, p.IsSkipped, p.HasWon }),
        tableCards = room.TableCards, currentTurnIndex = room.CurrentTurnIndex,
        lastPlayedCards = room.LastPlayedCards, lastPlayerId = room.LastPlayerId, lastPlay = room.LastPlay,
        isGameStarted = room.IsGameStarted, isGameFinished = room.IsGameFinished,
        gameMode = room.GameMode, winners = room.Winners, roundNumber = room.RoundNumber,
        isRoomCreator = room.CreatedBy == player.ConnectionId, yourHand = player.Hand,
        isNewRound = room.FreeLeadPlayerId == player.ConnectionId, notice = room.Notice,
        isPaused = room.IsGameStarted && room.Players.Any(p => !p.IsConnected)
    });

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Retain private hands briefly; no moves are accepted while a seat reconnects.
        foreach (var room in manager.GetActiveRooms())
        {
            await room.Gate.WaitAsync();
            try
            {
                var p = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (p == null) continue;
                p.IsConnected = false;
                await Broadcast(room);
                var id = p.ConnectionId;
                var clients = hubContext.Clients;
                _ = ExpireSeat(room, id, clients);
            }
            finally { room.Gate.Release(); }
        }
        await base.OnDisconnectedAsync(exception);
    }
    private async Task ExpireSeat(GameRoom room, string id, IHubClients clients)
    {
        await Task.Delay(TimeSpan.FromSeconds(60));
        await room.Gate.WaitAsync();
        try
        {
            var p = room.Players.FirstOrDefault(p => p.ConnectionId == id && !p.IsConnected);
            if (p == null) return;
            if (room.IsGameStarted)
            {
                new GameLogicService().ResetGame(room);
                room.Notice = $"{p.Name} no volvió a conectarse. La partida fue cancelada.";
            }
            room.Players.Remove(p);
            if (room.CreatedBy == id)
            {
                var owner = room.Players.FirstOrDefault(x => x.IsConnected) ?? room.Players.FirstOrDefault();
                room.CreatedBy = owner?.ConnectionId; room.CreatorName = owner?.Name;
            }
            if (manager.RemoveIfEmpty(room)) return;
            room.Revision++;
            // Clients fetch their private snapshot after this notification.
            await clients.Group(room.Id).SendAsync("PlayerLeft", p.Name, room.Players.Count);
        }
        finally { room.Gate.Release(); }
    }
}
