# 🥒 Implementación Backend - Juego Pepino

## 🎯 Reglas del Juego Pepino

### 📋 Configuración del Juego
- **Mínimo**: 2 jugadores
- **Máximo**: 8 jugadores
- **Mazos**: 
  - ≤4 jugadores: máximo 2 mazos
  - >4 jugadores: máximo 3 mazos
- **Ganadores máximos**:
  - ≤4 jugadores: 2 ganadores
  - >4 jugadores: 3 ganadores

### 🃏 Cartas (Naipes Españoles)
- **Valores**: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
- **Palos**: ♠, ♥, ♦, ♣
- **Pepino de Oro**: 3♦ (inicia el juego)
- **Comodín**: 2 (permite jugada libre)
- **Jerarquía**: 3 < 4 < 5 < ... < 12 < 1

### 🎲 Mecánicas
1. **Reparto**: Todas las cartas se reparten entre los jugadores
2. **Inicio**: Empieza quien tiene el 3♦ (Pepino de Oro)
3. **Jugadas**: 1 hasta X cartas del mismo valor
4. **Turnos**: El siguiente debe jugar cartas de mayor valor
5. **PEPINEADO**: Misma jugada = salta al siguiente jugador
6. **Victoria**: Quien se queda sin cartas gana

## 🏗️ Arquitectura del Backend

### 📁 Estructura del Proyecto
```
GameServer/
├── Program.cs                    # Configuración de la aplicación
├── appsettings.json             # Configuración
├── Hubs/
│   └── GameHub.cs               # Hub principal de SignalR
├── Models/
│   ├── Card.cs                  # Modelo de carta
│   ├── Player.cs                # Modelo de jugador
│   ├── GameRoom.cs              # Modelo de sala de juego
│   ├── GameState.cs             # Estado del juego
│   ├── GameMode.cs              # Modo de juego
│   └── PlayedCards.cs           # Cartas jugadas
├── Services/
│   ├── CardService.cs           # Lógica de cartas
│   ├── GameRoomManager.cs       # Gestión de salas
│   └── GameLogicService.cs      # Lógica del juego
├── Interfaces/
│   ├── IGameRoomManager.cs      # Interfaz del gestor de salas
│   └── ICardService.cs          # Interfaz del servicio de cartas
├── DTOs/
│   ├── GameStateDto.cs          # DTO para estado del juego
│   └── PlayerDto.cs             # DTO para jugador
└── Extensions/
    └── ServiceCollectionExtensions.cs # Configuración de servicios
```

### 🔧 Configuración de la Aplicación

