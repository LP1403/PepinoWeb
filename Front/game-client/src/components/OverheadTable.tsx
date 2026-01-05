import React from 'react';
import './OverheadTable.css';

interface PlayerInfo {
    id: string;
    name: string;
    cardCount: number;
    avatarColor?: string;
}

interface OverheadTableProps {
    players: PlayerInfo[];
    bottomPlayerId?: string;
    title?: string;
}

function TablePlayer({ player, angle, distance }: { player: PlayerInfo; angle: number; distance: number; }) {
    // position via transform rotate/translate to create circular distribution
    const style: React.CSSProperties = {
        transform: `rotate(${angle}deg) translate(${distance}px) rotate(${-angle}deg)`
    };

    return (
        <div className="table-player" style={style}>
            <div className="player-avatar" style={{ background: player.avatarColor || '#8fbf8f' }}>{player.name.charAt(0)}</div>
            <div className="player-name">{player.name}</div>
            <div className="player-cards">
                {Array.from({ length: Math.min(player.cardCount, 7) }).map((_, i) => (
                    <div key={i} className="card-back" />
                ))}
                {player.cardCount > 7 && <div className="cards-more">+{player.cardCount - 7}</div>}
            </div>
        </div>
    );
}

export default function OverheadTable({ players, bottomPlayerId, title = 'Pepino - Sala Demo' }: OverheadTableProps) {
    // compute angles around a circle keeping bottom player centered at bottom
    const count = players.length;
    // determine index of bottom player
    const bottomIdx = Math.max(0, players.findIndex(p => p.id === bottomPlayerId));

    // angles spread across 360 but we bias so bottom player at 270deg
    return (
        <div className="overhead-table-root">
            <div className="table-title">{title}</div>
            <div className="table-arena">
                <div className="table-surface">{/* central surface */}
                    <div className="table-center-label">PEPINO</div>
                </div>

                {players.map((p, i) => {
                    // compute relative position so bottomIdx maps to 270deg
                    const rel = (i - bottomIdx + count) % count;
                    const angle = 270 + (rel * 360) / count;
                    // distance depends on position (closer for bottom player)
                    const distance = i === bottomIdx ? 140 : 220;
                    return <TablePlayer key={p.id} player={p} angle={angle} distance={distance} />;
                })}

                {/* Bottom player's hand (large, perspective) */}
                {bottomIdx >= 0 && players[bottomIdx] && (
                    <div className="bottom-hand">
                        {Array.from({ length: Math.min(players[bottomIdx].cardCount, 10) }).map((_, i) => (
                            <div key={i} className="hand-card" style={{ left: `${i * 34}px`, transform: `rotate(${(i - 4) * 6}deg)` }} />
                        ))}
                        {players[bottomIdx].cardCount > 10 && <div className="hand-more">+{players[bottomIdx].cardCount - 10}</div>}
                    </div>
                )}
            </div>
        </div>
    );
}
