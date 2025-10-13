# 🥒 Game Server Backend - Juego Pepino

Este es el backend completo para el juego de cartas **Pepino**, implementado con ASP.NET Core y SignalR.

## 🎯 Características del Juego Pepino

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

### 🎲 Mecánicas del Juego
1. **Reparto**: Todas las cartas se reparten entre los jugadores
2. **Inicio**: Empieza quien tiene el 3♦ (Pepino de Oro)
3. **Jugadas**: 1 hasta X cartas del mismo valor
4. **Turnos**: El siguiente debe jugar cartas de mayor valor
5. **PEPINEADO**: Misma jugada = salta al siguiente jugador
6. **Victoria**: Quien se queda sin cartas gana

## 🏗️ Estructura del Proyecto

### Modelos (`Models/`)
- `Card.cs`: Representa una carta con palo, valor y detección de Pepino de Oro
- `Player.cs`: Representa un jugador con conexión, mano y estado del juego
- `GameRoom.cs`: Representa una sala de juego con estado completo
- `GameMode.cs`: Configuración del juego según número de jugadores
- `PlayedCards.cs`: Cartas jugadas con metadatos

### Servicios (`Services/`)
- `CardService.cs`: Lógica completa de cartas, mazos múltiples y validaciones
- `GameLogicService.cs`: Lógica del juego, turnos y mecánicas PEPINEADO
- `GameRoomManager.cs`: Gestión de salas y jugadores

### Hubs (`Hubs/`)
- `GameHub.cs`: Hub de SignalR para comunicación en tiempo real

## 📡 Endpoints HTTP

### GET `/api/rooms`
Obtiene todas las salas activas con información básica.

### GET `/api/rooms/{roomId}`
Obtiene información detallada de una sala específica.

## 🔄 Métodos SignalR

### Cliente → Servidor
- `JoinRoom(roomId, playerName)`: Unirse a una sala
- `StartGame(roomId)`: Iniciar el juego y repartir cartas
- `PlayCards(roomId, cards)`: Jugar cartas (múltiples del mismo valor)
- `PassTurn(roomId)`: Pasar turno
- `GetGameState(roomId)`: Obtener estado del juego
- `LeaveRoom(roomId)`: Salir de la sala

### Servidor → Cliente
- `PlayerJoined(name, count)`: Nuevo jugador se unió
- `PlayerLeft(name, count)`: Jugador salió
- `CardsDealt(playerHand)`: Cartas repartidas al jugador
- `CardsPlayed(playedCards)`: Cartas jugadas con metadatos
- `PlayerSkipped(playerName)`: Jugador saltado por PEPINEADO
- `PlayerWon(playerName)`: Jugador ganó
- `GameStateUpdated(gameState)`: Estado completo del juego
- `PlayerDisconnected(playerName)`: Jugador desconectado
- `Error(message)`: Mensaje de error

## 🎮 Flujo del Juego

### 1. **Unirse a Sala**
```
Cliente → JoinRoom(roomId, playerName) → Servidor
Servidor → PlayerJoined(name, count) → Todos los clientes
```

### 2. **Iniciar Juego**
```
Cliente → StartGame(roomId) → Servidor
Servidor → CardsDealt(playerHand) → Cada jugador
Servidor → GameStateUpdated(state) → Todos los clientes
```

### 3. **Jugar Cartas**
```
Cliente → PlayCards(roomId, cards) → Servidor
Servidor → CardsPlayed(playedCards) → Todos los clientes
Servidor → GameStateUpdated(state) → Todos los clientes
```

### 4. **PEPINEADO**
```
Si cartas jugadas = cartas anteriores → Salta 2 jugadores
Servidor → PlayerSkipped(playerName) → Todos los clientes
```

## 🚀 Ejecutar el Proyecto

1. Asegúrate de tener .NET 8.0 instalado
2. Navega al directorio del proyecto:
   ```bash
   cd Back/GameServer/GameServer
   ```
3. Ejecuta el proyecto:
   ```bash
   dotnet run
   ```
4. El servidor estará disponible en `http://localhost:5000`

## ⚙️ Configuración

El servidor está configurado para aceptar conexiones desde `http://localhost:5173` (puerto típico de Vite/React).

### Configuración de CORS
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .WithOrigins("http://localhost:5173");
    });
});
```

## 🔧 Desarrollo

### Compilar el Proyecto
```bash
dotnet build
```

### Ejecutar Tests (futuro)
```bash
dotnet test
```

### Limpiar Build
```bash
dotnet clean
```

## 📊 Estado del Juego

El backend implementa completamente:

✅ **Gestión de salas** con límites de jugadores  
✅ **Mazos múltiples** según número de jugadores  
✅ **Reparto automático** de todas las cartas  
✅ **Detección del Pepino de Oro** (3♦)  
✅ **Validación de jugadas** con reglas del juego  
✅ **Mecánica PEPINEADO** con saltos de turno  
✅ **Gestión de turnos** automática  
✅ **Múltiples ganadores** según configuración  
✅ **Comunicación en tiempo real** con SignalR  
✅ **Estado completo** del juego sincronizado  

## 🎯 Próximos Pasos

1. **Implementar reconexión** de jugadores desconectados
2. **Agregar logging** detallado para debugging
3. **Implementar persistencia** con base de datos
4. **Agregar métricas** de juego
5. **Optimizar rendimiento** para múltiples salas

---

**¡El backend del juego Pepino está completamente implementado y listo para usar! 🥒** 