#### Program.cs
```csharp
using Microsoft.AspNetCore.SignalR;
using GameServer.Hubs;
using GameServer.Services;
using GameServer.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Registrar servicios personalizados
builder.Services.AddSingleton<IGameRoomManager, GameRoomManager>();
builder.Services.AddSingleton<ICardService, CardService>();
builder.Services.AddSingleton<GameLogicService>();

var app = builder.Build();

// Configurar middleware
app.UseCors("AllowAll");
app.UseRouting();

// Mapear SignalR Hub
app.MapHub<GameHub>("/gamehub");

app.Run();
```

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GameSettings": {
    "MaxPlayersPerRoom": 8,
    "MinPlayersToStart": 2,
    "MaxDecksForSmallGame": 2,
    "MaxDecksForLargeGame": 3,
    "MaxWinnersForSmallGame": 2,
    "MaxWinnersForLargeGame": 3
  }
}
```

### 🎮 Patrón de Arquitectura

#### 1. **Capa de Presentación (SignalR Hub)**
- **GameHub.cs**: Maneja la comunicación en tiempo real
- Responsabilidades:
  - Recibir conexiones de clientes
  - Procesar métodos del hub
  - Enviar eventos a clientes
  - Coordinar con servicios de negocio

#### 2. **Capa de Servicios (Business Logic)**
- **GameRoomManager.cs**: Gestión de salas de juego
- **CardService.cs**: Lógica de cartas y mazos
- **GameLogicService.cs**: Reglas del juego y validaciones

#### 3. **Capa de Modelos (Domain Models)**
- **Card.cs**: Entidad carta
- **Player.cs**: Entidad jugador
- **GameRoom.cs**: Entidad sala de juego
- **GameState.cs**: Estado del juego

#### 4. **Capa de DTOs (Data Transfer Objects)**
- **GameStateDto.cs**: Para transferir estado del juego
- **PlayerDto.cs**: Para transferir datos de jugador

### 🔄 Flujo de Datos

#### 1. **Conexión de Cliente**
```
Cliente → SignalR → GameHub.JoinRoom() → GameRoomManager → Respuesta
```

#### 2. **Inicio de Juego**
```
Cliente → GameHub.StartGame() → CardService → GameRoomManager → Respuesta
```

#### 3. **Jugada de Cartas**
```
Cliente → GameHub.PlayCards() → GameLogicService → CardService → Respuesta
```

#### 4. **Actualización de Estado**
```
GameLogicService → GameRoomManager → GameHub → Clientes
```

### 🎯 Patrones de Diseño Utilizados

#### 1. **Singleton Pattern**
```csharp
// GameRoomManager como singleton para gestión global de salas
builder.Services.AddSingleton<IGameRoomManager, GameRoomManager>();
```

#### 2. **Dependency Injection**
```csharp
public class GameHub : Hub
{
    private readonly IGameRoomManager _gameRoomManager;
    private readonly ICardService _cardService;

    public GameHub(IGameRoomManager gameRoomManager, ICardService cardService)
    {
        _gameRoomManager = gameRoomManager;
        _cardService = cardService;
    }
}
```

#### 3. **Repository Pattern** (implícito en GameRoomManager)
```csharp
public interface IGameRoomManager
{
    GameRoom GetOrCreateRoom(string roomId);
    GameRoom GetRoom(string roomId);
    GameRoom GetRoomByPlayerId(string playerId);
    void RemoveRoom(string roomId);
    List<GameRoom> GetAllRooms();
}
```

#### 4. **Service Layer Pattern**
```csharp
public class GameLogicService
{
    public bool ValidatePlay(List<Card> cards, GameRoom room);
    public void ProcessTurn(GameRoom room, Player player);
    public bool CheckGameEnd(GameRoom room);
}
```

### 🔒 Gestión de Estado

#### 1. **Estado en Memoria**
- **GameRoomManager**: Mantiene todas las salas activas
- **GameRoom**: Estado completo de cada sala
- **Player**: Estado individual de cada jugador

#### 2. **Sincronización**
- **SignalR Groups**: Cada sala es un grupo SignalR
- **GameState**: Estado completo sincronizado
- **Eventos**: Notificaciones en tiempo real

#### 3. **Persistencia** (Futuro)
```csharp
// Para futuras implementaciones con base de datos
public interface IGameRepository
{
    Task<GameRoom> GetRoomAsync(string roomId);
    Task SaveRoomAsync(GameRoom room);
    Task<List<GameRoom>> GetActiveRoomsAsync();
}
```

### 🚀 Escalabilidad

#### 1. **Horizontal Scaling**
- **SignalR Backplane**: Para múltiples instancias
- **Redis**: Para compartir estado entre servidores
- **Load Balancer**: Para distribuir conexiones

#### 2. **Vertical Scaling**
- **Async/Await**: Operaciones asíncronas
- **Memory Management**: Limpieza de salas inactivas
- **Connection Pooling**: Gestión eficiente de conexiones

#### 3. **Monitoring**
```csharp
public class GameMetrics
{
    public int ActiveRooms { get; set; }
    public int TotalPlayers { get; set; }
    public int ActiveGames { get; set; }
    public Dictionary<string, int> RoomSizes { get; set; }
}
```

### 🔧 Configuración de Servicios

#### ServiceCollectionExtensions.cs
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar servicios del juego
        services.Configure<GameSettings>(configuration.GetSection("GameSettings"));
        
        // Registrar servicios
        services.AddSingleton<IGameRoomManager, GameRoomManager>();
        services.AddSingleton<ICardService, CardService>();
        services.AddSingleton<GameLogicService>();
        
        // Configurar SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 102400; // 100KB
        });
        
        return services;
    }
}
```

