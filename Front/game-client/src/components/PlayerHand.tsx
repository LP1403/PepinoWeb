import { useState, useRef } from 'react';
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
    isNewRound?: boolean;
}

export default function PlayerHand({
    cards,
    onPlay,
    isMyTurn,
    lastPlayedCards,
    isFirstPlay,
    isNewRound = false
}: PlayerHandProps) {
    const [selectedCards, setSelectedCards] = useState<Card[]>([]);
    const [validationMessage, setValidationMessage] = useState<string>('');
    const [carouselOffset, setCarouselOffset] = useState(0);
    const [isDragging, setIsDragging] = useState(false);
    const [dragStart, setDragStart] = useState(0);
    const [dragCurrent, setDragCurrent] = useState(0);
    const carouselRef = useRef<HTMLDivElement>(null);

    // Funciones de navegación del carrusel semicircular
    const scrollLeft = () => {
        if (cards.length <= 5) return; // No navegar si hay 5 o menos cartas
        const newOffset = Math.max(0, carouselOffset - 1);
        console.log('⬅️ Scroll Left:', { from: carouselOffset, to: newOffset, totalCards: cards.length });
        setCarouselOffset(newOffset);
    };

    const scrollRight = () => {
        if (cards.length <= 5) return; // No navegar si hay 5 o menos cartas
        const maxOffset = Math.max(0, cards.length - 5); // Mostrar máximo 5 cartas visibles
        const newOffset = Math.min(maxOffset, carouselOffset + 1);
        console.log('➡️ Scroll Right:', { from: carouselOffset, to: newOffset, maxOffset, totalCards: cards.length });
        setCarouselOffset(newOffset);
    };

    // Funciones de drag
    const handleMouseDown = (e: React.MouseEvent) => {
        if (!isMyTurn) return;
        setIsDragging(true);
        setDragStart(e.clientX);
        setDragCurrent(e.clientX);
    };

    const handleMouseMove = (e: React.MouseEvent) => {
        if (!isDragging || !isMyTurn) return;
        setDragCurrent(e.clientX);
    };

    const handleMouseUp = () => {
        if (!isDragging) return;
        setIsDragging(false);

        const dragDistance = dragStart - dragCurrent;
        if (Math.abs(dragDistance) > 50) {
            if (dragDistance > 0) {
                scrollRight();
            } else {
                scrollLeft();
            }
        }
    };

    const handleTouchStart = (e: React.TouchEvent) => {
        if (!isMyTurn) return;
        setIsDragging(true);
        setDragStart(e.touches[0].clientX);
        setDragCurrent(e.touches[0].clientX);
    };

    const handleTouchMove = (e: React.TouchEvent) => {
        if (!isDragging || !isMyTurn) return;
        setDragCurrent(e.touches[0].clientX);
    };

    const handleTouchEnd = () => {
        if (!isDragging) return;
        setIsDragging(false);

        const dragDistance = dragStart - dragCurrent;
        if (Math.abs(dragDistance) > 50) {
            if (dragDistance > 0) {
                scrollRight();
            } else {
                scrollLeft();
            }
        }
    };

    // Calcular qué cartas mostrar en el carrusel
    const getVisibleCards = () => {
        const sortedCards = cards.sort((a, b) => a.value - b.value);
        const visibleCount = 5; // Máximo 5 cartas visibles
        const startIndex = carouselOffset;
        const endIndex = Math.min(startIndex + visibleCount, sortedCards.length);
        const visibleCards = sortedCards.slice(startIndex, endIndex);

        console.log('🎠 Carrusel Debug:', {
            totalCards: cards.length,
            carouselOffset,
            startIndex,
            endIndex,
            visibleCards: visibleCards.length,
            cardValues: visibleCards.map(c => c.value)
        });

        return visibleCards;
    };

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

        console.log('🎯 DEBUG PlayerHand - Validando jugada:');
        console.log(`   🃏 Cartas seleccionadas: ${selectedCards.length} cartas`);
        console.log(`   🎮 Primera jugada: ${isFirstPlay}`);
        console.log(`   🔄 Nueva ronda: ${isNewRound}`);
        console.log(`   📋 Última jugada: ${lastPlayedCards?.length || 0} cartas`);

        const validation = CardService.validatePlay(selectedCards, lastPlayedCards, isFirstPlay, isNewRound);

        console.log(`   ✅ ¿Es válida? ${validation.isValid}`);
        if (!validation.isValid) {
            console.log(`   ❌ Razón: ${validation.reason}`);
        }

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

    const isCardSelected = (card: Card) => selectedCards.some(c => c.id === card.id);

    return (
        <div className="player-hand">
            <div className="hand-header">
                <div className="hand-title-section">
                    <h2>Tu Mano ({cards.length} cartas)</h2>
                    {isMyTurn && (
                        <div className="turn-info">
                            {isNewRound && (
                                <span className="new-round-indicator">🔄 Nueva ronda - Juega libremente</span>
                            )}
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
                        className="validation-message-inline"
                        initial={{ opacity: 0, y: -10 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -10 }}
                    >
                        {validationMessage}
                    </motion.div>
                )}

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
            </div>

            {/* Mensaje de espera - arriba de las cartas */}
            {!isMyTurn && cards.length > 0 && (
                <div className="turn-indicator">
                    Esperando tu turno...
                </div>
            )}

            {/* Carrusel de cartas */}
            <div className="cards-carousel-container">
                {/* Flecha izquierda */}
                <button
                    className="carousel-arrow carousel-arrow-left"
                    onClick={scrollLeft}
                    disabled={cards.length <= 5 || carouselOffset <= 0}
                >
                    ‹
                </button>

                {/* Contenedor del carrusel semicircular */}
                <div
                    ref={carouselRef}
                    className="cards-carousel"
                    onMouseDown={handleMouseDown}
                    onMouseMove={handleMouseMove}
                    onMouseUp={handleMouseUp}
                    onMouseLeave={handleMouseUp}
                    onTouchStart={handleTouchStart}
                    onTouchMove={handleTouchMove}
                    onTouchEnd={handleTouchEnd}
                    style={{ cursor: isDragging ? 'grabbing' : 'grab' }}
                >
                    <div className="cards-carousel-content">
                        {getVisibleCards().map((card, index) => (
                            <motion.div
                                key={card.id}
                                className="carousel-card"
                                style={{
                                    zIndex: getVisibleCards().length - index,
                                    '--card-transform': `translateX(${(index - 2) * 50}px) rotate(${(index - 2) * 10}deg)`
                                } as React.CSSProperties}
                                initial={{
                                    opacity: 0,
                                    y: 100,
                                    rotate: 0
                                }}
                                animate={{
                                    opacity: 1,
                                    y: 0,
                                    rotate: (index - 2) * 10
                                }}
                                transition={{
                                    duration: 0.6,
                                    delay: index * 0.1,
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
                </div>

                {/* Flecha derecha */}
                <button
                    className="carousel-arrow carousel-arrow-right"
                    onClick={scrollRight}
                    disabled={cards.length <= 5 || carouselOffset >= Math.max(0, cards.length - 5)}
                >
                    ›
                </button>
            </div>
        </div>
    );
}