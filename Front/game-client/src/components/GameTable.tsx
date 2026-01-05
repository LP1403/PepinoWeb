import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useGameConnection } from '../hooks/useGameConnection';
import PlayerHand from './PlayerHand';
import PepineadoEffect from './PepineadoEffect';
import AnimatedCard from './AnimatedCard';
import GameModeSelector from './GameModeSelector';
import PepinoArena from './PepinoArena';
import type { Card } from '../types/Card';

interface GameTableProps {
    roomId: string;
    playerName: string;
}

export default function GameTable({ roomId, playerName }: GameTableProps) {
    const {
        players,
        hand,
        isConnected,
        isGameStarted,
        isMyTurn,
        lastPlayedCards,
        isFirstPlay,
        gameMode,
        winners,
        showPepineado,
        pepineadoPlayer,
        isRoomCreator,
        isNewRound,
        playCards,
        startGame,
        selectGameMode
    } = useGameConnection({ roomId, playerName });

    // Log players array and warn on duplicate names to help debug multi-client issues
    React.useEffect(() => {
        try {
            console.log('📡 Players (debug):', players.map(p => ({ name: p.name, id: p.connectionId, cards: p.cardCount })));
            const names = players.map(p => p.name);
            const dup = names.filter((v, i, a) => a.indexOf(v) !== i);
            if (dup.length > 0) {
                console.warn('⚠️ Duplicate player names detected:', Array.from(new Set(dup)));
            }
        } catch (e) {
            console.error('Error logging players debug', e);
        }
    }, [players]);

    // Fallback: if backend stopped sending `isRoomCreator`, assume the first player is the creator
    const isCreator = isRoomCreator || (players && players.length > 0 && players[0].name === playerName);
    // Debug logs
    console.log("🎮 GameTable Debug:", {
        isRoomCreator,
        gameMode,
        isGameStarted,
        playersCount: players.length,
        isNewRound
    });

    // Log detallado para debugging
    console.log("🔍 Debug detallado:", {
        isRoomCreator: isRoomCreator,
        gameModeExists: !!gameMode,
        gameModeDeckCount: gameMode?.deckCount,
        isGameStarted: isGameStarted,
        playersCount: players.length,
        isNewRound: isNewRound,
        shouldShowSelector: isRoomCreator && !gameMode,
        shouldShowStartButton: isRoomCreator && gameMode,
        shouldShowWaiting: !isRoomCreator
    });

    // Log adicional para debugging del creador
    console.log("👑 Debug del creador:", {
        playerName,
        isRoomCreator,
        gameMode: gameMode?.deckCount,
        isNewRound: isNewRound,
        shouldShowStartButton: isRoomCreator && gameMode && !isGameStarted
    });

    const handlePlayCards = async (cards: Card[]) => {
        await playCards(cards);
    };

    const handleStartGame = async () => {
        await startGame();
    };

    const handleSelectGameMode = async (deckCount: number) => {
        await selectGameMode(deckCount);
    };

    if (!isConnected) {
        return (
            <div className="loading">
                <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ duration: 0.5 }}
                >
                    Conectando al servidor...
                </motion.div>
            </div>
        );
    }
    // Render the full-screen Pepino arena to match the requested composition
    const arenaPlayers = players.map(p => ({ id: p.connectionId, name: p.name, cards: p.cardCount, avatarColor: undefined }));

    // Determine local player's connectionId more robustly: match by name + hand size when possible
    const localPlayer = players.find(p => p.name === playerName && p.cardCount === (hand?.length ?? 0))
        || players.find(p => p.name === playerName)
        || players[0];

    const localConnectionId = localPlayer?.connectionId;

    return (
        <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
            {/* Debug panel: muestra jugadores recibidos del servidor */}
            <div style={{ position: 'fixed', top: 8, left: 8, zIndex: 1100, background: 'rgba(0,0,0,0.6)', color: '#fff', padding: 8, borderRadius: 8, fontSize: 12, maxWidth: 340, maxHeight: 220, overflow: 'auto' }}>
                <strong>Debug players</strong>
                <div style={{ marginTop: 6 }}>
                    {players.map(p => (
                        <div key={p.connectionId || p.name} style={{ marginBottom: 6 }}>
                            <div>
                                <strong>{p.name}</strong>
                                {p.connectionId === localConnectionId && (
                                    <span style={{ marginLeft: 8, color: '#4caf50' }}>(Tú)</span>
                                )}
                            </div>
                            <div style={{ opacity: 0.8 }}>id: {p.connectionId || '—'}</div>
                            <div style={{ opacity: 0.8 }}>cards: {p.cardCount ?? 0}</div>
                        </div>
                    ))}
                </div>
            </div>
            <PepinoArena players={arenaPlayers} bottomPlayerId={localConnectionId} lastPlayedCards={lastPlayedCards} />
            {/* Keep Pepineado effect over the arena */}
            <PepineadoEffect isVisible={showPepineado} playerName={pepineadoPlayer} />

            {/* Overlay: game controls and selector */}
            <div style={{ position: 'fixed', top: 24, left: '50%', transform: 'translateX(-50%)', zIndex: 1000, pointerEvents: 'auto' }}>
                {!isGameStarted && (
                    <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                        {isCreator && !gameMode && (
                            <GameModeSelector onSelectMode={handleSelectGameMode} playerCount={Math.max(1, players.length)} currentMode={gameMode?.deckCount} />
                        )}

                        {isCreator && gameMode && (
                            <div style={{ background: 'rgba(0,0,0,0.6)', color: '#fff', padding: 12, borderRadius: 10 }}>
                                <div>Modo: {gameMode.deckCount} mazo{gameMode.deckCount > 1 ? 's' : ''}</div>
                                <div>{gameMode.cardsPerPlayer} cartas por jugador • {gameMode.maxWinners} ganadores</div>
                                <button className="start-game-btn" onClick={handleStartGame} style={{ marginTop: 8 }}>Iniciar Juego</button>
                            </div>
                        )}

                        {!isCreator && (
                            <div style={{ background: 'rgba(0,0,0,0.6)', color: '#fff', padding: 12, borderRadius: 10 }}>
                                Esperando que el creador inicie la partida...
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* Player hand overlay at bottom */}
            <div style={{ position: 'fixed', left: 0, right: 0, bottom: 8, zIndex: 1000, display: 'flex', justifyContent: 'center', pointerEvents: 'auto' }}>
                <div style={{ width: '88%', maxWidth: 1100 }}>
                    <PlayerHand
                        cards={hand}
                        onPlay={handlePlayCards}
                        isMyTurn={isMyTurn}
                        lastPlayedCards={lastPlayedCards}
                        isFirstPlay={isFirstPlay}
                        isNewRound={isNewRound}
                    />
                </div>
            </div>
        </div>
    );
}