### 📊 Logging y Debugging

#### 1. **Structured Logging**
```csharp
public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;

    public async Task JoinRoom(string roomId, string playerName)
    {
        _logger.LogInformation("Player {PlayerName} joining room {RoomId}", playerName, roomId);
        // ... lógica del método
    }
}
```

#### 2. **Health Checks**
```csharp
public class GameHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Verificar estado del juego
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
```

### 🔐 Seguridad

#### 1. **Validación de Entrada**
```csharp
public async Task PlayCards(string roomId, List<Card> cards)
{
    if (string.IsNullOrEmpty(roomId) || cards == null)
    {
        await Clients.Caller.SendAsync("Error", "Datos inválidos");
        return;
    }
    // ... resto de la lógica
}
```

#### 2. **Rate Limiting**
```csharp
[RateLimit(MaxRequests = 10, TimeWindow = 60)] // 10 requests por minuto
public async Task PlayCards(string roomId, List<Card> cards)
{
    // ... lógica del método
}
```

---

## ��️ Modelos Backend

### Card.cs
```csharp
public class Card
{
    public string Suit { get; set; } // "♠", "♥", "♦", "♣"
    public int Value { get; set; }   // 1-12
    public string Id { get; set; }   // Identificador único
    public bool IsPepinoOro { get; set; } // true si es 3♦
    
    public Card(string suit, int value)
    {
        Suit = suit;
        Value = value;
        Id = $"{suit}-{value}-{Guid.NewGuid()}";
        IsPepinoOro = suit == "♦" && value == 3;
    }
}
```

### Player.cs
```csharp
public class Player
{
    public string ConnectionId { get; set; }
    public string Name { get; set; }
    public List<Card> Hand { get; set; } = new();
    public bool IsConnected { get; set; } = true;
    public bool IsCurrentTurn { get; set; } = false;
    public bool IsSkipped { get; set; } = false;
    public bool HasWon { get; set; } = false;
}
```

### GameRoom.cs
```csharp
public class GameRoom
{
    public string Id { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<Card> TableCards { get; set; } = new();
    public List<Card> Deck { get; set; } = new();
    public bool IsGameStarted { get; set; } = false;
    public int CurrentTurnIndex { get; set; } = 0;
    public List<Card> LastPlayedCards { get; set; } = new();
    public string LastPlayerId { get; set; }
    public GameMode GameMode { get; set; }
    public List<string> Winners { get; set; } = new();
    public int RoundNumber { get; set; } = 1;
}

public class GameMode
{
    public int DeckCount { get; set; }
    public int MaxWinners { get; set; }
    public int CardsPerPlayer { get; set; }
}
```

## 🔧 Servicios Backend

