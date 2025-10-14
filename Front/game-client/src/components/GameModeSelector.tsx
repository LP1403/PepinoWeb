import { motion } from 'framer-motion';

interface GameModeSelectorProps {
    onSelectMode: (deckCount: number) => void;
    currentMode?: number;
    playerCount?: number;
}

export default function GameModeSelector({ onSelectMode, currentMode, playerCount = 1 }: GameModeSelectorProps) {
    const getRecommendedMode = () => {
        const count = playerCount || 1;
        if (count <= 2) return 2;
        if (count <= 4) return 1;
        if (count <= 6) return 2;
        return 3;
    };

    const getCardsPerPlayer = (deckCount: number) => {
        const count = playerCount || 1;
        return Math.floor((deckCount * 40) / count);
    };

    const recommendedMode = getRecommendedMode();

    // Debug logs
    console.log("🎯 GameModeSelector Debug:", {
        currentMode,
        playerCount,
        recommendedMode
    });

    const handleModeSelect = (deckCount: number) => {
        console.log(`🎯 Seleccionando modo desde componente: ${deckCount} mazos`);
        onSelectMode(deckCount);
    };

    return (
        <motion.div
            className="game-mode-selector"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
        >
            <h3>🎯 Seleccionar Modo de Juego</h3>
            <p className="mode-description">
                Elige cuántos mazos usar para esta partida. Más mazos = más cartas por jugador = partida más larga.
            </p>

            <div className="mode-options">
                {[1, 2, 3].map((deckCount) => {
                    const cardsPerPlayer = getCardsPerPlayer(deckCount);
                    const isRecommended = deckCount === recommendedMode;
                    const isSelected = currentMode === deckCount;

                    return (
                        <motion.button
                            key={deckCount}
                            className={`mode-option ${isSelected ? 'selected' : ''} ${isRecommended ? 'recommended' : ''}`}
                            onClick={() => handleModeSelect(deckCount)}
                            whileHover={{ scale: 1.05 }}
                            whileTap={{ scale: 0.95 }}
                            initial={{ opacity: 0, x: -20 }}
                            animate={{ opacity: 1, x: 0 }}
                            transition={{ delay: deckCount * 0.1 }}
                        >
                            <div className="mode-header">
                                <span className="deck-count">{deckCount} Mazo{deckCount > 1 ? 's' : ''}</span>
                                {isRecommended && <span className="recommended-badge">⭐ Recomendado</span>}
                            </div>
                            <div className="mode-details">
                                <span className="cards-info">{cardsPerPlayer} cartas por jugador</span>
                                <span className="total-cards">{deckCount * 40} cartas totales</span>
                            </div>
                            {isSelected && (
                                <motion.div
                                    className="selected-indicator"
                                    initial={{ scale: 0 }}
                                    animate={{ scale: 1 }}
                                    transition={{ type: "spring", stiffness: 500 }}
                                >
                                    ✅ Seleccionado
                                </motion.div>
                            )}
                        </motion.button>
                    );
                })}
            </div>

            <div className="mode-info">
                <div className="info-item">
                    <strong>👥 Jugadores:</strong> {playerCount}
                </div>
                <div className="info-item">
                    <strong>🎯 Modo recomendado:</strong> {recommendedMode} mazo{recommendedMode > 1 ? 's' : ''} ({getCardsPerPlayer(recommendedMode)} cartas por jugador)
                </div>
            </div>
        </motion.div>
    );
} 