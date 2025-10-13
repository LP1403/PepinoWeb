import { useState } from "react";
import { motion } from "framer-motion";

interface LobbyProps {
    onJoin: (roomId: string, name: string) => void;
}

export default function Lobby({ onJoin }: LobbyProps) {
    const [room, setRoom] = useState("");
    const [name, setName] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [showRules, setShowRules] = useState(false);

    const handleJoin = () => {
        if (!room.trim() || !name.trim()) {
            alert("Por favor ingresa un nombre y un ID de sala");
            return;
        }

        setIsLoading(true);
        // Simular un pequeño delay para la animación
        setTimeout(() => {
            onJoin(room.trim(), name.trim());
        }, 500);
    };

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            handleJoin();
        }
    };

    return (
        <motion.div
            className="lobby"
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
        >
            <div className="lobby-container">
                <motion.div
                    className="lobby-header"
                    initial={{ scale: 0.8 }}
                    animate={{ scale: 1 }}
                    transition={{ delay: 0.2, duration: 0.5 }}
                >
                    <h1>🥒 Pepino</h1>
                    <p>¡El clásico juego de naipes españoles en tiempo real!</p>
                    <div className="pepino-subtitle">
                        <span>🎯 Objetivo: Quedarse sin cartas</span>
                        <span>🥒 El 3 de Oro (♦) inicia el juego</span>
                    </div>
                </motion.div>

                <motion.div
                    className="lobby-form"
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.4, duration: 0.6 }}
                >
                    <div className="input-group">
                        <label htmlFor="name">Tu Nombre</label>
                        <input
                            id="name"
                            type="text"
                            placeholder="Ingresa tu nombre"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            onKeyPress={handleKeyPress}
                            maxLength={20}
                        />
                    </div>

                    <div className="input-group">
                        <label htmlFor="room">ID de Sala</label>
                        <input
                            id="room"
                            type="text"
                            placeholder="Ingresa el ID de la sala"
                            value={room}
                            onChange={(e) => setRoom(e.target.value)}
                            onKeyPress={handleKeyPress}
                            maxLength={10}
                        />
                    </div>

                    <motion.button
                        className="join-btn"
                        onClick={handleJoin}
                        disabled={isLoading || !room.trim() || !name.trim()}
                        whileHover={{ scale: 1.05 }}
                        whileTap={{ scale: 0.95 }}
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        transition={{ delay: 0.6, duration: 0.4 }}
                    >
                        {isLoading ? (
                            <motion.div
                                animate={{ rotate: 360 }}
                                transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
                            >
                                ⏳
                            </motion.div>
                        ) : (
                            "🎯 Unirse a la Sala"
                        )}
                    </motion.button>
                </motion.div>

                <motion.div
                    className="lobby-info"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.8, duration: 0.6 }}
                >
                    <div className="info-section">
                        <h3>📋 Instrucciones</h3>
                        <ul>
                            <li>Ingresa tu nombre para identificarte</li>
                            <li>Escribe el ID de la sala donde quieres jugar</li>
                            <li>Si la sala no existe, se creará automáticamente</li>
                            <li>Máximo 8 jugadores por sala</li>
                            <li>¡Disfruta jugando con amigos!</li>
                        </ul>
                    </div>

                    <div className="rules-section">
                        <motion.button
                            className="rules-btn"
                            onClick={() => setShowRules(!showRules)}
                            whileHover={{ scale: 1.05 }}
                            whileTap={{ scale: 0.95 }}
                        >
                            📖 {showRules ? 'Ocultar' : 'Ver'} Reglas del Juego
                        </motion.button>

                        {showRules && (
                            <motion.div
                                className="rules-content"
                                initial={{ opacity: 0, height: 0 }}
                                animate={{ opacity: 1, height: 'auto' }}
                                exit={{ opacity: 0, height: 0 }}
                                transition={{ duration: 0.3 }}
                            >
                                <h4>🥒 Reglas del Pepino</h4>
                                <div className="rules-grid">
                                    <div className="rule-item">
                                        <strong>🎯 Objetivo:</strong> Quedarse sin cartas
                                    </div>
                                    <div className="rule-item">
                                        <strong>🥒 Pepino de Oro:</strong> El 3♦ inicia el juego
                                    </div>
                                    <div className="rule-item">
                                        <strong>🎭 Jugadas:</strong> 1 hasta X cartas del mismo valor
                                    </div>
                                    <div className="rule-item">
                                        <strong>⚡ Turnos:</strong> El siguiente debe jugar cartas de mayor valor
                                    </div>
                                    <div className="rule-item">
                                        <strong>🥒 PEPINEADO:</strong> Misma jugada = salta al siguiente jugador
                                    </div>
                                    <div className="rule-item">
                                        <strong>🏆 Victoria:</strong> Quien se queda sin cartas gana
                                    </div>
                                </div>

                                <div className="professions-info">
                                    <h5>👥 Profesiones por Palo:</h5>
                                    <div className="professions-grid">
                                        <div>♠ <strong>Policías</strong></div>
                                        <div>♥ <strong>Médicos</strong></div>
                                        <div>♦ <strong>Soldados</strong></div>
                                        <div>♣ <strong>Bufones</strong></div>
                                    </div>
                                </div>
                            </motion.div>
                        )}
                    </div>
                </motion.div>
            </div>
        </motion.div>
    );
}