# Frontend web Pepino (React + Vite)

Contexto general del proyecto en `../../CLAUDE.md`. Esto es solo lo específico del front web.

- Stack: React 19 + TypeScript + Vite 7, cliente `@microsoft/signalr`, animaciones Framer Motion.
- `src/hooks/useGameConnection.ts` centraliza la conexión SignalR y el estado del juego — es el
  punto de entrada para entender cómo el front habla con el backend.
- `src/services/CardService.ts` es un espejo liviano de la lógica de cartas del backend, usado
  para validación optimista en UI. La lógica autoritativa sigue siendo la del backend.
- `src/components/`: `Lobby` (conexión/join) → `GameModeSelector` (solo creador, elige mazos) →
  `GameTable`/`OverheadTable`/`PlayerHand` (mesa de juego). `PepinoArena` es una composición
  alternativa tipo UNO, en modo test.
- No hay estado global (Redux/Zustand) ni tests unitarios — deliberado, no es un olvido a "arreglar"
  sin que lo pidan.
- Correr: `npm install && npm run dev` (puerto 5173, coincide con el CORS configurado en el backend).
