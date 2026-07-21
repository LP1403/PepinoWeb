# Pepino — contexto del proyecto

Juego de cartas multiplayer inventado por el dueño del repo, con naipes españoles. Empezó como
juego web (React + backend .NET/SignalR) y se está migrando a Unity 3D, reutilizando el mismo
backend. Meta actual: terminar la migración a Unity y tenerlo conectado al Unity MCP para poder
operar el Editor desde Claude Code.

## Estructura del repo

```
Back/GameServer/GameServer/   backend ASP.NET Core 8 + SignalR (única fuente de verdad de las reglas)
Front/game-client/            cliente web, React 19 + TS + Vite (ver Front/game-client/CLAUDE.md)
UnityProject/PepinoUnity3D/   el proyecto Unity REAL (ver UnityProject/PepinoUnity3D/CLAUDE.md)
PepinoUnity3D/                SOLO documentación + scripts fuente sueltos — NO es un proyecto Unity
                              abrible (no tiene Packages/, ProjectSettings/, etc.). Sirve como
                              referencia/backup de los .md y .cs, pero el desarrollo real pasa en
                              UnityProject/PepinoUnity3D/.
test-*.html (raíz)            tests manuales viejos de SignalR/cartas, previos a tener el front en
                              React. Históricos, no forman parte del flujo de desarrollo actual.
```

**Trampa a evitar**: hay dos carpetas con nombre `PepinoUnity3D`. Si te piden trabajar en Unity,
es casi siempre `UnityProject/PepinoUnity3D/`.

## Reglas del juego "Pepino"

- Naipes españoles, 4 palos con temática: ♠ Policías, ♥ Médicos, ♦ Soldados, ♣ Bufones.
- Valores 1-12 (sin el 8 tradicional del mazo español; usa 1-12 tal cual, ver `CardService`).
- Jerarquía de juego: `3 < 4 < ... < 12 < 1` (el As es la carta más alta).
- El **2 es comodín**: permite jugada libre en cualquier momento.
- El **3♦ ("Pepino de Oro")** define quién empieza la partida.
- Se juegan grupos de 1 a N cartas del mismo valor; el siguiente jugador debe igualar cantidad y
  jugar valor igual o mayor.
- **PEPINEADO**: si un jugador juega exactamente la misma combinación (mismo valor + misma
  cantidad) que la jugada anterior, se salta el turno del siguiente jugador.
- Gana quien se queda primero sin cartas (2 ganadores si ≤4 jugadores, 3 si son más).
- 2-8 jugadores por sala, 1-3 mazos según cantidad de jugadores (ver `GameMode`).

Fuente canónica de las reglas: `Back/GameServer/GameServer/Services/CardService.cs` y
`GameLogicService.cs` — si hay dudas de comportamiento exacto, el código del backend manda sobre
cualquier doc.

## Arquitectura general

Cliente-servidor vía **SignalR** (hub en `http://localhost:5264/gamehub`). El backend .NET es la
única fuente de verdad del estado de juego (todo en memoria, sin DB, sin persistencia, sin auth).
Tanto el cliente web (React) como el cliente Unity son "vistas" delgadas que hablan el mismo
protocolo de eventos SignalR — no hay lógica de juego duplicada del lado del cliente más allá de
UI/animaciones y una copia liviana de `CardService` para validación optimista.

Eventos SignalR client→server: `JoinRoom`, `SelectGameMode`, `StartGame`, `PlayCards`, `PassTurn`,
`GetGameState`, `LeaveRoom`.
Eventos server→client: `GameStateUpdated`, `CardsDealt`, `CardsPlayed`, `PlayerJoined`,
`PlayerLeft`, `PlayerWon`, `PlayerSkipped`, `GameStarted`, `Error`.

## Cómo correr todo

```bash
# Backend (puerto 5264)
cd Back/GameServer/GameServer && dotnet run

# Frontend web (puerto 5173)
cd Front/game-client && npm install && npm run dev

# Unity: abrir UnityProject/PepinoUnity3D con Unity Hub (versión 6000.3.2f1)
```

## Git

- Rama de la migración Unity: `unity` (no `main`).
- Otras ramas: `conlospibes`, `visual` (experimentales).
- El `.gitignore` fue reescrito para Unity/.NET/Node — si aparecen archivos de `Library/`,
  `obj/`, `bin/`, `UserSettings/` como modificados/nuevos, es señal de que algo se está generando
  donde no debería, no de que el gitignore esté roto.

## Estado / pendientes conocidos

- Backend: sin persistencia (todo en memoria), sin autenticación, sin rate limiting.
- Unity: faltan assets visuales finales de cartas (sprites/materiales), sin object pooling.
- Unity MCP: en configuración (ver `UnityProject/PepinoUnity3D/CLAUDE.md` para el estado exacto).
- Docs pre-existentes más detalladas en `PepinoUnity3D/ARCHITECTURE.md`, `PROJECT_SUMMARY.md`,
  `QUICK_START.md`, `INDEX.md` — mayormente vigentes para entender decisiones de diseño Unity,
  pero para estructura de archivos real siempre confiar en `UnityProject/PepinoUnity3D/`.
