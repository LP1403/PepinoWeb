// Tipos para el juego Pepino con naipes españoles
export interface Card {
    suit: '♠' | '♥' | '♦' | '♣';
    value: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12; // Naipes españoles 1-12
    id: string; // identificador único para cada carta
    isPepinoOro?: boolean; // El 3 de oro (♦) es el pepino de oro
}

export interface Player {
    connectionId: string;
    name: string;
    cardCount: number; // Solo la cantidad de cartas, no la mano completa
    isConnected: boolean;
    isCurrentTurn: boolean;
    isSkipped: boolean; // Para el efecto "PEPINEADO"
    hasWon: boolean;
}

export interface GameRoom {
    id: string;
    players: Player[];
    tableCards: Card[];
    deck: Card[];
    isGameStarted: boolean;
    currentTurnIndex: number;
    lastPlayedCards: Card[]; // Última jugada para comparar
    lastPlayerId: string; // ID del último jugador que jugó
    gameMode: GameMode;
    winners: string[]; // IDs de los ganadores
    roundNumber: number;
    createdBy?: string; // ID del jugador que creó la sala
}

export interface GameState {
    roomId: string;
    players: Player[];
    tableCards: Card[];
    currentTurnIndex: number;
    lastPlayedCards: Card[];
    lastPlayerId: string;
    isGameStarted: boolean;
    gameMode: GameMode;
    winners: string[];
    roundNumber: number;
    yourHand: Card[]; // Mano privada del jugador actual
    isRoomCreator: boolean; // Si el jugador actual es el creador de la sala
    isNewRound?: boolean; // Si es una nueva ronda (vuelta completa)
}

export interface GameMode {
    deckCount: number; // 1, 2 o 3 mazos
    maxWinners: number; // 2 para ≤4 jugadores, 3 para >4 jugadores
    cardsPerPlayer: number; // Calculado automáticamente
}

export interface PlayedCards {
    cards: Card[];
    playerId: string;
    playerName: string;
    isPepineado: boolean; // Si es la misma jugada que la anterior
}

// Tipos para las jugadas
export interface CardPlay {
    cards: Card[];
    playerId: string;
    isValid: boolean;
    reason?: string; // Razón si no es válida
}

// Eventos específicos del juego
export interface GameEvent {
    type: 'CARD_PLAYED' | 'PEPINEADO' | 'TURN_SKIPPED' | 'GAME_WON' | 'ROUND_STARTED';
    data: PlayedCards | string | number; // Tipos específicos para cada evento
} 