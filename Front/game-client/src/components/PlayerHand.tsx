/* eslint-disable @typescript-eslint/no-explicit-any */
import { useState } from 'react';
import { motion } from 'framer-motion';
import type { Card } from '../types/Card';
import { CardService } from '../services/CardService';
import AnimatedCard from './AnimatedCard';

interface PlayerHandProps {
    cards: Card[];
    onPlay: (cards: Card[]) => void;
    isMyTurn: boolean;
    lastPlayedCards: Card[] | null;
    isFirstPlay: boolean;
}

export default function PlayerHand({
    cards,
    onPlay,
    isMyTurn,
    lastPlayedCards,
    isFirstPlay
}: PlayerHandProps) {
    const [selectedCards, setSelectedCards] = useState<Card[]>([]);
    const [validationMessage, setValidationMessage] = useState<string>('');

    const handleCardClick = (card: Card) => {
        if (!isMyTurn) return;

        setSelectedCards(prev => {
            const isSelected = prev.some(c => c.id === card.id);

            if (isSelected) {
                // Deseleccionar carta
                return prev.filter(c => c.id !== card.id);
            } else {
                // Seleccionar carta
                const newSelection = [...prev, card];

                // Verificar que todas las cartas seleccionadas tengan el mismo valor
                const firstValue = newSelection[0].value;
                const allSameValue = newSelection.every(c => c.value === firstValue);

                if (!allSameValue) {
                    setValidationMessage('Solo puedes seleccionar cartas del mismo valor');
                    return prev; // No agregar la carta
                }

                setValidationMessage('');
                return newSelection;
            }
        });
    };

    const handlePlayCards = () => {
        if (selectedCards.length === 0) {
            setValidationMessage('Debes seleccionar al menos una carta');
            return;
        }

        const validation = CardService.validatePlay(selectedCards, lastPlayedCards, isFirstPlay);

        if (!validation.isValid) {
            setValidationMessage(validation.reason || 'Jugada inválida');
            return;
        }

        onPlay(selectedCards);
        setSelectedCards([]);
        setValidationMessage('');
    };

    const handlePass = () => {
        // Pasar turno (solo si no es la primera jugada)
        if (!isFirstPlay && lastPlayedCards && lastPlayedCards.length > 0) {
            onPlay([]); // Array vacío indica pasar
        }
    };

    // Agrupar cartas por valor para mostrar mejor
    const groupedCards = cards.reduce((groups, card) => {
        const value = card.value;
        if (!groups[value]) {
            groups[value] = [];
        }
        groups[value].push(card);
        return groups;
    }, {} as Record<Card['value'], Card[]>);

    const isCardSelected = (card: Card) => selectedCards.some(c => c.id === card.id);

    return (
        <div className="player-hand">
            <div className="hand-info">
                <h2>Tu Mano ({cards.length} cartas)</h2>
                {isMyTurn && (
                    <div className="turn-info">
                        <span className="turn-indicator active">¡Tu turno!</span>
                        {selectedCards.length > 0 && (
                            <span className="selected-count">
                                Seleccionadas: {selectedCards.length}
                            </span>
                        )}
                    </div>
                )}
            </div>

            {validationMessage && (
                <motion.div
                    className="validation-message"
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                >
                    {validationMessage}
                </motion.div>
            )}

            {/* Controles arriba */}
            {isMyTurn && (
                <div className="hand-controls-top">
                    <motion.button
                        className="play-btn"
                        onClick={handlePlayCards}
                        disabled={selectedCards.length === 0}
                        whileHover={{ scale: 1.05 }}
                        whileTap={{ scale: 0.95 }}
                    >
                        Jugar {selectedCards.length > 0 ? `(${selectedCards.length} carta${selectedCards.length > 1 ? 's' : ''})` : ''}
                    </motion.button>

                    {!isFirstPlay && lastPlayedCards && lastPlayedCards.length > 0 && (
                        <motion.button
                            className="pass-btn"
                            onClick={handlePass}
                            whileHover={{ scale: 1.05 }}
                            whileTap={{ scale: 0.95 }}
                        >
                            Pasar
                        </motion.button>
                    )}
                </div>
            )}

            {/* Cartas en una sola línea */}
            <div className="cards-container-single-line">
                {cards.map((card, index) => (
                    <motion.div
                        key={card.id}
                        style={{
                            zIndex: cards.length - index
                        }}
                        initial={{
                            opacity: 0,
                            y: 50,
                            rotateY: -90,
                            scale: 0.8
                        }}
                        animate={{
                            opacity: 1,
                            y: 0,
                            rotateY: 0,
                            scale: 1
                        }}
                        transition={{
                            duration: 0.5,
                            delay: index * 0.05,
                            type: "spring",
                            stiffness: 200
                        }}
                    >
                        <AnimatedCard
                            card={card}
                            isSelected={isCardSelected(card)}
                            isPlayable={isMyTurn}
                            onClick={() => handleCardClick(card)}
                            showValue={true}
                        />
                    </motion.div>
                ))}
            </div>

            {!isMyTurn && cards.length > 0 && (
                <div className="turn-indicator">
                    Esperando tu turno...
                </div>
            )}
        </div>
    );
}