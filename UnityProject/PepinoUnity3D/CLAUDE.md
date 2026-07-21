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

- Paquetes AI instalados en `Packages/manifest.json`: **solo** `com.unity.ai.assistant`
  (`2.16.0-pre.1`) y `com.unity.ai.inference` (`2.6.1`, sin relación con MCP, se dejó como
  estaba). **NO agregar** `com.unity.ai.generators` ni `com.unity.ai.toolkit` a mano — ver
  lección abajo, rompe todo.
- 2026-07-21: se bumpeó `com.unity.ai.assistant` de `1.0.0-pre.12` → `2.16.0-pre.1`, porque el
  soporte de Unity MCP recién aparece desde `2.0.0-pre.1`. La versión vieja no tenía MCP.
- 2026-07-21 (mismo día): el usuario abrió el proyecto con Unity **6000.5.4f1** (más nuevo que el
  6000.3.2f1 original). Pasamos por dos rondas de errores de compilación / Safe Mode:
  1. Primero: `ai.generators`/`ai.toolkit` viejos (`1.0.0-pre.20`) usaban `Object.GetInstanceID()`,
     que Editor 6000.5.4f1 promovió de obsoleto-warning a obsoleto-**error**. Fix intentado:
     agregar `ai.toolkit` a mano y bumpear `ai.generators` a `1.7.0-pre.1`.
  2. Eso generó algo peor: cientos de `GUID ... conflicts with ...` y finalmente
     `Assembly with name 'Unity.AI.Generators.IO.Srp' already exists`. Causa real: **desde
     `ai.assistant` 2.x, ese paquete ya trae empaquetados adentro suyo (bajo
     `Packages/com.unity.ai.assistant/Modules/Unity.AI.*`) los mismos módulos Animate/
     Generators/Image/Mesh/ModelSelector/Pbr/Sound/Toolkit que antes vivían en paquetes
     separados, con los mismos GUID**. Tener `ai.generators`/`ai.toolkit` instalados aparte
     duplica esos GUID y Unity no puede resolverlo.
  - **Fix final y correcto**: sacar `com.unity.ai.generators` y `com.unity.ai.toolkit` del
    manifest por completo. `ai.assistant` 2.16 no los lista como dependencia (confirmado
    consultando `packages.unity.com/com.unity.ai.assistant`) precisamente porque ya no los
    necesita — los trae embebidos.
  - **Lección**: con `com.unity.ai.assistant` en la rama 2.x, NO instalar `ai.generators` ni
    `ai.toolkit` como paquetes separados — son redundantes y colisionan. Si en el futuro esto
    vuelve a pasar al actualizar el Editor, revisar `Logs/Editor.log` del proyecto
    (`grep "error CS"` primero, si no aparece nada relevante buscar `"already exists"` o
    `"conflicts with"`) antes que nada — casi seguro son estos paquetes AI, no el código propio
    del juego.
- Pasos manuales pendientes (requieren el Editor abierto, no se pueden hacer headless):
  1. Abrir este proyecto en Unity Hub y dejar que Package Manager resuelva la nueva versión.
  2. `Edit > Project Settings > AI > Unity MCP` — confirmar que "Unity Bridge" está en Running.
  3. En "Integrations", elegir Claude Code → Configure (genera la config del MCP server apuntando
     al bridge local).
  4. Recargar Claude Code para que aparezcan las tools nuevas del MCP de Unity.
- Vendored NuGet (SignalR client) vive en `Packages/*.nupkg` (formato NuGetForUnity) y en
  `Assets/Packages/*.dll` — ambos se commitean a propósito, no son basura de build.
