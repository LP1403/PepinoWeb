# 🚀 Guía Rápida de Inicio - Pepino Unity 3D

## ⚡ Setup Rápido (10 minutos)

### 1. Abrir Unity Hub
- Click en **"New Project"**
- Selecciona: **"3D (Built-in Render Pipeline)"** o **"Universal 3D"**
  - 💡 Recomendado: **3D (Built-in)** - más simple
  - ⚡ Alternativa: **Universal 3D (URP)** - más moderno
  - ❌ NO uses: **High Definition 3D (HDRP)** - muy pesado
- Nombre: `PepinoUnity3D`
- Click **"Create Project"**

### 2. Copiar Scripts
```
Copia la carpeta: Assets/Scripts/ 
A tu proyecto Unity
```

### 3. Instalar Dependencias

#### A) TextMeshPro
```
Window > TextMeshPro > Import TMP Essential Resources
```

#### B) SignalR Client
**Opción Rápida - NuGet for Unity:**
```
1. Window > Package Manager
2. Add package from git URL:
   https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
3. NuGet > Manage NuGet Packages
4. Instalar: Microsoft.AspNetCore.SignalR.Client
```

#### C) LeanTween
```
Descargar de: https://assetstore.unity.com/packages/tools/animation/leantween-3595
Importar en Unity
```

### 4. Configurar Escena

**Crear GameObjects:**
```
Hierarchy:
├── NetworkManager    (+ NetworkManager.cs)
├── GameManager      (+ GameManager.cs)
├── HandManager      (+ HandManager.cs)
├── TableManager     (+ TableManager.cs)
└── Canvas (UI)
    ├── LobbyPanel   (+ LobbyUI.cs)
    ├── GamePanel    (+ GameUI.cs)
    └── GameModeSelector (+ GameModeSelectorUI.cs)
```

**Crear GameConfig:**
```
Right-click en Project > Create > Pepino > GameConfig
Asignar a NetworkManager y GameManager
```

**Crear Prefab de Carta:**
```
1. 3D Object > Cube
2. Escala: (0.7, 1.0, 0.1)
3. Añadir: Card3DController.cs
4. Añadir: Box Collider
5. Crear materiales y asignar
6. Arrastrar a Project para crear prefab
7. Asignar prefab a HandManager y TableManager
```

### 5. Configurar UI

**LobbyPanel necesita:**
- RoomIdInput (TMP_InputField)
- PlayerNameInput (TMP_InputField)
- ConnectButton (Button)
- JoinButton (Button)
- StatusText (TextMeshPro)

**GamePanel necesita:**
- RoomInfoText (TextMeshPro)
- TurnInfoText (TextMeshPro)
- PlayCardsButton (Button)
- PassTurnButton (Button)
- NotificationText (TextMeshPro)
- PepineadoEffectPanel (Panel)

Asigna todas las referencias en los scripts correspondientes.

### 6. Iniciar Backend

```bash
cd Back/GameServer/GameServer
dotnet run
```

### 7. ¡Jugar!

1. Click **Play** en Unity
2. Click **Conectar**
3. Ingresa sala y nombre
4. Click **Unirse**
5. ¡A jugar! 🥒

## 🔧 Configuración Mínima

Si quieres empezar lo más rápido posible, esta es la configuración mínima:

### GameConfig Settings:
```
Server URL: http://localhost:5264/gamehub
Reconnection Delays: [0, 2, 10, 30]
Enable Debug Logs: true
```

### Camera Position:
```
Position: (0, 10, -10)
Rotation: (45, 0, 0)
```

### Card Prefab Mínimo:
```
- Cube (0.7, 1.0, 0.1)
- Card3DController.cs
- Box Collider
- 3 materiales (blanco, verde, amarillo)
```

## ⚠️ Problemas Comunes

### "SignalR no encontrado"
```
Instala: Microsoft.AspNetCore.SignalR.Client desde NuGet
```

### "No se puede conectar"
```
Verifica que el backend esté corriendo:
cd Back/GameServer/GameServer
dotnet run
```

### "UI no se ve"
```
Canvas debe estar en "Screen Space - Overlay"
Asigna TODAS las referencias en los scripts
```

### "LeanTween no existe"
```
Importa LeanTween desde Asset Store
```

## 📱 Testing Rápido

Para testear solo (sin otros jugadores):

1. En `GameHub.cs` línea 101:
   ```csharp
   if (room.Players.Count < 1) // Ya está así para testing
   ```

2. Conéctate y crea una sala
3. Selecciona modo de juego
4. Click "Iniciar Juego"

## 🎯 Checklist

- [ ] Unity proyecto creado
- [ ] Scripts copiados
- [ ] TextMeshPro importado
- [ ] SignalR Client instalado
- [ ] LeanTween importado
- [ ] GameConfig creado
- [ ] Managers configurados
- [ ] Card Prefab creado
- [ ] UI configurada
- [ ] Backend corriendo
- [ ] ¡FUNCIONA! 🎉

## 💡 Tips

- **Usa Debug Logs**: Activa `enableDebugLogs` en GameConfig
- **Prueba Conexión**: Primero testea solo conectarte
- **Una Cosa a la Vez**: Configura y prueba cada componente
- **Usa el Web Client**: Puedes jugar contra la versión web

## 🆘 Ayuda

Si algo no funciona:

1. Revisa la consola de Unity (errores en rojo)
2. Revisa la consola del backend (logs del servidor)
3. Verifica que todas las referencias estén asignadas
4. Asegúrate que el backend esté corriendo

## 🎮 ¡Listo para Jugar!

Una vez completado este setup, tendrás un juego multiplayer 3D funcional que usa el mismo backend que la versión web.

---
**Tiempo estimado: 10-15 minutos**
**Dificultad: Media**

