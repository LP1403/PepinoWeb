using GameServer.Models;
using GameServer.Services;
using Microsoft.AspNetCore.SignalR;
using System.Numerics;

namespace GameServer.Hubs;

public class GameHub : Hub
{
    private readonly GameRoomManager _roomManager;

    public GameHub(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task JoinRoom(string roomId, string playerName)
    {
        var room = _roomManager.GetOrCreateRoom(roomId);
        
        if (room.Players.Count >= 8)
        {
            await Clients.Caller.SendAsync("Error", "La sala está llena");
            return;
        }

        var player = new Player
        {
            ConnectionId = Context.ConnectionId,
            Name = playerName,
            IsConnected = true
        };

        // Si es el primer jugador, establecerlo como creador de la sala
        if (room.Players.Count == 0)
        {
            room.CreatedBy = Context.ConnectionId;
            room.CreatorName = playerName;
            Console.WriteLine($"👑 {playerName} es el creador de la sala {roomId}");
        }

        room.Players.Add(player);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, room.Players.Count);
        await SendGameStateUpdate(room);
    }

    public async Task SelectGameMode(string roomId, int deckCount)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;

        Console.WriteLine($"🎯 SelectGameMode llamado por {Context.ConnectionId} para sala {roomId} con {deckCount} mazos");
        Console.WriteLine($"🔍 DEBUG SelectGameMode:");
        Console.WriteLine($"   👤 ConnectionId del llamador: {Context.ConnectionId}");
        Console.WriteLine($"   👑 Creador de la sala: {room.CreatedBy}");
        Console.WriteLine($"   ✅ Es creador: {room.CreatedBy == Context.ConnectionId}");

        // Verificar que solo el creador de la sala pueda seleccionar el modo
        if (room.CreatedBy != Context.ConnectionId)
        {
            Console.WriteLine($"❌ {Context.ConnectionId} intentó seleccionar modo pero no es el creador. Creador: {room.CreatedBy}");
            await Clients.Caller.SendAsync("Error", "Solo el creador de la sala puede seleccionar el modo de juego");
            return;
        }

        // Validar el número de mazos
        if (deckCount < 1 || deckCount > 3)
        {
            await Clients.Caller.SendAsync("Error", "El número de mazos debe estar entre 1 y 3");
            return;
        }

        // Calcular el modo de juego con el número de mazos seleccionado
        var gameMode = new GameMode
        {
            DeckCount = deckCount,
            MaxWinners = room.Players.Count <= 4 ? 2 : 3,
            CardsPerPlayer = (deckCount * 40) / room.Players.Count
        };

        room.GameMode = gameMode;
        Console.WriteLine($"🎯 Modo de juego seleccionado: {deckCount} mazos, {gameMode.CardsPerPlayer} cartas por jugador");
        Console.WriteLine($"📊 Enviando estado actualizado a {room.Players.Count} jugadores");

