# 🥒 Pepino - Juego de Cartas Multiplayer

Un juego de cartas español multiplayer en tiempo real con temática de Pepino, desarrollado con .NET 8 SignalR y React + TypeScript.

## 🎮 Características del Juego

### 🃏 Reglas del Pepino
- **Objetivo**: Quedarse sin cartas
- **Pepino de Oro**: El 3♦ inicia el juego
- **Jugadas**: 1 hasta X cartas del mismo valor
- **Turnos**: El siguiente debe jugar cartas de mayor valor
- **PEPINEADO**: Misma jugada = salta al siguiente jugador
- **Victoria**: Quien se queda sin cartas gana

### 🎭 Temática de Profesiones
- ♠ **Policías** (Espadas)
- ♥ **Médicos** (Corazones) 
- ♦ **Soldados** (Diamantes)
- ♣ **Bufones** (Tréboles)

### ✨ Funcionalidades Implementadas

#### 🎯 Gestión de Salas
- ✅ Solo el creador de la sala puede iniciar el juego
- ✅ Selección de modo de juego (1, 2 o 3 mazos)
- ✅ Cálculo automático de cartas por jugador
- ✅ Máximo 8 jugadores por sala

#### 🎨 Interfaz Mejorada
- ✅ Diseño temático de Pepino con animaciones
- ✅ Cartas animadas con profesiones
- ✅ Efectos visuales para PEPINEADO
- ✅ Indicadores de turno y estado
- ✅ Responsive design

#### 🎮 Mecánicas de Juego
- ✅ Reparto automático de cartas
- ✅ Validación de jugadas
- ✅ Sistema PEPINEADO
- ✅ Múltiples ganadores
- ✅ Turnos automáticos

#### 🔧 Características Técnicas
- ✅ Conexión SignalR en tiempo real
- ✅ Reconexión automática
- ✅ Sincronización de estado
- ✅ Logs detallados en backend
- ✅ Manejo de errores

## 🚀 Instalación y Ejecución

### Prerrequisitos
- .NET 8 SDK
- Node.js 18+
- npm o yarn

### Backend (.NET 8 SignalR)

```bash
cd Back/GameServer/GameServer
dotnet restore
dotnet run
```

El backend estará disponible en: `http://localhost:5264`

### Frontend (React + TypeScript)

```bash
cd Front/game-client
npm install
npm run dev
```

El frontend estará disponible en: `http://localhost:5173`

## 🎯 Cómo Jugar

### 1. Crear/Unirse a una Sala
- Ingresa tu nombre y el ID de la sala
- Si la sala no existe, se creará automáticamente
- El primer jugador será el creador de la sala

### 2. Configurar el Juego
- **Solo el creador** puede seleccionar el modo de juego
- Elige entre 1, 2 o 3 mazos según la duración deseada
- El sistema recomienda automáticamente el mejor modo

### 3. Iniciar la Partida
- **Solo el creador** puede iniciar el juego
- Se reparten todas las cartas automáticamente
- Quien tenga el 3♦ (Pepino de Oro) inicia

### 4. Jugar
- Selecciona cartas del mismo valor
- Debes jugar cartas de mayor valor que la última jugada
- Si juegas la misma jugada, se activa PEPINEADO
- El objetivo es quedarte sin cartas

## 📊 Modos de Juego

### Cálculo de Mazos
- **1 Mazo**: 40 cartas totales
- **2 Mazos**: 80 cartas totales  
- **3 Mazos**: 120 cartas totales

### Recomendaciones por Jugadores
- **2 jugadores**: 2 mazos (40 cartas cada uno)
- **3-4 jugadores**: 1 mazo (10-13 cartas cada uno)
- **5-6 jugadores**: 2 mazos (13-16 cartas cada uno)
- **7-8 jugadores**: 3 mazos (15-17 cartas cada uno)

## 🎨 Características Visuales

### Animaciones
- ✅ Cartas con efectos de entrada
- ✅ Animaciones de selección
- ✅ Efectos PEPINEADO
- ✅ Transiciones suaves
- ✅ Indicadores de turno animados

### Diseño Responsive
- ✅ Adaptable a móviles y tablets
- ✅ Grid layouts flexibles
- ✅ Controles táctiles optimizados

### Temática
- ✅ Favicon de pepino personalizado
- ✅ Colores y gradientes temáticos
- ✅ Iconos y emojis descriptivos
- ✅ Tipografía clara y legible

## 🔧 Estructura del Proyecto

```
PepinoWeb/
├── Back/
│   └── GameServer/
│       ├── GameServer/
│       │   ├── Hubs/GameHub.cs
│       │   ├── Models/
│       │   ├── Services/
│       │   └── Program.cs
│       └── README.md
├── Front/
│   └── game-client/
│       ├── src/
│       │   ├── components/
│       │   ├── hooks/
│       │   ├── services/
│       │   └── types/
│       ├── public/
│       └── package.json
└── README.md
```

## 🛠️ Tecnologías Utilizadas

### Backend
- **.NET 8** - Framework principal
- **ASP.NET Core SignalR** - Comunicación en tiempo real
- **C#** - Lenguaje de programación
- **Entity Framework** - Manejo de datos (preparado)

### Frontend
- **React 18** - Framework de UI
- **TypeScript** - Tipado estático
- **Vite** - Build tool y dev server
- **Framer Motion** - Animaciones
- **SignalR Client** - Cliente para comunicación en tiempo real

## 🎯 Próximas Mejoras

### Funcionalidades Planificadas
- [ ] Sonidos y efectos de audio
- [ ] Chat en tiempo real
- [ ] Estadísticas de partidas
- [ ] Modo torneo
- [ ] Persistencia de datos
- [ ] Autenticación de usuarios

### Mejoras Técnicas
- [ ] Tests unitarios
- [ ] Docker deployment
- [ ] CI/CD pipeline
- [ ] Monitoreo y logs
- [ ] Optimización de rendimiento

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

## 🎮 ¡Disfruta Jugando Pepino!

¡Conecta con amigos, crea salas y disfruta del clásico juego de cartas español con todas las mejoras modernas!

---

**Desarrollado con ❤️ y 🥒** 