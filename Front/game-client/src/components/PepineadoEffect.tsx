import { motion, AnimatePresence } from 'framer-motion';

interface PepineadoEffectProps {
    isVisible: boolean;
    playerName: string;
}

export default function PepineadoEffect({ isVisible, playerName }: PepineadoEffectProps) {
    return (
        <AnimatePresence>
            {isVisible && (
                <motion.div
                    className="pepineado-overlay"
                    initial={{ opacity: 0, scale: 0.5 }}
                    animate={{ opacity: 1, scale: 1 }}
                    exit={{ opacity: 0, scale: 1.5 }}
                    transition={{ duration: 0.8, type: "spring", stiffness: 200 }}
                >
                    <div className="pepineado-content">
                        <motion.div
                            className="pepineado-text"
                            initial={{ y: -50, opacity: 0 }}
                            animate={{ y: 0, opacity: 1 }}
                            transition={{ delay: 0.2, duration: 0.6 }}
                        >
                            🥒 PEPINEADO! 🥒
                        </motion.div>
                        <motion.div
                            className="pepineado-player"
                            initial={{ y: 50, opacity: 0 }}
                            animate={{ y: 0, opacity: 1 }}
                            transition={{ delay: 0.4, duration: 0.6 }}
                        >
                            {playerName} salta al siguiente!
                        </motion.div>

                        {/* Efectos de partículas */}
                        <motion.div
                            className="pepineado-particles"
                            initial={{ scale: 0 }}
                            animate={{ scale: 1 }}
                            transition={{ delay: 0.6, duration: 0.4 }}
                        >
                            {[...Array(8)].map((_, i) => (
                                <motion.div
                                    key={i}
                                    className="particle"
                                    initial={{
                                        x: 0,
                                        y: 0,
                                        opacity: 1,
                                        scale: 0
                                    }}
                                    animate={{
                                        x: Math.cos(i * 45 * Math.PI / 180) * 100,
                                        y: Math.sin(i * 45 * Math.PI / 180) * 100,
                                        opacity: 0,
                                        scale: 1
                                    }}
                                    transition={{
                                        delay: 0.8 + i * 0.1,
                                        duration: 1,
                                        ease: "easeOut"
                                    }}
                                >
                                    🥒
                                </motion.div>
                            ))}
                        </motion.div>
                    </div>
                </motion.div>
            )}
        </AnimatePresence>
    );
} 