        // Enviar estado actualizado a todos los jugadores
        await SendGameStateUpdate(room);
        Console.WriteLine("✅ Estado enviado después de seleccionar modo de juego");
    }

    public async Task StartGame(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) 
        {
            Console.WriteLine($"❌ No se puede iniciar el juego: Sala {roomId} no encontrada");
            return;
        }
        // Para testing local, permitir 1 jugador. Para producción, cambiar a < 2
        if (room.Players.Count < 1) 
        {
            Console.WriteLine($"❌ No se puede iniciar el juego: No hay jugadores en la sala");
            return;
        }
        // Verificar que solo el creador de la sala pueda iniciar el juego
        if (room.CreatedBy != Context.ConnectionId)
        {
            Console.WriteLine($"❌ {Context.ConnectionId} intentó iniciar el juego pero no es el creador de la sala");
            await Clients.Caller.SendAsync("Error", "Solo el creador de la sala puede iniciar el juego");
            return;
        }
        // Verificar que se haya seleccionado un modo de juego
        if (room.GameMode == null)
        {
            await Clients.Caller.SendAsync("Error", "Debe seleccionar un modo de juego antes de iniciar");
            return;
        }
        Console.WriteLine($"🎮 Iniciando juego en sala {roomId} con {room.Players.Count} jugadores");
        Console.WriteLine($"📊 Modo de juego: {room.GameMode.DeckCount} mazos, {room.GameMode.MaxWinners} ganadores máx, {room.GameMode.CardsPerPlayer} cartas por jugador");
        // PRUEBA: Verificar generación de cartas
        Console.WriteLine("🧪 Ejecutando prueba de generación de cartas...");
        CardService.TestCardGeneration();
        // Crear y barajar mazos
        Console.WriteLine($"🃏 Creando {room.GameMode.DeckCount} mazos...");
        var allCards = CardService.CreateMultipleDecks(room.GameMode.DeckCount);
        Console.WriteLine($"🃏 Mazos creados: {allCards.Count} cartas totales");
        Console.WriteLine($"🔀 Barajando mazos...");
        var shuffledDeck = CardService.ShuffleDeck(allCards);
        Console.WriteLine($"🔀 Mazos barajados: {shuffledDeck.Count} cartas");
        // Repartir todas las cartas
        Console.WriteLine($"🎴 Repartiendo cartas entre {room.Players.Count} jugadores...");
        var (hands, remainingDeck) = CardService.DealAllCards(shuffledDeck, room.Players.Count);
        Console.WriteLine($"🎴 Cartas repartidas: {hands.Count} manos, {remainingDeck.Count} cartas restantes");
        // Asignar manos a jugadores
        Console.WriteLine($"👤 Asignando manos a jugadores...");
        for (int i = 0; i < room.Players.Count; i++)
        {
            room.Players[i].Hand = hands[i];
            Console.WriteLine($"👤 {room.Players[i].Name}: {hands[i].Count} cartas");
            // Log de las primeras 3 cartas para verificar
            if (hands[i].Count > 0)
            {
                var sampleCards = hands[i].Take(3).Select(c => $"{c.Value}{c.Suit}").ToList();
                Console.WriteLine($"   📋 Muestra de cartas: {string.Join(", ", sampleCards)}");
            }
        }
        // Encontrar quien tiene el Pepino de Oro
        Console.WriteLine($"🥒 Buscando Pepino de Oro...");
        var pepinoOroIndex = CardService.FindPepinoOroPlayer(hands);
        room.CurrentTurnIndex = pepinoOroIndex;
        
        // Limpiar todos los turnos primero
        foreach (var player in room.Players)
        {
            player.IsCurrentTurn = false;
            player.IsSkipped = false;
        }
        
        // Establecer el turno del jugador con Pepino de Oro
        room.Players[pepinoOroIndex].IsCurrentTurn = true;
        Console.WriteLine($"🥒 Pepino de Oro encontrado en: {room.Players[pepinoOroIndex].Name} (índice {pepinoOroIndex})");
        Console.WriteLine($"🎯 Turno inicial establecido para: {room.Players[pepinoOroIndex].Name}");
        // Actualizar estado del juego
        room.IsGameStarted = true;
        room.Deck = remainingDeck;
        room.LastPlayedCards.Clear();
        room.LastPlayerId = null;
        room.GameStartedAt = DateTime.UtcNow;
        Console.WriteLine("✅ Juego iniciado correctamente");
        // Enviar manos a cada jugador
        Console.WriteLine($"📤 Enviando manos a cada jugador...");
        for (int i = 0; i < room.Players.Count; i++)
        {
            Console.WriteLine($"📤 Enviando {room.Players[i].Hand.Count} cartas a {room.Players[i].Name} (ConnectionId: {room.Players[i].ConnectionId})");
            
            // Log detallado de las cartas que se envían a cada jugador
            if (room.Players[i].Hand.Count > 0)
            {
                var handDetails = room.Players[i].Hand.Take(5).Select(c => $"{c.Value}{c.Suit}").ToList();
                Console.WriteLine($"   📋 Cartas enviadas a {room.Players[i].Name}: {string.Join(", ", handDetails)}");
            }
            
            await Clients.Client(room.Players[i].ConnectionId)
                .SendAsync("CardsDealt", room.Players[i].Hand);
        }
        // Notificar a todos que el juego ha iniciado
        await Clients.Group(roomId).SendAsync("GameStarted", roomId);
        Console.WriteLine($"🎮 Notificando inicio del juego a todos los jugadores de la sala {roomId}");
        
        Console.WriteLine($"🔄 Enviando estado del juego actualizado...");
        await SendGameStateUpdate(room);
        Console.WriteLine("🔄 Estado del juego enviado a todos los jugadores");
    }

    public async Task PlayCards(string roomId, List<Card> cards)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null || !room.IsGameStarted) return;

        var currentPlayer = room.Players[room.CurrentTurnIndex];
        if (currentPlayer.ConnectionId != Context.ConnectionId) return;

        var isFirstPlay = room.LastPlayedCards.Count == 0;
        var isValidPlay = CardService.ValidatePlay(cards, room.LastPlayedCards, isFirstPlay);

        if (!isValidPlay)
        {
            await Clients.Caller.SendAsync("Error", "Jugada inválida");
            return;
        }

        // Remover cartas de la mano del jugador
        foreach (var card in cards)
        {
            currentPlayer.Hand.RemoveAll(c => c.Id == card.Id);
        }

        // Verificar si el jugador ganó
        if (currentPlayer.Hand.Count == 0)
        {
            currentPlayer.HasWon = true;
            room.Winners.Add(currentPlayer.ConnectionId);
            
            await Clients.Group(roomId).SendAsync("PlayerWon", currentPlayer.Name);
            
            // Verificar si el juego terminó
            if (room.Winners.Count >= room.GameMode!.MaxWinners)
            {
                room.IsGameStarted = false;
                await SendGameStateUpdate(room);
                return;
            }
        }

        // Verificar si es PEPINEADO
        var isPepineado = CardService.IsPepineado(cards, room.LastPlayedCards);

        // Agregar cartas a la mesa
        room.TableCards.AddRange(cards);
        room.LastPlayedCards = cards;
        room.LastPlayerId = currentPlayer.ConnectionId;

        // Enviar evento de cartas jugadas
        var playedCards = new PlayedCards
        {
            Cards = cards,
            PlayerId = currentPlayer.ConnectionId,
            PlayerName = currentPlayer.Name,
            IsPepineado = isPepineado
        };

        await Clients.Group(roomId).SendAsync("CardsPlayed", playedCards);

        // Mover al siguiente turno
        await MoveToNextTurn(room, isPepineado);

        await SendGameStateUpdate(room);
    }

    public async Task PassTurn(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null || !room.IsGameStarted) return;

        var currentPlayer = room.Players[room.CurrentTurnIndex];
        if (currentPlayer.ConnectionId != Context.ConnectionId) return;

        // Solo se puede pasar si no es la primera jugada
        if (room.LastPlayedCards.Count == 0) return;

        await MoveToNextTurn(room, false);
        await SendGameStateUpdate(room);
    }

    public async Task GetGameState(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;

        var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        if (player != null)
        {
            await SendGameStateToPlayer(room, player);
        }
    }

    public async Task LeaveRoom(string roomId, string playerName)
    {
        var room = _roomManager.GetRoom(roomId);
        var player = room?.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        
        if (player != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            room!.Players.Remove(player);
            
            // Si no quedan jugadores, eliminar la sala
            if (room.Players.Count == 0)
            {
                _roomManager.RemoveRoom(roomId);
            }
            else
            {
                await Clients.Group(roomId).SendAsync("PlayerLeft", player.Name, room.Players.Count);
                await SendGameStateUpdate(room);
            }
        }
    }

    private async Task MoveToNextTurn(GameRoom room, bool skipNext)
    {
        // Limpiar estado del turno actual
        room.Players[room.CurrentTurnIndex].IsCurrentTurn = false;
        room.Players[room.CurrentTurnIndex].IsSkipped = false;

        // Calcular siguiente jugador
        int nextIndex = room.CurrentTurnIndex;
        int skipCount = skipNext ? 2 : 1; // PEPINEADO salta 2 jugadores

        for (int i = 0; i < skipCount; i++)
        {
            do
            {
                nextIndex = (nextIndex + 1) % room.Players.Count;
            } while (room.Players[nextIndex].HasWon); // Saltar ganadores
        }

        // Si el siguiente jugador está saltado por PEPINEADO, marcarlo
        if (skipNext)
        {
            room.Players[nextIndex].IsSkipped = true;
            await Clients.Group(room.Id).SendAsync("PlayerSkipped", room.Players[nextIndex].Name);
        }

        room.CurrentTurnIndex = nextIndex;
        room.Players[nextIndex].IsCurrentTurn = true;
    }

    private async Task SendGameStateToPlayer(GameRoom room, Player player)
    {
        var isCreator = room.CreatedBy == player.ConnectionId;
        
        Console.WriteLine($"🔍 DEBUG SendGameStateToPlayer:");
        Console.WriteLine($"   👤 Jugador: {player.Name} (ConnectionId: {player.ConnectionId})");
        Console.WriteLine($"   👑 Creador de la sala: {room.CreatedBy}");
        Console.WriteLine($"   ✅ Es creador: {isCreator}");
        Console.WriteLine($"   🎯 Modo de juego: {room.GameMode?.DeckCount ?? 0} mazos");
        Console.WriteLine($"   🎮 Juego iniciado: {room.IsGameStarted}");
        
        var gameState = new
        {
            roomId = room.Id,
            players = room.Players.Select(p => new 
            { 
                name = p.Name, 
                connectionId = p.ConnectionId,
                cardCount = p.Hand.Count, // Solo la cantidad, NO la mano completa
                isCurrentTurn = p.IsCurrentTurn,
                isSkipped = p.IsSkipped,
                hasWon = p.HasWon,
                isConnected = p.IsConnected
            }).ToList(),
            tableCards = room.TableCards,
            currentTurnIndex = room.CurrentTurnIndex,
            lastPlayedCards = room.LastPlayedCards,
            lastPlayerId = room.LastPlayerId,
            isGameStarted = room.IsGameStarted,
            gameMode = room.GameMode,
            winners = room.Winners,
            roundNumber = room.RoundNumber,
            isRoomCreator = isCreator,
            yourHand = player.Hand // Solo la mano del jugador actual
        };

        Console.WriteLine($"📤 Enviando estado a {player.Name} (ConnectionId: {player.ConnectionId})");
        Console.WriteLine($"👑 Es creador: {isCreator}");
        Console.WriteLine($"📊 Estado del juego: IsGameStarted={room.IsGameStarted}, Jugadores={room.Players.Count}");
        Console.WriteLine($"🎴 Mano del jugador {player.Name}: {player.Hand.Count} cartas");
        Console.WriteLine($"🔍 DEBUG: Enviando isRoomCreator={isCreator} a {player.Name}");
        
        // Log detallado de la mano del jugador
        if (player.Hand.Count > 0)
        {
            var handDetails = player.Hand.Take(5).Select(c => $"{c.Value}{c.Suit}").ToList();
            Console.WriteLine($"   📋 Primeras cartas: {string.Join(", ", handDetails)}");
        }

        await Clients.Client(player.ConnectionId).SendAsync("GameStateUpdated", gameState);
    }

    private async Task SendGameStateUpdate(GameRoom room)
    {
        foreach (var player in room.Players)
        {
            await SendGameStateToPlayer(room, player);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Buscar en qué sala estaba el jugador
        var room = _roomManager.GetRoomByConnectionId(Context.ConnectionId);
        if (room != null)
        {
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id);
                room.Players.Remove(player);
                
                // Si no quedan jugadores, eliminar la sala
                if (room.Players.Count == 0)
                {
                    _roomManager.RemoveRoom(room.Id);
                }
                else
                {
                    await Clients.Group(room.Id).SendAsync("PlayerLeft", player.Name, room.Players.Count);
                    await SendGameStateUpdate(room);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
