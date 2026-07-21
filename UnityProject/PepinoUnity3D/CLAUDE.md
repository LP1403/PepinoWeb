# Proyecto Unity Pepino (el real)

Contexto general del proyecto en `../../CLAUDE.md`. Esto es lo específico de Unity.

**Este es el proyecto Unity que se abre en el Editor** (tiene `Packages/`, `ProjectSettings/`,
`Assets/`). La carpeta hermana `PepinoUnity3D/` en la raíz del repo (fuera de `UnityProject/`) es
solo documentación + copia de los `.cs` como referencia — no es un proyecto Unity funcional, no
la abras esperando que compile.

- Unity Editor: **6000.3.2f1** (Unity 6).
- Rama git de trabajo: `unity`.
- Arquitectura: Unity es cliente visual puro, habla con el mismo backend SignalR que el front web
  (`http://localhost:5264/gamehub`, hardcodeado por defecto en `GameConfig.asset`/`GameConfig.cs`).
  No hay lógica de reglas de juego reimplementada en Unity más allá de espejar los mismos modelos.
- `Assets/Scripts/` organizado en `Models/`, `Managers/` (`NetworkManager` = conexión SignalR,
  `GameManager` = estado local), `Controllers/` (`Card3DController`, `HandManager`,
  `TableManager`), `UI/` (Canvas 2D: Lobby, GameModeSelector, Game HUD), `Config/` (`GameConfig`
  ScriptableObject), `Utils/` (`SetupValidator`).
- Patrones: Singleton (`NetworkManager`/`GameManager` con `DontDestroyOnLoad`), eventos
  `Action<T>` desacoplados, MVC.
- Docs de diseño más largas (siguen vigentes para arquitectura/decisiones, no para paths de
  archivos): `../../PepinoUnity3D/ARCHITECTURE.md`, `PROJECT_SUMMARY.md`, `QUICK_START.md`,
  `INDEX.md`, `TEMPLATE_GUIDE.md`.

## Unity MCP

- Paquetes AI ya instalados: `com.unity.ai.assistant`, `com.unity.ai.generators`,
  `com.unity.ai.inference` (`com.unity.ai.toolkit` entra como dependencia transitiva).
- 2026-07-21: se bumpeó `com.unity.ai.assistant` de `1.0.0-pre.12` → `2.16.0-pre.1` en
  `Packages/manifest.json`, porque el soporte de Unity MCP recién aparece desde `2.0.0-pre.1`.
  La versión vieja no tenía MCP en absoluto.
- Pasos manuales pendientes (requieren el Editor abierto, no se pueden hacer headless):
  1. Abrir este proyecto en Unity Hub y dejar que Package Manager resuelva la nueva versión.
  2. `Edit > Project Settings > AI > Unity MCP` — confirmar que "Unity Bridge" está en Running.
  3. En "Integrations", elegir Claude Code → Configure (genera la config del MCP server apuntando
     al bridge local).
  4. Recargar Claude Code para que aparezcan las tools nuevas del MCP de Unity.
- Vendored NuGet (SignalR client) vive en `Packages/*.nupkg` (formato NuGetForUnity) y en
  `Assets/Packages/*.dll` — ambos se commitean a propósito, no son basura de build.
