# 🏗️ Arquitectura del Proyecto - Pepino Unity 3D

## 📐 Diseño General

Este proyecto sigue una arquitectura **Cliente-Servidor** donde:
- **Servidor (.NET)**: Maneja toda la lógica del juego
- **Cliente (Unity)**: Solo visualiza y envía comandos

```
┌─────────────────────────────────────────┐
│         Backend (.NET SignalR)          │
│                                         │
│  ┌─────────────┐   ┌────────────────┐  │
│  │  GameHub    │   │  GameLogic     │  │
│  │  (SignalR)  │◄─►│  Service       │  │
│  └─────────────┘   └────────────────┘  │
│         ▲                               │
└─────────┼───────────────────────────────┘
          │ WebSocket (SignalR)
          │
┌─────────▼───────────────────────────────┐
│      Cliente Unity (Visualización)      │
│                                         │
│  ┌──────────────┐   ┌───────────────┐  │
│  │ Network      │   │   Game        │  │
│  │ Manager      │◄─►│   Manager     │  │
│  └──────────────┘   └───────────────┘  │
│         ▲                   │           │
│         │                   ▼           │
│  ┌──────┴────┐    ┌─────────────────┐  │
│  │   UI      │◄──►│  3D Controllers │  │
│  │ Scripts   │    │  (Cards, Hand)  │  │
│  └───────────┘    └─────────────────┘  │
└─────────────────────────────────────────┘
```

## 🎯 Patrones de Diseño Utilizados

### 1. Singleton Pattern
**Usado en:** NetworkManager, GameManager

```csharp
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**Por qué:** Necesitamos acceso global a la conexión de red y al estado del juego.

### 2. Observer Pattern (Event System)
**Usado en:** Todos los managers

```csharp
// Publicador
public event Action<GameState> OnGameStateUpdated;

// Suscriptor
NetworkManager.Instance.OnGameStateUpdated += HandleGameStateUpdated;
```

**Por qué:** Desacopla componentes y permite reacción a eventos de red.

### 3. MVC (Model-View-Controller)
**Estructura:**

- **Models:** `Card`, `Player`, `GameState` (datos puros)
- **Views:** UI Scripts, 3D Controllers (visualización)
- **Controllers:** `GameManager`, `NetworkManager` (lógica)

### 4. ScriptableObject Pattern
**Usado en:** GameConfig

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Pepino/GameConfig")]
public class GameConfig : ScriptableObject
{
    public string serverUrl;
    public float cardAnimationDuration;
    // ...
}
```

**Por qué:** Configuración centralizada y modificable desde el editor.

## 📦 Estructura de Carpetas

```
Assets/Scripts/
├── Models/              # Datos (POCO classes)
│   ├── Card.cs
│   ├── Player.cs
│   ├── GameState.cs
│   ├── GameMode.cs
│   └── PlayedCards.cs
│
├── Managers/            # Lógica central
│   ├── NetworkManager.cs    # Comunicación SignalR
│   └── GameManager.cs        # Estado y lógica del juego
│
├── Controllers/         # Control de GameObjects 3D
│   ├── Card3DController.cs  # Una carta individual
│   ├── HandManager.cs        # Gestión de la mano
│   └── TableManager.cs       # Gestión de la mesa
│
├── UI/                  # Scripts de interfaz
│   ├── LobbyUI.cs
│   ├── GameModeSelectorUI.cs
│   └── GameUI.cs
│
├── Config/              # Configuración
│   └── GameConfig.cs
│
└── Utils/               # Utilidades
    └── (futuras helpers)
```

## 🔄 Flujo de Datos

### Conectarse y Unirse a una Sala

```
Usuario                 LobbyUI           NetworkManager      Backend
  │                       │                     │               │
  │─── Click Connect ────►│                     │               │
  │                       │──── ConnectToServer()───────────────►│
  │                       │                     │               │
  │                       │◄──── Connected ─────────────────────┤
  │                       │                     │               │
  │─── Click Join ────────►│                     │               │
  │                       │──── JoinRoom() ─────►│               │
  │                       │                     │─── JoinRoom ──►│
  │                       │                     │               │
  │                       │◄──── PlayerJoined event ────────────┤
```

### Jugar Cartas

```
Usuario          Card3D          GameManager     NetworkManager    Backend
  │                │                  │                │              │
  │─── Click ─────►│                  │                │              │
  │                │─ ToggleSelection()               │              │
  │                │                  │                │              │
  │                │◄── SetSelected ──┤                │              │
  │                │                  │                │              │
  │─ Click Play ───┼──────────────────►│                │              │
  │                │                  │─ Validate      │              │
  │                │                  │─ PlayCards()───►│              │
  │                │                  │                │── PlayCards ►│
  │                │                  │                │              │
  │                │                  │◄──── GameStateUpdated ────────┤
  │                │                  │                │              │
  │                │◄── UpdateHand ───┤                │              │
```

### Actualización de Estado

```
Backend              NetworkManager          GameManager          UI / Controllers
  │                       │                       │                     │
  │─ GameStateUpdated ───►│                       │                     │
  │                       │─── Event ────────────►│                     │
  │                       │                       │─── Event ──────────►│
  │                       │                       │                     │
  │                       │                       │◄── Update UI/3D ───┤
```

## 🎮 Componentes Principales

### NetworkManager
**Responsabilidad:** Comunicación con el servidor

**Métodos Públicos:**
- `ConnectToServer()`: Conecta a SignalR
- `JoinRoom()`: Une a una sala
- `SelectGameMode()`: Selecciona modo de juego
- `StartGame()`: Inicia la partida
- `PlayCards()`: Juega cartas
- `PassTurn()`: Pasa el turno