### CardService.cs
```csharp
public static class CardService
{
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
    private static readonly int[] Values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    public static List<Card> CreateSpanishDeck()
    {
        var deck = new List<Card>();
        foreach (var suit in Suits)
        {
            foreach (var value in Values)
            {
                deck.Add(new Card(suit, value));
            }
        }
        return deck;
    }

    public static List<Card> CreateMultipleDecks(int deckCount)
    {
        var allCards = new List<Card>();
        for (int i = 0; i < deckCount; i++)
        {
            var deck = CreateSpanishDeck();
            foreach (var card in deck)
            {
                card.Id = $"{card.Id}-deck{i}";
            }
            allCards.AddRange(deck);
        }
        return allCards;
    }

    public static List<Card> ShuffleDeck(List<Card> deck)
    {
        var shuffled = new List<Card>(deck);
        var random = new Random();
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        return shuffled;
    }

    public static GameMode CalculateGameMode(int playerCount)
    {
        int deckCount, maxWinners;

        if (playerCount <= 4)
        {
            deckCount = Math.Min(2, Math.Max(1, (int)Math.Ceiling(40.0 / playerCount)));
            maxWinners = 2;
        }
        else
        {
            deckCount = Math.Min(3, Math.Max(1, (int)Math.Ceiling(40.0 / playerCount)));
            maxWinners = 3;
        }

        int totalCards = deckCount * 40;
        int cardsPerPlayer = totalCards / playerCount;

        return new GameMode
        {
            DeckCount = deckCount,
            MaxWinners = maxWinners,
            CardsPerPlayer = cardsPerPlayer
        };
    }

    public static (List<List<Card>> hands, List<Card> remainingDeck) DealAllCards(List<Card> deck, int numPlayers)
    {
        var hands = new List<List<Card>>();
        for (int i = 0; i < numPlayers; i++)
        {
            hands.Add(new List<Card>());
        }

        var remainingDeck = new List<Card>(deck);
        int currentPlayer = 0;

        while (remainingDeck.Count > 0)
        {
            var card = remainingDeck[remainingDeck.Count - 1];
            remainingDeck.RemoveAt(remainingDeck.Count - 1);
            hands[currentPlayer].Add(card);
            currentPlayer = (currentPlayer + 1) % numPlayers;
        }

        return (hands, remainingDeck);
    }

    public static int GetCardValue(Card card)
    {
        if (card.Value == 2) return 0; // Comodín
        if (card.Value == 1) return 13; // El 1 es el más alto
        return card.Value; // 3-12 mantienen su valor
    }

    public static bool ValidatePlay(List<Card> selectedCards, List<Card> lastPlayedCards, bool isFirstPlay)
    {
        if (selectedCards.Count == 0) return false;

        // Verificar que todas las cartas tengan el mismo valor
        var firstValue = selectedCards[0].Value;
        if (!selectedCards.All(c => c.Value == firstValue)) return false;

        // Si es la primera jugada, cualquier carta es válida
        if (isFirstPlay || lastPlayedCards == null || lastPlayedCards.Count == 0) return true;

        // Verificar que la cantidad de cartas sea la misma
        if (selectedCards.Count != lastPlayedCards.Count) return false;

        // Verificar que el valor sea mayor
        var lastValue = GetCardValue(lastPlayedCards[0]);
        var currentValue = GetCardValue(selectedCards[0]);

        return currentValue > lastValue;
    }

    public static bool IsPepineado(List<Card> selectedCards, List<Card> lastPlayedCards)
    {
        if (lastPlayedCards == null || lastPlayedCards.Count == 0) return false;
        if (selectedCards.Count != lastPlayedCards.Count) return false;

        var selectedValue = selectedCards[0].Value;
        var lastValue = lastPlayedCards[0].Value;

        return selectedValue == lastValue && selectedCards.All(c => c.Value == selectedValue);
    }

    public static int FindPepinoOroPlayer(List<List<Card>> hands)
    {
        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i].Any(c => c.IsPepinoOro))
            {
                return i;
            }
        }
        return 0;
    }
}
```

## 🎮 GameHub.cs

