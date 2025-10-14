import { motion } from 'framer-motion';
import type { Card } from '../types/Card';

interface AnimatedCardProps {
    card: Card;
    isSelected?: boolean;
    isPlayable?: boolean;
    onClick?: () => void;
    className?: string;
    showValue?: boolean;
}

export default function AnimatedCard({
    card,
    isSelected = false,
    isPlayable = false,
    onClick,
    className = "",
    showValue = true
}: AnimatedCardProps) {
    // Función para obtener la imagen de la carta
    const getCardImage = (card: Card): string => {
        // Comodín (valor 2) - siempre usa comodin.png
        if (card.value === 2) {
            return '/src/assets/cartas/comodin.png';
        }

        // 3 de Oro - usa pepinooro.png
        if (card.suit === '♦' && card.value === 3) {
            return '/src/assets/cartas/pepinooro.png';
        }

        // Mapeo de palos a profesiones y valores a imágenes
        const suitProfessionMap = {
            '♠': 'policia', // Policía
            '♥': 'bombero', // Médico (usa bombero)
            '♦': 'gaucho',  // Soldado (usa gaucho)
            '♣': 'frances'  // Bufón (usa francés)
        };

        const profession = suitProfessionMap[card.suit];
        const value = card.value;

        // Solo usar imágenes para valores 1 y 3
        if (value === 1 || value === 3) {
            return `/src/assets/cartas/${profession}${value}.png`;
        }

        // Para otros valores, no usar imagen personalizada
        return '';
    };

    const cardImage = getCardImage(card);

    // Mapeo de palos a profesiones
    const suitProfessions = {
        '♠': { name: 'Policía', icon: '👮', color: '#2C3E50' },
        '♥': { name: 'Médico', icon: '👨‍⚕️', color: '#E74C3C' },
        '♦': { name: 'Soldado', icon: '💂', color: '#E67E22' },
        '♣': { name: 'Bufón', icon: '🤡', color: '#27AE60' }
    };

    const profession = suitProfessions[card.suit as keyof typeof suitProfessions];

    // Valor numérico para comparaciones
    const getValueDisplay = (value: number) => {
        const valueMap: { [key: number]: string } = {
            1: 'A', 2: '2', 3: '3', 4: '4', 5: '5', 6: '6',
            7: '7', 8: '8', 9: '9', 10: '10', 11: 'J', 12: 'Q'
        };
        return valueMap[value] || value.toString();
    };

    const cardVariants = {
        initial: {
            scale: 0.8,
            rotateY: -180,
            opacity: 0
        },
        animate: {
            scale: 1,
            rotateY: 0,
            opacity: 1
        },
        hover: {
            scale: isPlayable ? 1.1 : 1.05,
            y: isPlayable ? -10 : -5
        },
        selected: {
            scale: 1.2,
            y: -20,
            boxShadow: "0 25px 50px rgba(76, 175, 80, 0.6)",
            border: "3px solid #4CAF50"
        }
    };

    return (
        <motion.div
            className={`animated-card ${className} ${isSelected ? 'selected' : ''} ${isPlayable ? 'playable' : ''}`}
            variants={cardVariants}
            initial="initial"
            animate={isSelected ? "selected" : "animate"}
            whileHover={isPlayable ? "hover" : undefined}
            onClick={onClick}
            style={{
                cursor: isPlayable ? 'pointer' : 'default',
                background: 'linear-gradient(135deg, #ffffff, #f8f9fa)',
                border: `2px solid ${profession.color}`,
                boxShadow: '0 4px 15px rgba(0, 0, 0, 0.1)'
            }}
        >

            <div className="card-content">
                {/* Contenido de la carta */}
                {cardImage ? (
                    /* Imagen personalizada - no mostrar valores ni iconos */
                    <div className="card-image-container">
                        <img
                            src={cardImage}
                            alt={`${profession.name} ${card.value}`}
                            className="card-image"
                            style={{
                                width: '100%',
                                height: '100%',
                                objectFit: 'contain',
                                borderRadius: '4px'
                            }}
                        />
                    </div>
                ) : (
                    /* Fallback con iconos y valores */
                    <>
                        {/* Valor superior */}
                        {showValue && (
                            <div className="card-value top" style={{ color: profession.color }}>
                                {getValueDisplay(card.value)}
                            </div>
                        )}

                        {/* Profesión central */}
                        <div className="card-profession">
                            <div className="profession-icon" style={{ fontSize: '2.5em' }}>
                                {profession.icon}
                            </div>
                            <div className="profession-name" style={{
                                color: profession.color,
                                fontSize: '0.8em',
                                fontWeight: 'bold',
                                textAlign: 'center'
                            }}>
                                {profession.name}
                            </div>
                        </div>

                        {/* Valor inferior */}
                        {showValue && (
                            <div className="card-value bottom" style={{ color: profession.color }}>
                                {getValueDisplay(card.value)}
                            </div>
                        )}
                    </>
                )}


                {/* Detalles de la profesión según el valor 
                <div className="profession-details">
                    {card.value === 1 && (
                        <div className="detail-icon">👑</div>
                    )}
                    {card.value === 2 && (
                        <div className="detail-icon">🎭</div>
                    )}
                    {card.value === 11 && (
                        <div className="detail-icon">⚔️</div>
                    )}
                    {card.value === 12 && (
                        <div className="detail-icon">👸</div>
                    )}
                </div>
                */}
            </div>

            {/* Efecto de selección */}
            {isSelected && (
                <motion.div
                    className="selection-indicator"
                    initial={{ scale: 0, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ duration: 0.3 }}
                    style={{
                        position: 'absolute',
                        top: '-10px',
                        right: '-10px',
                        background: '#4CAF50',
                        color: 'white',
                        borderRadius: '50%',
                        width: '30px',
                        height: '30px',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '16px',
                        fontWeight: 'bold',
                        zIndex: 1000,
                        boxShadow: '0 4px 8px rgba(0,0,0,0.3)'
                    }}
                >
                    ✓
                </motion.div>
            )}
        </motion.div>
    );
} 