**Eventos:**
- `OnConnectionChanged`
- `OnGameStateUpdated`
- `OnCardsDealt`
- `OnCardsPlayed`
- `OnPlayerJoined/Left/Won/Skipped`
- `OnGameStarted`
- `OnError`

### GameManager
**Responsabilidad:** Gestión del estado del juego

**Métodos Públicos:**
- `InitializeGame()`: Inicializa sesión
- `ToggleCardSelection()`: Selecciona/deselecciona carta
- `PlaySelectedCards()`: Juega cartas seleccionadas
- `PassTurn()`: Pasa turno
- `ValidatePlay()`: Valida jugada

**Propiedades:**
- `CurrentGameState`: Estado actual
- `SelectedCards`: Cartas seleccionadas
- `CurrentRoomId`: ID de sala actual
- `CurrentPlayerName`: Nombre del jugador

### Card3DController
**Responsabilidad:** Comportamiento de una carta 3D

**Métodos:**
- `Initialize()`: Inicializa con datos
- `SetSelected()`: Selecciona/deselecciona
- `AnimatePlay()`: Anima carta jugándose
- `ToggleSelection()`: Toggle selección

**Interacciones:**
- `OnMouseEnter/Exit`: Hover
- `OnMouseDown`: Click

### HandManager
**Responsabilidad:** Gestión de cartas en la mano

**Métodos:**
- `UpdateHand()`: Actualiza cartas en mano
- `ArrangeCardsInArc()`: Organiza en arco
- `RemoveCard()`: Remueve carta
- `ClearHand()`: Limpia mano
- `GetSelectedCards()`: Obtiene seleccionadas

### TableManager
**Responsabilidad:** Gestión de cartas en la mesa

**Métodos:**
- `AddCardsToTable()`: Añade cartas jugadas
- `ClearTable()`: Limpia mesa
- `ShowPepineadoEffect()`: Muestra efecto

## 🔐 Validación

### Client-Side (Unity)
- Validación de UI (inputs no vacíos)
- Validación visual (mismo valor de carta)
- Validación de turno (es mi turno?)

### Server-Side (.NET)
- ✅ Validación de jugadas (CardService.ValidatePlay)
- ✅ Validación de turnos (GameHub)
- ✅ Validación de permisos (creador de sala)
- ✅ Validación de estado (juego iniciado?)

**Importante:** La validación definitiva SIEMPRE es en el servidor.

## 📡 Comunicación SignalR

### Eventos del Cliente → Servidor

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `JoinRoom` | roomId, playerName | Unirse a sala |
| `SelectGameMode` | roomId, deckCount | Seleccionar mazos |
| `StartGame` | roomId | Iniciar juego |
| `PlayCards` | roomId, cards[] | Jugar cartas |
| `PassTurn` | roomId | Pasar turno |
| `GetGameState` | roomId | Solicitar estado |
| `LeaveRoom` | roomId, playerName | Salir de sala |

### Eventos del Servidor → Cliente

| Evento | Datos | Descripción |
|--------|-------|-------------|
| `GameStateUpdated` | GameState | Estado actualizado |
| `CardsDealt` | Card[] | Cartas repartidas |
| `CardsPlayed` | PlayedCards | Cartas jugadas |
| `PlayerJoined` | name, count | Jugador se unió |
| `PlayerLeft` | name, count | Jugador se fue |
| `PlayerWon` | name | Jugador ganó |
| `PlayerSkipped` | name | Jugador saltado |
| `GameStarted` | roomId | Juego iniciado |
| `Error` | message | Error |

## 🎨 Sistema de UI

### Jerarquía UI
```
Canvas (Screen Space - Overlay)
├── LobbyPanel (activo al inicio)
│   └── LobbyUI.cs
├── GamePanel (activo en juego)
│   ├── GameUI.cs
│   └── GameModeSelector
│       └── GameModeSelectorUI.cs
└── NotificationPanel
```

### Estados de UI
1. **Lobby**: Desconectado → Conectando → Conectado
2. **Sala**: Esperando jugadores → Selección modo → Juego iniciado
3. **Juego**: Esperando turno → Mi turno → Jugando

## 🔄 Ciclo de Vida

### Inicialización
```
1. Unity Start()
2. NetworkManager.ConnectToServer()
3. User joins room → NetworkManager.JoinRoom()
4. GameManager.InitializeGame()
5. Subscribe to events
```

### Durante el Juego
```
1. Server sends GameStateUpdated
2. NetworkManager receives event
3. GameManager updates state
4. UI/Controllers update visuals
5. User interacts → GameManager validates → NetworkManager sends
6. Repeat
```

### Finalización
```
1. Game ends (player wins)
2. Server sends PlayerWon events
3. UI shows winner
4. User can start new game or leave
```

## 🚀 Optimizaciones Futuras

### Object Pooling
- Reutilizar GameObjects de cartas
- Reducir Instantiate/Destroy

### Compresión de Datos
- Usar IDs en lugar de objetos completos
- Delta compression para estados

### Predicción Client-Side
- Predecir movimientos para reducir latencia
- Validar contra servidor

### Asset Bundles
- Cargar assets de cartas dinámicamente
- Reducir tamaño inicial

## 📊 Ventajas de esta Arquitectura

✅ **Separación de Responsabilidades**: Cada componente tiene un rol claro
✅ **Reutilización**: Mismo backend para web y Unity
✅ **Escalabilidad**: Fácil añadir features
✅ **Testeable**: Componentes desacoplados
✅ **Mantenible**: Código organizado
✅ **Extensible**: Fácil modificar/ampliar

---

**Esta arquitectura permite un desarrollo modular y profesional** 🏗️

