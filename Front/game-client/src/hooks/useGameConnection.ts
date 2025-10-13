import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import type { Card, Player, GameState, GameMode, PlayedCards } from '../types/Card';

interface UseGameConnectionProps {
    roomId: string;
    playerName: string;
}

interface UseGameConnectionReturn {
    connection: signalR.HubConnection | undefined;
    gameState: GameState | null;
    players: Player[];
    tableCards: Card[];
    hand: Card[];
    isConnected: boolean;
    isGameStarted: boolean;
    isMyTurn: boolean;
    lastPlayedCards: Card[] | null;
    isFirstPlay: boolean;
    gameMode: GameMode | null;
    winners: string[];
    showPepineado: boolean;
    pepineadoPlayer: string;
    isRoomCreator: boolean;
    isNewRound: boolean;
    playCards: (cards: Card[]) => Promise<void>;
    startGame: () => Promise<void>;
    selectGameMode: (deckCount: number) => Promise<void>;
}

export function useGameConnection({ roomId, playerName }: UseGameConnectionProps): UseGameConnectionReturn {
    const [connection, setConnection] = useState<signalR.HubConnection>();
    const [gameState, setGameState] = useState<GameState | null>(null);
    const [players, setPlayers] = useState<Player[]>([]);
    const [tableCards, setTableCards] = useState<Card[]>([]);
    const [hand, setHand] = useState<Card[]>([]);
    const [isConnected, setIsConnected] = useState(false);
    const [isGameStarted, setIsGameStarted] = useState(false);
    const [isMyTurn, setIsMyTurn] = useState(false);
    const [lastPlayedCards, setLastPlayedCards] = useState<Card[] | null>(null);
    const [isFirstPlay, setIsFirstPlay] = useState(true);
    const [gameMode, setGameMode] = useState<GameMode | null>(null);
    const [winners, setWinners] = useState<string[]>([]);
    const [showPepineado, setShowPepineado] = useState(false);
    const [pepineadoPlayer, setPepineadoPlayer] = useState('');
    const [isRoomCreator, setIsRoomCreator] = useState(false);
    const [isNewRound, setIsNewRound] = useState(false);

    // Conectar al hub
    useEffect(() => {
        console.log("🔄 Iniciando conexión SignalR...");
        const conn = new signalR.HubConnectionBuilder()
            .withUrl("http://127.0.0.1:5264/gamehub")
            .withAutomaticReconnect([0, 2000, 10000, 30000]) // Reintentos progresivos
            .build();

        setConnection(conn);

        conn.start().then(() => {
            console.log("✅ Conectado a SignalR exitosamente");
            setIsConnected(true);
            console.log(`🎯 Uniéndose a sala: ${roomId} como: ${playerName}`);
            conn.invoke("JoinRoom", roomId, playerName);
        }).catch(err => {
            console.error("❌ Error de conexión SignalR: ", err);
            // No mostrar alert en el primer intento, solo reintentar
            if (conn.state === signalR.HubConnectionState.Disconnected) {
                console.log("🔄 Reintentando conexión...");
            }
        });

        // Manejar desconexión cuando el usuario cierra la pestaña
        const handleBeforeUnload = () => {
            console.log("🔌 Usuario cerrando pestaña, desconectando...");
            conn.invoke("LeaveRoom", roomId, playerName).catch(err => {
                console.error("Error al salir de la sala:", err);
            });
        };

        window.addEventListener('beforeunload', handleBeforeUnload);

        return () => {
            console.log("🔌 Desconectando SignalR...");
            conn.invoke("LeaveRoom", roomId, playerName).catch(err => {
                console.error("Error al salir de la sala:", err);
            });
            conn.stop();
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [roomId, playerName]);

    // Configurar eventos del hub
    useEffect(() => {
        if (!connection) return;

        // Evento cuando un jugador se une
        connection.on("PlayerJoined", (name: string, count: number) => {
            console.log(`👤 ${name} se unió. Jugadores: ${count}`);
            // Solicitar actualización del estado para todos
            connection.invoke("GetGameState", roomId).catch(err => {
                console.error("Error obteniendo estado del juego:", err);
            });
        });

        // Evento cuando un jugador se desconecta
        connection.on("PlayerLeft", (name: string, count: number) => {
            console.log(`👋 ${name} se desconectó. Jugadores restantes: ${count}`);
            // Solicitar actualización del estado para todos
            connection.invoke("GetGameState", roomId).catch(err => {
                console.error("Error obteniendo estado del juego:", err);
            });
        });

        // Evento cuando se actualiza el estado del juego
        connection.on("GameStateUpdated", (state: GameState) => {
            console.log("🔄 Estado del juego actualizado:", state);
            console.log("🔍 DEBUG Frontend - Estado recibido:");
            console.log(`   👑 isRoomCreator: ${state.isRoomCreator} (tipo: ${typeof state.isRoomCreator})`);
            console.log(`   🎯 gameMode: ${state.gameMode ? JSON.stringify(state.gameMode) : 'null'}`);
            console.log(`   🎮 isGameStarted: ${state.isGameStarted}`);
            console.log(`   👥 players: ${state.players?.length || 0}`);
            console.log(`   🔄 isNewRound: ${state.isNewRound} (tipo: ${typeof state.isNewRound})`);
            console.log(`   🔍 Estado completo recibido:`, JSON.stringify(state, null, 2));

            setGameState(state);
            setPlayers(state.players ?? []);
            setTableCards(state.tableCards ?? []);
            setIsGameStarted(state.isGameStarted);
            setLastPlayedCards(state.lastPlayedCards ?? []);
            setGameMode(state.gameMode);
            setWinners(state.winners ?? []);
            setIsRoomCreator(state.isRoomCreator);
            setIsNewRound(state.isNewRound ?? false);

            // Usar la mano privada del jugador (YourHand) en lugar de buscar en el array
            if (state.yourHand) {
                setHand(state.yourHand);
                console.log(`🎴 Tu mano actualizada: ${state.yourHand.length} cartas`);
                if (state.yourHand.length > 0) {
                    const sample = state.yourHand.slice(0, 3).map(c => `${c.value}${c.suit}`).join(', ');
                    console.log(`📋 Muestra de tu mano: ${sample}`);
                }
            }

            // Encontrar el jugador actual para el turno
            const currentPlayer = (state.players ?? []).find(p => p.name === playerName);
            if (currentPlayer) {
                setIsMyTurn(currentPlayer.isCurrentTurn);
                console.log(`🎮 Tu turno: ${currentPlayer.isCurrentTurn}`);
            }

            // Determinar si es la primera jugada
            setIsFirstPlay((state.lastPlayedCards ?? []).length === 0);
        });

        // Evento cuando se juegan cartas
        connection.on("CardsPlayed", (playedCards: PlayedCards) => {
            console.log(`🃏 ${playedCards.playerName} jugó cartas:`, playedCards.cards);
            setTableCards(prev => [...prev, ...playedCards.cards]);
            setLastPlayedCards(playedCards.cards);

            // Mostrar efecto PEPINEADO si aplica
            if (playedCards.isPepineado) {
                setPepineadoPlayer(playedCards.playerName);
                setShowPepineado(true);
                setTimeout(() => setShowPepineado(false), 3000); // Ocultar después de 3 segundos
            }
        });

        // Evento cuando se reparten las cartas
        connection.on("CardsDealt", (playerHand: Card[]) => {
            console.log("🎴 Cartas repartidas:", playerHand);
            console.log(`📊 Cantidad de cartas recibidas: ${playerHand?.length || 0}`);
            if (playerHand && playerHand.length > 0) {
                console.log(`📋 Primeras cartas: ${playerHand.slice(0, 3).map(c => `${c.value}${c.suit}`).join(', ')}`);
            }
            setHand(playerHand);
        });

        // Evento cuando un jugador es saltado
        connection.on("PlayerSkipped", (playerName: string) => {
            console.log(`⏭️ ${playerName} fue saltado!`);
        });

        // Evento cuando alguien gana
        connection.on("PlayerWon", (playerName: string) => {
            console.log(`🏆 ${playerName} ganó!`);
            alert(`¡${playerName} ha ganado!`);
        });

        // Evento cuando el juego inicia
        connection.on("GameStarted", (roomId: string) => {
            console.log(`🎮 ¡El juego ha iniciado en la sala ${roomId}!`);
            // Solicitar actualización del estado
            connection.invoke("GetGameState", roomId).catch(err => {
                console.error("Error obteniendo estado del juego:", err);
            });
        });

        // Evento de error
        connection.on("Error", (msg: string) => {
            console.error("❌ Error del juego:", msg);
            alert(`Error: ${msg}`);
        });

        return () => {
            connection.off("PlayerJoined");
            connection.off("PlayerLeft");
            connection.off("GameStateUpdated");
            connection.off("CardsPlayed");
            connection.off("CardsDealt");
            connection.off("PlayerSkipped");
            connection.off("PlayerWon");
            connection.off("GameStarted");
            connection.off("Error");
        };
    }, [connection, playerName, roomId]);

    // Función para jugar cartas
    const playCards = useCallback(async (cards: Card[]) => {
        if (!connection) return;

        try {
            if (cards.length === 0) {
                // Pasar turno
                await connection.invoke("PassTurn", roomId);
            } else {
                // Jugar cartas
                await connection.invoke("PlayCards", roomId, cards);
            }
        } catch (error) {
            console.error("Error playing cards:", error);
        }
    }, [connection, roomId]);

    // Función para iniciar el juego
    const startGame = useCallback(async () => {
        if (!connection) return;

        try {
            await connection.invoke("StartGame", roomId);
        } catch (error) {
            console.error("Error starting game:", error);
        }
    }, [connection, roomId]);

    // Función para seleccionar modo de juego
    const selectGameMode = useCallback(async (deckCount: number) => {
        if (!connection) return;

        try {
            console.log(`🎯 Seleccionando modo de juego: ${deckCount} mazos`);
            await connection.invoke("SelectGameMode", roomId, deckCount);
            console.log("✅ Modo de juego seleccionado enviado al servidor");
        } catch (error) {
            console.error("Error selecting game mode:", error);
        }
    }, [connection, roomId]);

    return {
        connection,
        gameState,
        players: players ?? [],
        tableCards: tableCards ?? [],
        hand: hand ?? [],
        isConnected,
        isGameStarted,
        isMyTurn,
        lastPlayedCards: lastPlayedCards ?? [],
        isFirstPlay,
        gameMode,
        winners: winners ?? [],
        showPepineado,
        pepineadoPlayer,
        isRoomCreator,
        isNewRound,
        playCards,
        startGame,
        selectGameMode
    };
} 