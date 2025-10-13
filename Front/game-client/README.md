# 🥒 Pepino - Juego de Cartas Multiplayer

Un juego de cartas 2D multiplayer para web basado en el clásico juego **Pepino** con naipes españoles, animaciones fluidas y tiempo real.

## 🚀 Tecnologías

- **Frontend**: React + Vite + TypeScript + Framer Motion
- **Backend**: ASP.NET Core 8 + SignalR
- **Comunicación**: SignalR para tiempo real

## 🎮 Reglas del Juego Pepino

### 📋 Objetivo
Ganar el juego quedándose sin cartas. El juego termina cuando se alcanza el máximo de ganadores permitidos.

### 🎯 Configuración
- **Mínimo**: 2 jugadores
- **Máximo**: 8 jugadores
- **Mazos**: 
  - ≤4 jugadores: máximo 2 mazos
  - >4 jugadores: máximo 3 mazos
- **Ganadores máximos**:
  - ≤4 jugadores: 2 ganadores
  - >4 jugadores: 3 ganadores

### 🃏 Cartas
- **Naipes españoles**: 1 al 12 (40 cartas por mazo)
- **El 3 de oro (♦)** es el **Pepino de Oro** 🥒
- **Valores**: 3 < 4 < 5 < ... < 12 < 1
- **El 2 es comodín**: Permite jugada libre

### 🎲 Mecánicas
1. **Inicio**: Empieza quien tiene el Pepino de Oro (3♦)
2. **Jugadas**: Se pueden jugar 1 hasta X cartas del mismo valor
3. **Turnos**: El siguiente debe jugar cartas de mayor valor
4. **PEPINEADO**: Si juegas exactamente la misma jugada que el anterior, el siguiente jugador es saltado
5. **Victoria**: Quien se queda sin cartas gana

### 🥒 Efecto PEPINEADO
Cuando un jugador hace la misma jugada que el anterior, aparece una animación especial que dice "🥒 PEPINEADO! 🥒" y el siguiente jugador es saltado automáticamente.

## ✨ Características Implementadas

### ✅ Funcionalidades Completadas
- **Lobby atractivo** con animaciones y validaciones
- **Sistema de salas** (hasta 8 jugadores por sala)
- **Conexión en tiempo real** con SignalR
- **Interfaz moderna** con diseño glassmorphism
- **Animaciones fluidas** con Framer Motion
- **Tipos TypeScript** robustos para cartas y estado del juego
- **Servicio de cartas** con mazo completo y barajado
- **Hook personalizado** para manejo de conexión SignalR
- **Diseño responsive** para móviles y desktop
- **Reglas completas del Pepino** implementadas
- **Efecto PEPINEADO** con animaciones especiales
- **Selección múltiple de cartas** del mismo valor
- **Validación de jugadas** en tiempo real
- **Sistema de turnos** automático
- **Indicadores visuales** para estados de jugadores
- **Modo de juego dinámico** según cantidad de jugadores

### 🎯 Próximas Funcionalidades
- [ ] Reparto inicial de manos desde backend
- [ ] Sincronización completa del estado del juego
- [ ] Chat entre jugadores
- [ ] Sonidos y efectos de audio
- [ ] Estadísticas de juego
- [ ] Modo torneo

## 🏗️ Estructura del Proyecto

```
game-client/
├── src/
│   ├── components/
│   │   ├── Lobby.tsx          # Pantalla de entrada
│   │   ├── GameTable.tsx      # Mesa de juego principal
│   │   ├── PlayerHand.tsx     # Mano del jugador
│   │   └── PepineadoEffect.tsx # Efecto PEPINEADO
│   ├── hooks/
│   │   └── useGameConnection.ts # Hook para SignalR
│   ├── services/
│   │   └── CardService.ts     # Lógica de cartas
│   ├── types/
│   │   └── Card.ts           # Tipos TypeScript
│   ├── App.tsx
│   └── main.tsx
```

## 🎨 Diseño y UX

### Características Visuales
- **Glassmorphism**: Efectos de cristal con backdrop-filter
- **Gradientes**: Fondos atractivos y botones con gradientes
- **Animaciones**: Transiciones suaves y efectos hover
- **Responsive**: Adaptable a diferentes tamaños de pantalla
- **Efecto PEPINEADO**: Animación especial con partículas

### Componentes Principales
- **Lobby**: Interfaz de entrada con validaciones
- **GameTable**: Mesa principal con jugadores y cartas
- **PlayerHand**: Mano del jugador con selección múltiple
- **PepineadoEffect**: Efecto visual para el PEPINEADO

## 🚀 Instalación y Uso

### Prerrequisitos
- Node.js 18+ 
- Backend ASP.NET Core corriendo en `https://localhost:5001`

### Instalación
```bash
npm install
```

### Desarrollo
```bash
npm run dev
```

El frontend estará disponible en `http://localhost:5173`

### Construcción
```bash
npm run build
```

## 🎮 Cómo Jugar

1. **Entrar al lobby**: Ingresa tu nombre y el ID de la sala
2. **Esperar jugadores**: Se necesitan mínimo 2 jugadores
3. **Iniciar juego**: El host puede iniciar cuando esté listo
4. **Seleccionar cartas**: Haz clic en cartas del mismo valor
5. **Jugar**: Presiona "Jugar" para hacer tu jugada
6. **PEPINEADO**: Si puedes hacer la misma jugada, ¡salta al siguiente!
7. **Ganar**: Quítate todas las cartas para ganar

## 🔧 Configuración del Backend

El backend debe estar configurado con:
- SignalR Hub en `/gamehub`
- Métodos: `JoinRoom`, `PlayCards`, `PassTurn`, `StartGame`
- Eventos: `PlayerJoined`, `CardsPlayed`, `GameStateUpdated`, `CardsDealt`, `PlayerSkipped`, `PlayerWon`, `Error`

## 📱 Responsive Design

El juego se adapta automáticamente a:
- **Desktop**: Layout completo con grid
- **Tablet**: Ajustes de tamaño de cartas
- **Mobile**: Layout vertical optimizado

## 🎨 Personalización

### Colores y Temas
Los colores principales están definidos en `src/index.css`:
- **Primario**: Verde (#4CAF50)
- **Secundario**: Dorado (#FFD700)
- **Pepino**: Verde pepino (#4CAF50)
- **Fondo**: Gradiente azul
- **Cartas**: Rojo para ♥♦, Negro para ♠♣

### Animaciones
Las animaciones usan Framer Motion con:
- **Entrada**: Fade in con escalado
- **Hover**: Elevación y escalado
- **Cartas**: Rotación y aparición secuencial
- **PEPINEADO**: Efecto especial con partículas

## 🔮 Roadmap

### Fase 1: MVP (Actual)
- ✅ Lobby y conexión básica
- ✅ Interfaz moderna
- ✅ Reglas completas del Pepino
- ✅ Efecto PEPINEADO
- ✅ Sistema de turnos

### Fase 2: Juego Completo
- [ ] Reparto automático de cartas
- [ ] Chat entre jugadores
- [ ] Sonidos y efectos
- [ ] Estadísticas de juego

### Fase 3: Mejoras
- [ ] Temas personalizables
- [ ] Modo torneo
- [ ] IA para jugar solo
- [ ] Tutorial interactivo

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature
3. Commit tus cambios
4. Push a la rama
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT.

---

**¡Disfruta jugando al Pepino! 🥒🎮**
