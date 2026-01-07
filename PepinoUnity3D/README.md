# 🥒 Pepino Unity 3D - Juego de Cartas Multiplayer

Versión Unity 3D del juego de cartas español multiplayer "Pepino". Reutiliza completamente el backend .NET 8 SignalR existente.

## 🎮 Descripción

Este es un juego de cartas 3D desarrollado en Unity que se conecta al mismo backend que la versión web. Los jugadores pueden jugar juntos en tiempo real, ya sea desde Unity o desde la web.

## 📋 Requisitos Previos

### Software Necesario

1. **Unity Hub** (ya instalado ✅)
2. **Unity Editor** 2021.3 LTS o superior
3. **.NET 8 SDK** (para el backend)
4. **Visual Studio 2022** o **Visual Studio Code** (recomendado para C#)

### Dependencias de Unity

Este proyecto requiere los siguientes paquetes de Unity:

1. **TextMeshPro** (UI)
2. **SignalR Client para Unity**
3. **LeanTween** (para animaciones)

## 🚀 Instalación Paso a Paso

### 1. Crear el Proyecto Unity

1. Abre **Unity Hub**
2. Click en **"New Project"**
3. Selecciona el template:
   - **Recomendado:** "3D (Built-in Render Pipeline)"
   - **Alternativa:** "Universal 3D" (URP)
   - **NO usar:** "High Definition 3D" (HDRP - muy pesado)
4. Nombre: `PepinoUnity3D`
5. Ubicación: Donde quieras
6. Click **"Create Project"**

### 2. Importar los Scripts

Una vez creado el proyecto Unity:

1. Copia la carpeta `Assets/Scripts` de este repositorio a tu proyecto Unity
2. Unity detectará automáticamente los scripts

### 3. Instalar SignalR Client para Unity

**Opción A: Usando NuGet for Unity (Recomendado)**

1. En Unity, ve a **Window > Package Manager**
2. Click en **"+"** > **"Add package from git URL"**
3. Pega: `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
4. Una vez instalado, ve a **NuGet > Manage NuGet Packages**
5. Busca e instala:
   - `Microsoft.AspNetCore.SignalR.Client` (v8.0 o superior)
   - `System.Threading.Channels`

**Opción B: Manual (DLLs)**

1. Descarga las DLLs necesarias desde NuGet:
   - Microsoft.AspNetCore.SignalR.Client
   - Microsoft.AspNetCore.SignalR.Common
   - Microsoft.AspNetCore.SignalR.Protocols.Json
   - Microsoft.Extensions.DependencyInjection
   - Microsoft.Extensions.Logging
   - System.Threading.Channels

2. Copia todas las DLLs a `Assets/Plugins/`

### 4. Instalar LeanTween

1. Descarga LeanTween desde: https://assetstore.unity.com/packages/tools/animation/leantween-3595
2. O descarga desde GitHub: https://github.com/dentedpixel/LeanTween
3. Importa en Unity: **Assets > Import Package > Custom Package**

### 5. Configurar TextMeshPro

1. En Unity, ve a **Window > TextMeshPro > Import TMP Essential Resources**
2. Click **"Import"**

## 🎨 Configuración del Proyecto

### 1. Crear la Escena Principal

1. Crea una nueva escena: **File > New Scene**
2. Guárdala como `GameScene`

### 2. Configurar los Managers

Crea GameObjects vacíos en la jerarquía:

```
Hierarchy:
├── Managers
│   ├── NetworkManager
│   ├── GameManager
│   └── UIManager
├── Controllers
│   ├── HandManager
│   └── TableManager
├── UI
│   ├── Canvas (UI)
│   │   ├── LobbyPanel
│   │   ├── GamePanel
│   │   └── NotificationPanel
└── Camera
    └── Main Camera
```

#### NetworkManager Setup

1. Crea GameObject: **"NetworkManager"**
2. Añade el script: `NetworkManager.cs`
3. Crea un ScriptableObject para GameConfig:
   - Right-click en Project > **Create > Pepino > GameConfig**
   - Asigna el GameConfig al NetworkManager
   - Configura `serverUrl`: `http://localhost:5264/gamehub`

#### GameManager Setup

1. Crea GameObject: **"GameManager"**
2. Añade el script: `GameManager.cs`
3. Asigna el mismo GameConfig

#### HandManager Setup

1. Crea GameObject: **"HandManager"**
2. Añade el script: `HandManager.cs`
3. Crea un prefab de carta (ver sección "Crear Prefab de Carta")
4. Asigna el prefab en `cardPrefab`

#### TableManager Setup

1. Crea GameObject: **"TableManager"**
2. Añade el script: `TableManager.cs`
3. Asigna el mismo prefab de carta

### 3. Crear UI Canvas

1. Click derecho en Hierarchy > **UI > Canvas**
2. Configura el Canvas:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler**: Scale With Screen Size
   - **Reference Resolution**: 1920x1080

#### Lobby Panel

```
LobbyPanel (Panel)
├── Background (Image)
├── TitleText (TextMeshPro)
├── RoomIdInput (TMP_InputField)
├── PlayerNameInput (TMP_InputField)
├── ConnectButton (Button)
├── JoinButton (Button)
└── StatusText (TextMeshPro)
```

Añade el script `LobbyUI.cs` al LobbyPanel y asigna las referencias.

#### Game Panel

```
GamePanel (Panel)
├── TopBar (Panel)
│   ├── RoomInfoText (TextMeshPro)
│   └── TurnInfoText (TextMeshPro)
├── PlayersList (Vertical Layout Group)
├── BottomBar (Panel)
│   ├── PlayCardsButton (Button)
│   └── PassTurnButton (Button)
├── NotificationText (TextMeshPro)
└── PepineadoEffect (Panel)
    └── EffectText (TextMeshPro)
```

Añade el script `GameUI.cs` al GamePanel y asigna todas las referencias.

#### Game Mode Selector

Dentro del GamePanel, crea:

```
GameModeSelector (Panel)
├── Deck1Button (Button)
├── Deck2Button (Button)
├── Deck3Button (Button)
├── StartGameButton (Button)
├── ModeInfoText (TextMeshPro)
└── PlayersInfoText (TextMeshPro)
```

Añade el script `GameModeSelectorUI.cs` y asigna las referencias.

### 4. Crear Prefab de Carta

1. Crea un **3D Object > Cube** (o Quad si prefieres plano)
2. Escala: `(0.7, 1.0, 0.1)` para simular una carta
3. Añade el script `Card3DController.cs`
4. Añade un **Box Collider** (para detectar clicks)
5. Crea 3 materiales:
   - `DefaultCardMaterial` (blanco)
   - `SelectedCardMaterial` (verde)
   - `HighlightCardMaterial` (amarillo)
6. Asigna los materiales al script
7. Arrastra el objeto a la carpeta Project para crear el prefab
8. Elimina el objeto de la escena

### 5. Configurar la Cámara

Posiciona la cámara para ver bien la mesa:

```
Main Camera:
Position: (0, 10, -10)
Rotation: (45, 0, 0)
```

## 🎯 Preparar Assets de Cartas

Aunque el juego funcionará sin assets visuales, puedes preparar sprites para cada carta:

### Estructura de Carpeta de Assets

```
Assets/Resources/Cards/
├── ♠_1.png   (As de Espadas)
├── ♠_2.png   (2 de Espadas)
...
├── ♥_1.png   (As de Copas)
...
├── ♦_1.png   (As de Oros)
├── ♦_3.png   (3 de Oros - Pepino de Oro)
...
└── ♣_1.png   (As de Bastos)
```

**Nota**: El usuario mencionó que conseguirá los assets después, así que por ahora el juego funcionará con colores sólidos.

## 🎮 Cómo Jugar

### 1. Iniciar el Backend

```bash
cd Back/GameServer/GameServer
dotnet restore
dotnet run
```

El servidor debe estar corriendo en `http://localhost:5264`

### 2. Ejecutar el Juego en Unity

1. Abre la escena `GameScene`
2. Click en **Play** ▶️
3. En el lobby:
   - Click **"Conectar"** para conectarte al servidor
   - Ingresa un **ID de sala** (ej: "SALA1")
   - Ingresa tu **nombre**
   - Click **"Unirse a la Sala"**

### 3. Iniciar Partida

- Si eres el **primer jugador** (creador):
  - Verás el selector de modo de juego
  - Selecciona **1, 2 o 3 mazos**
  - Click **"Iniciar Juego"**

- Si eres un jugador adicional:
  - Espera a que el creador inicie el juego

### 4. Jugar

- Cuando sea tu turno, verás **"¡TU TURNO!"**
- Click en las cartas para seleccionarlas
- Click **"Jugar Cartas"** para jugarlas
- Click **"Pasar Turno"** si no puedes jugar

## 📊 Arquitectura del Proyecto

```
PepinoUnity3D/
├── Assets/
│   ├── Scripts/
│   │   ├── Models/           # Modelos de datos (Card, Player, GameState)
│   │   ├── Managers/         # NetworkManager, GameManager
│   │   ├── Controllers/      # Card3DController, HandManager, TableManager
│   │   ├── UI/              # LobbyUI, GameUI, GameModeSelectorUI
│   │   ├── Utils/           # Utilidades generales
│   │   └── Config/          # GameConfig (ScriptableObject)
│   ├── Scenes/              # GameScene
│   ├── Prefabs/             # CardPrefab
│   ├── Materials/           # Materiales de cartas
│   └── Resources/
│       └── Cards/           # Sprites/Texturas de cartas
└── README.md
```

## 🔧 Configuración del GameConfig

El `GameConfig` es un ScriptableObject que contiene toda la configuración:

```
Network Settings:
- Server URL: http://localhost:5264/gamehub
- Reconnection Delays: 0, 2, 10, 30 segundos

Game Settings:
- Max Players Per Room: 8
- Min Players To Start: 2

UI Settings:
- Card Animation Duration: 0.3s
- Pepineado Effect Duration: 3s
- Selected Card Scale: 1.2

3D Settings:
- Card Spacing: 1.5
- Hand Arc Radius: 8
- Selected Card Height: 0.5

Debug:
- Enable Debug Logs: true
```

## 🐛 Troubleshooting

### Error: "SignalR no encontrado"

- Asegúrate de haber instalado las DLLs de SignalR en `Assets/Plugins/`
- Verifica que sean compatibles con .NET Standard 2.1

### Error: "No se puede conectar al servidor"

- Verifica que el backend esté corriendo: `dotnet run`
- Verifica la URL en GameConfig: `http://localhost:5264/gamehub`
- Desactiva firewall/antivirus temporalmente

### Las cartas no se ven

- Verifica que el prefab de carta tenga un MeshRenderer
- Asigna materiales al Card3DController
- Ajusta la posición de la cámara

### LeanTween no funciona

- Importa LeanTween desde Asset Store o GitHub
- Asegúrate que esté en la carpeta `Assets/`

### UI no se ve

- Verifica que el Canvas esté en modo "Screen Space - Overlay"
- Asigna todas las referencias en los scripts de UI
- Revisa la Event Camera si usas World Space Canvas

## 🎨 Próximos Pasos

1. **Añadir Assets Visuales**
   - Importar sprites de cartas
   - Crear texturas para cada carta
   - Modificar `Card3DController` para mostrar sprites

2. **Mejorar Efectos Visuales**
   - Partículas para PEPINEADO
   - Sonidos de cartas
   - Animaciones más elaboradas

3. **Optimizaciones**
   - Object Pooling para cartas
   - Compresión de datos de red
   - Reducir allocations

4. **Features Adicionales**
   - Chat en tiempo real
   - Emotes/Reacciones
   - Estadísticas de partida
   - Replay system

## 📝 Notas Importantes

- ✅ El backend es **exactamente el mismo** que la versión web
- ✅ No necesitas modificar **nada** del backend
- ✅ Jugadores de Unity y Web pueden jugar juntos
- ✅ Toda la lógica del juego está en el servidor
- ✅ Unity solo es un cliente visual

## 🤝 Compatibilidad

Este proyecto es compatible con:
- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ Android (con ajustes menores)
- ✅ iOS (con ajustes menores)
- ✅ WebGL (requiere configuración especial de SignalR)

## 📚 Recursos Útiles

- [Documentación de SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [Unity C# Scripting Reference](https://docs.unity3d.com/ScriptReference/)
- [LeanTween Documentation](http://dentedpixel.com/developer-diary/)
- [TextMeshPro Documentation](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)

## 🎮 ¡Disfruta Jugando Pepino en 3D!

---

**Desarrollado con ❤️ y 🥒**
**Backend reutilizado al 100% - Zero cambios necesarios**

