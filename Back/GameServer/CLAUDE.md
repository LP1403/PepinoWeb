# Backend Pepino (ASP.NET Core + SignalR)

Contexto general del proyecto en `../../CLAUDE.md`. Esto es solo lo específico del backend.

- Proyecto real: `GameServer/` (el `.csproj` está ahí, no en `Back/GameServer/`).
- .NET 8, SignalR 10.0.1. Hub: `Hubs/GameHub.cs`, sirve en `http://localhost:5264/gamehub`.
- Estado 100% en memoria vía `Services/GameRoomManager.cs` (`Dictionary<string, GameRoom>`) — se
  pierde al reiniciar el proceso. No hay base de datos ni migraciones.
- CORS abierto solo a `http://localhost:5173` (el puerto de Vite). Si el cliente Unity necesita
  pegarle desde otro origen/esquema, revisar la política de CORS en `Program.cs`.
- La lógica de reglas del juego (validación de jugadas, PEPINEADO, búsqueda del Pepino de Oro)
  vive en `Services/CardService.cs` y `Services/GameLogicService.cs` — es la fuente de verdad,
  por encima de cualquier .md.
- `appsettings.Development.json` está en `.gitignore` (se sacó del tracking de git en 2026-07;
  antes se commiteaba sin querer). No tiene secretos actualmente, pero no debe volver a trackearse.
- Correr: `cd GameServer && dotnet run` (el `.csproj` está en `Back/GameServer/GameServer/`).