### Métodos del Hub
```csharp
public class GameHub : Hub
{
    private readonly GameRoomManager _gameRoomManager;

    public GameHub(GameRoomManager gameRoomManager)
    {
        _gameRoomManager = gameRoomManager;
    }

    public async Task JoinRoom(string roomId, string playerName)
    {
        var room = _gameRoomManager.GetOrCreateRoom(roomId);
        
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

        room.Players.Add(player);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, room.Players.Count);
        await SendGameStateUpdate(room);
    }

    public async Task StartGame(string roomId)
    {
        var room = _gameRoomManager.GetRoom(roomId);
        if (room == null || room.Players.Count < 2) return;

        // Calcular modo de juego
        room.GameMode = CardService.CalculateGameMode(room.Players.Count);

        // Crear y barajar mazos
        var allCards = CardService.CreateMultipleDecks(room.GameMode.DeckCount);
        var shuffledDeck = CardService.ShuffleDeck(allCards);

        // Repartir todas las cartas
        var (hands, remainingDeck) = CardService.DealAllCards(shuffledDeck, room.Players.Count);

        // Asignar manos a jugadores
        for (int i = 0; i < room.Players.Count; i++)
        {
            room.Players[i].Hand = hands[i];
        }

        // Encontrar quien tiene el Pepino de Oro
        var pepinoOroIndex = CardService.FindPepinoOroPlayer(hands);
        room.CurrentTurnIndex = pepinoOroIndex;
        room.Players[pepinoOroIndex].IsCurrentTurn = true;

        // Actualizar estado del juego
        room.IsGameStarted = true;
        room.Deck = remainingDeck;
        room.LastPlayedCards.Clear();
        room.LastPlayerId = null;

        // Enviar manos a cada jugador
        for (int i = 0; i < room.Players.Count; i++)
        {
            await Clients.Client(room.Players[i].ConnectionId)
                .SendAsync("CardsDealt", room.Players[i].Hand);
        }

        await SendGameStateUpdate(room);
    }

    public async Task PlayCards(string roomId, List<Card> cards)
    {
        var room = _gameRoomManager.GetRoom(roomId);
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
            if (room.Winners.Count >= room.GameMode.MaxWinners)
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
        var room = _gameRoomManager.GetRoom(roomId);
        if (room == null || !room.IsGameStarted) return;

        var currentPlayer = room.Players[room.CurrentTurnIndex];
        if (currentPlayer.ConnectionId != Context.ConnectionId) return;

        // Solo se puede pasar si no es la primera jugada
        if (room.LastPlayedCards.Count == 0) return;

        await MoveToNextTurn(room, false);
        await SendGameStateUpdate(room);
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

    private async Task SendGameStateUpdate(GameRoom room)
    {
        var gameState = new GameState
        {
            RoomId = room.Id,
            Players = room.Players,
            TableCards = room.TableCards,
            CurrentTurnIndex = room.CurrentTurnIndex,
            LastPlayedCards = room.LastPlayedCards,
            LastPlayerId = room.LastPlayerId,
            IsGameStarted = room.IsGameStarted,
            GameMode = room.GameMode,
            Winners = room.Winners,
            RoundNumber = room.RoundNumber
        };

        await Clients.Group(room.Id).SendAsync("GameStateUpdated", gameState);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var room = _gameRoomManager.GetRoomByPlayerId(Context.ConnectionId);
        if (room != null)
        {
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player != null)
            {
                player.IsConnected = false;
                await Clients.Group(room.Id).SendAsync("PlayerJoined", $"{player.Name} (desconectado)", room.Players.Count);
                await SendGameStateUpdate(room);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

## 📡 Eventos SignalR

### Eventos Enviados por el Backend
- `PlayerJoined(name, count)` - Jugador se une
- `GameStateUpdated(state)` - Estado completo del juego
- `CardsDealt(playerHand)` - Cartas repartidas al jugador
- `CardsPlayed(playedCards)` - Cartas jugadas
- `PlayerSkipped(playerName)` - Jugador saltado por PEPINEADO
- `PlayerWon(playerName)` - Jugador ganó
- `Error(message)` - Error del juego

### Eventos Recibidos por el Backend
- `JoinRoom(roomId, playerName)` - Unirse a sala
- `StartGame(roomId)` - Iniciar juego
- `PlayCards(roomId, cards)` - Jugar cartas
- `PassTurn(roomId)` - Pasar turno

## 🎯 Próximos Pasos

1. **Implementar GameRoomManager** para gestión de salas
2. **Agregar validaciones** adicionales
3. **Implementar reconexión** de jugadores
4. **Agregar logging** para debugging
5. **Optimizar rendimiento** para múltiples salas

---

**¡El backend está listo para implementar el juego Pepino completo! 🥒** 