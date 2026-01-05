import React from 'react';
import './PepinoArena.css';
import mesaFondo from '../assets/mesaFondo.jpg';
import pepinoOro from '../assets/cartas/pepinooro.png';
import AnimatedCard from './AnimatedCard';
import type { Card } from '../types/Card';

interface ArenaPlayer {
    id: string;
    name: string;
    cards: number;
    avatarColor?: string;
}

interface PepinoArenaProps {
    players: ArenaPlayer[];
    bottomPlayerId?: string;
    lastPlayedCards?: Card[] | null;
}

export default function PepinoArena({ players, bottomPlayerId, lastPlayedCards = [] }: PepinoArenaProps) {
    const bottomIdx = players.findIndex(p => p.id === bottomPlayerId);

    // Rotate players array so the local (bottom) player is at index 0 — ensures consistent positioning
    const orderedPlayers = React.useMemo(() => {
        if (!players || players.length === 0) return players;
        if (bottomIdx <= 0) return players; // if bottom not found or already first, keep order
        const first = players.slice(bottomIdx);
        const rest = players.slice(0, bottomIdx);
        return [...first, ...rest];
    }, [players, bottomIdx]);

    return (
        <div className="pepino-arena-root">
            <img src={mesaFondo} alt="mesa" className="mesa-bg" />

            <div className="arena-overlay">
                <div className="arena-center">
                    <img src={pepinoOro} className="pepino-token" alt="pepino" />

                    {/* Render last played cards in center */}
                    <div className="center-play-cards">
                        {(lastPlayedCards ?? []).map((c, idx) => (
                            <div key={c.id} className="center-card-wrapper" style={{ left: `${idx * 30}px`, zIndex: 50 - idx }}>
                                <AnimatedCard card={c} showValue={true} className="center-played-card" />
                            </div>
                        ))}
                    </div>
                </div>

                {orderedPlayers.map((p, i) => {
                    // precomputed positions to mimic UNO composition
                    const positions = [
                        { top: '78%', left: '50%', transform: 'translate(-50%, 0%)' }, // bottom center (you)
                        { top: '8%', left: '50%', transform: 'translate(-50%, 0%)' }, // top center
                        { top: '22%', left: '12%', transform: 'translate(0%, -50%)' }, // left-upper
                        { top: '22%', left: '88%', transform: 'translate(-100%, -50%)' }, // right-upper
                        { top: '50%', left: '6%', transform: 'translate(0%, -50%)' }, // left-mid
                        { top: '50%', left: '94%', transform: 'translate(-100%, -50%)' } // right-mid
                    ];

                    const pos = positions[i] || { top: `${10 + (i * 12)}%`, left: `${50 + (i * 6)}%` };

                    const visible = Math.min(p.cards, 6);
                    const cardWidth = 60; // px, keep in sync with CSS
                    const gap = 18; // spacing between card centers
                    const totalWidth = visible > 0 ? (cardWidth + (visible - 1) * gap) : 0;

                    return (
                        <div key={p.id} className={`arena-player ${p.id === bottomPlayerId ? 'bottom-player' : ''}`} style={pos}>
                            <div className="player-avatar" style={{ background: p.avatarColor || '#3b82f6' }}>{p.name.charAt(0)}</div>
                            <div className="player-name">
                                {p.name}
                                {p.id && (
                                    <div className="player-id-short">{`#${String(p.id).slice(-4)}`}</div>
                                )}
                            </div>
                            <div className="player-stack" style={{ width: `${totalWidth}px`, position: 'relative', marginTop: 8 }}>
                                {/* render small fanned stack centered within container */}
                                {Array.from({ length: visible }).map((_, idx) => {
                                    const offset = (idx - (visible - 1) / 2) * gap;
                                    const rotation = (idx - (visible - 1) / 2) * 4; // gentler rotation
                                    const cardLeft = (totalWidth / 2) + offset - (cardWidth / 2);
                                    return (
                                        <div key={idx} className="stack-card" style={{ left: `${cardLeft}px`, transform: `rotate(${rotation}deg)` }} />
                                    );
                                })}
                                {p.cards > visible && (() => {
                                    const rightmostOffset = ((visible - 1) / 2) * gap;
                                    const stackMoreLeft = (totalWidth / 2) + rightmostOffset + (cardWidth / 2) + 6;
                                    return <div className="stack-more" style={{ left: `${stackMoreLeft}px` }}>+{p.cards - visible}</div>;
                                })()}
                            </div>
                        </div>
                    );
                })}

                {/* bottom hand perspective intentionally omitted to avoid duplicating the player's own cards;
                    the player's hand is rendered by the `PlayerHand` component overlay. */}
            </div>
        </div>
    );
}
