import { motion, AnimatePresence } from 'framer-motion';
import { useGameConnection } from '../hooks/useGameConnection';
import PlayerHand from './PlayerHand';
import PepineadoEffect from './PepineadoEffect';
import AnimatedCard from './AnimatedCard';
import GameModeSelector from './GameModeSelector';
import type { Card } from '../types/Card';

interface GameTableProps {
    roomId: string;
    playerName: string;
}

export default function GameTable({ roomId, playerName }: GameTableProps) {
    const {
        players,
        tableCards,
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
        playCards,
        startGame,
        selectGameMode
    } = useGameConnection({ roomId, playerName });

    // Debug logs
    console.log("🎮 GameTable Debug:", {
        isRoomCreator,
        gameMode,
        isGameStarted,
        playersCount: players.length
    });

    // Log detallado para debugging
    console.log("🔍 Debug detallado:", {
        isRoomCreator: isRoomCreator,
        gameModeExists: !!gameMode,
        gameModeDeckCount: gameMode?.deckCount,
        isGameStarted: isGameStarted,
        playersCount: players.length,
        shouldShowSelector: isRoomCreator && !gameMode,
        shouldShowStartButton: isRoomCreator && gameMode,
        shouldShowWaiting: !isRoomCreator
    });

    // Log adicional para debugging del creador
    console.log("👑 Debug del creador:", {
        playerName,
        isRoomCreator,
        gameMode: gameMode?.deckCount,
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

    return (
        <div className="game-table">
            {/* Efecto PEPINEADO */}
            <PepineadoEffect
                isVisible={showPepineado}
                playerName={pepineadoPlayer}
            />

            <div className="game-header">
                <h1>🥒 Pepino - Sala: {roomId}</h1>
                <p>Jugador: {playerName}</p>
                <div className="connection-status">
                    {isConnected ? '🟢 Conectado' : '🔴 Desconectado'}
                </div>

                {gameMode && (
                    <div className="game-mode-info">
                        <span>Mazos: {gameMode.deckCount}</span>
                        <span>•</span>
                        <span>Ganadores máx: {gameMode.maxWinners}</span>
                        <span>•</span>
                        <span>Cartas por jugador: {gameMode.cardsPerPlayer}</span>
                    </div>
                )}
            </div>

            <div className="game-content">
                {/* Lista de jugadores */}
                <div className="players-section">
                    <h2>Jugadores ({players.length}/8)</h2>
                    <div className="players-list">
                        {players.map((player, index) => (
                            <motion.div
                                key={player.connectionId}
                                className={`player-item ${player.name === playerName ? 'current-player' : ''} ${player.isCurrentTurn ? 'current-turn' : ''} ${player.isSkipped ? 'skipped' : ''} ${player.hasWon ? 'winner' : ''}`}
                                initial={{ opacity: 0, x: -20 }}
                                animate={{ opacity: 1, x: 0 }}
                                transition={{ delay: index * 0.1 }}
                            >
                                <div className="player-info">
                                    <span className="player-name">{player.name}</span>
                                    {player.name === playerName && <span className="you-indicator">(Tú)</span>}
                                    {player.isCurrentTurn && <span className="turn-indicator">🎯</span>}
                                    {player.isSkipped && <span className="skipped-indicator">⏭️</span>}
                                    {player.hasWon && <span className="winner-indicator">🏆</span>}
                                </div>
                                <span className="cards-count">({player.cardCount || 0} cartas)</span>
                            </motion.div>
                        ))}
                    </div>
                </div>

                {/* Mesa de juego */}
                <div className="table-section">
                    <h2>Mesa</h2>

                    {/* Información de la última jugada */}
                    {lastPlayedCards && lastPlayedCards.length > 0 && (
                        <div className="last-play-info">
                            <span>Última jugada: {lastPlayedCards.length} carta{lastPlayedCards.length > 1 ? 's' : ''} de valor {lastPlayedCards[0].value}</span>
                        </div>
                    )}

                    <div className="table-cards">
                        <AnimatePresence>
                            {tableCards.map((card, index) => (
                                <motion.div
                                    key={`${card.id}-${index}`}
                                    initial={{
                                        opacity: 0,
                                        scale: 0.5,
                                        y: 50,
                                        rotate: -180
                                    }}
                                    animate={{
                                        opacity: 1,
                                        scale: 1,
                                        y: 0,
                                        rotate: 0
                                    }}
                                    exit={{
                                        opacity: 0,
                                        scale: 0.5,
                                        y: -50
                                    }}
                                    transition={{
                                        duration: 0.6,
                                        type: "spring",
                                        stiffness: 150
                                    }}
                                >
                                    <AnimatedCard
                                        card={card}
                                        isSelected={false}
                                        isPlayable={false}
                                        showValue={true}
                                        className="table-card"
                                    />
                                </motion.div>
                            ))}
                        </AnimatePresence>
                    </div>
                </div>

                {/* Controles del juego */}
                <div className="game-controls">
                    {!isGameStarted && players.length >= 1 && (
                        <div className="game-setup">
                            {(() => {
                                console.log("🎮 Renderizando controles:", {
                                    isRoomCreator,
                                    gameMode: gameMode?.deckCount,
                                    shouldShowSelector: isRoomCreator && !gameMode,
                                    shouldShowStartButton: isRoomCreator && gameMode,
                                    shouldShowWaiting: !isRoomCreator
                                });

                                // Si es el creador y no hay modo seleccionado, mostrar selector
                                if (isRoomCreator && !gameMode) {
                                    console.log("🎯 Mostrando selector de modo (eres el creador)");
                                    return (
                                        <GameModeSelector
                                            onSelectMode={handleSelectGameMode}
                                            playerCount={players.length}
                                        />
                                    );
                                }

                                // Si es el creador y hay modo seleccionado, mostrar botón de iniciar
                                if (isRoomCreator && gameMode) {
                                    console.log("🎮 Mostrando botón de iniciar juego (modo seleccionado)");
                                    return (
                                        <motion.div
                                            className="game-ready"
                                            initial={{ opacity: 0, y: 20 }}
                                            animate={{ opacity: 1, y: 0 }}
                                            transition={{ duration: 0.5 }}
                                        >
                                            <div className="mode-selected">
                                                <h3>✅ Modo seleccionado: {gameMode.deckCount} mazo{gameMode.deckCount > 1 ? 's' : ''}</h3>
                                                <p>{gameMode.cardsPerPlayer} cartas por jugador • {gameMode.maxWinners} ganadores máximos</p>
                                            </div>
                                            <motion.button
                                                className="start-game-btn"
                                                onClick={handleStartGame}
                                                whileHover={{ scale: 1.05 }}
                                                whileTap={{ scale: 0.95 }}
                                                style={{
                                                    background: '#4caf50',
                                                    color: 'white',
                                                    fontSize: '16px',
                                                    padding: '15px 30px',
                                                    border: 'none',
                                                    borderRadius: '8px',
                                                    cursor: 'pointer',
                                                    fontWeight: 'bold'
                                                }}
                                            >
                                                🎮 Iniciar Juego de Pepino
                                            </motion.button>
                                        </motion.div>
                                    );
                                }

                                // Si no es el creador, mostrar mensaje de espera
                                if (!isRoomCreator) {
                                    console.log("⏳ Mostrando mensaje de espera (no eres el creador)");
                                    return (
                                        <div className="waiting-creator">
                                            <motion.div
                                                animate={{ rotate: 360 }}
                                                transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
                                            >
                                                ⏳
                                            </motion.div>
                                            <p>Esperando que el creador de la sala inicie el juego...</p>
                                        </div>
                                    );
                                }

                                return null;
                            })()}
                        </div>
                    )}

                    {!isGameStarted && players.length < 1 && (
                        <div className="waiting-players">
                            <motion.div
                                animate={{ scale: [1, 1.1, 1] }}
                                transition={{ duration: 1, repeat: Infinity }}
                            >
                                👥
                            </motion.div>
                            <p>Esperando jugadores... ({players.length}/1 mínimo)</p>
                        </div>
                    )}

                    {isGameStarted && winners.length > 0 && (
                        <div className="winners-section">
                            <h3>🏆 Ganadores:</h3>
                            <div className="winners-list">
                                {winners.map((winnerId, index) => {
                                    const winner = players.find(p => p.connectionId === winnerId);
                                    return (
                                        <motion.div
                                            key={winnerId}
                                            className="winner-item"
                                            initial={{ opacity: 0, scale: 0.8 }}
                                            animate={{ opacity: 1, scale: 1 }}
                                            transition={{ delay: index * 0.2 }}
                                        >
                                            {index + 1}. {winner?.name || 'Jugador'}
                                        </motion.div>
                                    );
                                })}
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {/* Mano del jugador */}
            <div className="player-hand-section">
                <PlayerHand
                    cards={hand}
                    onPlay={handlePlayCards}
                    isMyTurn={isMyTurn}
                    lastPlayedCards={lastPlayedCards}
                    isFirstPlay={isFirstPlay}
                />
            </div>
        </div>
    );
}