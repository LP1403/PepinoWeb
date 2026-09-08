import { useEffect, useMemo, useRef, useState } from 'react';
import type { CSSProperties, PointerEvent } from 'react';
import type { Card, GameState } from '../types/Card';
import { CardService } from '../services/CardService';
import { cardImage } from '../game3d/cardArt';
import { seatPositions } from '../game3d/layout';
import SceneView from './SceneView';
import GameModal from './GameModal';
import './GameTable3D.css';

export interface GameTable3DProps {
    state: GameState;
    busy?: boolean;
    connected?: boolean;
    onPlay: (cards: Card[]) => Promise<boolean>;
    onPass: () => Promise<boolean>;
    onLeave: () => void;
}
export default function GameTable3D({ state, busy = false, connected = true, onPlay, onPass, onLeave }: GameTable3DProps) {
    const [selected, setSelected] = useState<string[]>([]);
    const [portrait, setPortrait] = useState(innerWidth < 600);
    useEffect(() => { const resize = () => setPortrait(innerWidth < 600); addEventListener('resize', resize); return () => removeEventListener('resize', resize); }, []);
    const [confirmation, setConfirmation] = useState<{ cards: Card[]; revision: number } | null>(null);
    const [drag, setDrag] = useState<{ x: number; y: number; cards: Card[] } | null>(null);
    const gesture = useRef<{ id: string; x: number; y: number; dragging: boolean } | null>(null);
    const suppressClick = useRef(false);
    const [sound, setSound] = useState(false);
    const audio = useRef<AudioContext | null>(null);
    const previousPlay = useRef(state.lastPlay?.sequence ?? 0);
    const [effect, setEffect] = useState('');
    const scroll = useRef<HTMLDivElement>(null);
    const local = state.players.find(p => p.connectionId === state.yourPlayerId);
    const meIndex = state.players.findIndex(p => p.connectionId === state.yourPlayerId);
    const opponents = useMemo(() => [...state.players.slice(meIndex + 1), ...state.players.slice(0, meIndex)], [state.players, meIndex]);
    const seats = seatPositions(opponents.length, portrait);
    const turn = state.players.find(p => p.isCurrentTurn);
    const myTurn = !!local?.isCurrentTurn && state.isGameStarted && !state.isPaused && connected;
    const chosen = state.yourHand.filter(c => selected.includes(c.id));
    const validation = CardService.validatePlay(chosen, state.lastPlayedCards, !state.lastPlayedCards.length, !!state.isNewRound);
    const canPlay = myTurn && validation.isValid && !busy;
    const canPass = myTurn && state.lastPlayedCards.length > 0 && !state.isNewRound && !busy;
    const groups = useMemo(() => {
        const map = new Map<number, Card[]>();
        [...state.yourHand].sort((a,b) => CardService.getCardValue(a) - CardService.getCardValue(b) || a.suit.localeCompare(b.suit))
            .forEach(c => { if (!map.has(c.value)) map.set(c.value, []); map.get(c.value)!.push(c); });
        return [...map.entries()];
    }, [state.yourHand]);
    const playable = new Set(groups.flatMap(([, cards]) => {
        const required = cards[0].value === 2 || !state.lastPlayedCards.length || state.isNewRound ? 1 : state.lastPlayedCards.length;
        return cards.length >= required && CardService.validatePlay(cards.slice(0, required), state.lastPlayedCards, !state.lastPlayedCards.length, !!state.isNewRound).isValid ? cards.map(c => c.id) : [];
    }));
    useEffect(() => {
        const event = state.lastPlay;
        if (!event || event.sequence === previousPlay.current) return;
        previousPlay.current = event.sequence;
        if (event.isPepineado) setEffect(event.skippedPlayerId === state.yourPlayerId ? `${event.playerName} te pepineó` : `${event.playerName} pepineó a ${event.skippedPlayerName}`);
        else if (event.isWildcard) setEffect(`${event.playerName} jugó un comodín`);
        else setEffect('');
        if (sound && audio.current) {
            const context = audio.current, oscillator = context.createOscillator(), gain = context.createGain();
            oscillator.type = 'sine'; oscillator.frequency.setValueAtTime(580, context.currentTime);
            oscillator.frequency.exponentialRampToValueAtTime(240, context.currentTime + .13);
            gain.gain.setValueAtTime(.06, context.currentTime); gain.gain.exponentialRampToValueAtTime(.001, context.currentTime + .16);
            oscillator.connect(gain); gain.connect(context.destination); oscillator.start(); oscillator.stop(context.currentTime + .17);
        }
        const timer = setTimeout(() => setEffect(''), 2600); return () => clearTimeout(timer);
    }, [state.lastPlay, state.yourPlayerId, sound]);
    useEffect(() => () => { void audio.current?.close(); }, []);
    function toggle(id: string) {
        if (suppressClick.current) { suppressClick.current = false; return; }
        if (!myTurn || busy) return;
        setSelected(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
    }
    function move(e: PointerEvent<HTMLButtonElement>) {
        const g = gesture.current; if (!g || !myTurn || busy) return;
        if (!g.dragging && g.y - e.clientY > 18) {
            g.dragging = true; e.currentTarget.setPointerCapture(e.pointerId); suppressClick.current = true;
        }
        if (g.dragging) {
            const cards = chosen.some(c => c.id === g.id) ? chosen : state.yourHand.filter(c => c.id === g.id);
            setDrag({ x: e.clientX, y: e.clientY, cards });
        }
    }
    function release(e: PointerEvent<HTMLButtonElement>) {
        if (gesture.current?.dragging && drag && e.clientY < innerHeight * .65 && e.clientX > innerWidth * .15 && e.clientX < innerWidth * .85) {
            const valid = CardService.validatePlay(drag.cards, state.lastPlayedCards, !state.lastPlayedCards.length, !!state.isNewRound);
            setSelected(drag.cards.map(c => c.id));
            if (valid.isValid) setConfirmation({ cards: drag.cards, revision: state.revision });
        }
        gesture.current = null; setDrag(null);
    }
    async function submit() {
        if (!confirmation || confirmation.revision !== state.revision || !myTurn) return;
        if (await onPlay(confirmation.cards)) { setSelected([]); setConfirmation(null); }
    }
    const helper = state.isPaused ? 'Partida pausada · esperando reconexión (hasta 60 s)' : local?.hasWon ? '¡Ya estás entre los ganadores!' : !myTurn ? `${turn?.name ?? 'Otro jugador'} está pensando…` : chosen.length ? (validation.isValid ? `${chosen.length} carta${chosen.length > 1 ? 's' : ''} lista${chosen.length > 1 ? 's' : ''}` : validation.reason) : state.isNewRound ? 'Nueva ronda · Juega libremente' : 'Elegí tus cartas · los bordes dorados indican jugadas posibles';
    return <main className="pepino-game" data-testid="game" data-turn={myTurn} data-revision={state.revision}>
        <SceneView opponents={opponents} play={state.lastPlay} />
        <header className="game-topbar"><div className="wordmark">pepino<span>CLUB DE CARTAS</span></div>
            <div className="room-tag">SALA <b>{state.roomId}</b><span>RONDA {state.roundNumber}</span></div>
            <div className="top-actions"><button aria-label={sound ? 'Silenciar sonidos' : 'Activar sonidos'} onClick={() => { if (!audio.current) audio.current = new AudioContext(); void audio.current.resume(); setSound(!sound); }}>{sound ? 'SONIDO ON' : 'SONIDO OFF'}</button><button onClick={onLeave}>SALIR</button></div>
        </header>
        {opponents.map((p,i) => <div key={p.connectionId} className={`seat-badge seat-${i % 4} ${p.isCurrentTurn ? 'active' : ''}`} style={{ left: `${seats[i].avatarX * 100}%`, top: `${seats[i].avatarY * 100}%` }} data-testid="opponent">
            <div className="avatar">{p.name.slice(0,2).toUpperCase()}<span className="seat-count">{p.hasWon ? '★' : p.cardCount}</span></div>
            <span className="seat-name">{p.name}</span><small>{!p.isConnected ? 'Reconectando…' : p.hasWon ? 'Ganador' : p.isCurrentTurn ? 'JUGANDO' : `${p.cardCount} cartas`}</small>
        </div>)}
        <div className="pile-caption" data-testid="last-play">{state.lastPlay ? <><b>{state.lastPlay.cards.length} × {state.lastPlay.cards[0].value}</b><span>{state.lastPlay.playerName}</span></> : <span>El 3♦ decide quién empieza</span>}</div>
        {effect && <div className="play-effect" role="status"><strong>{state.lastPlay?.isPepineado ? '¡PEPINEADO!' : 'COMODÍN'}</strong><span>{effect}</span></div>}
        <div className={`turn-pill ${myTurn ? 'your-turn' : ''}`} role="status">{myTurn ? 'TU TURNO' : state.isPaused ? 'EN PAUSA' : `TURNO DE ${turn?.name.toUpperCase() ?? '…'}`}</div>
        {drag && <div className="drop-target">Soltá aquí para confirmar tu jugada</div>}
        <div className={`seat-badge local-seat seat-0 ${myTurn ? 'active' : ''}`}><div className="avatar">{local?.name.slice(0,2).toUpperCase()}<span className="seat-count">{state.yourHand.length}</span></div><span className="seat-name">{local?.name}</span><small>VOS</small></div>
        <section className="hand-area" aria-label="Tu mano">
            <p className={`hand-helper${!myTurn ? ' waiting' : ''}`} aria-live="polite">{helper}</p>
            <div className="hand-scroll" ref={scroll} onWheel={e => { if (scroll.current) scroll.current.scrollLeft += e.deltaY; }}>
                <div className="hand-fan">
                    {groups.map(([value, cards], gi) => <div className="value-stack" key={value} style={{ '--cards': cards.length, '--tilt': `${Math.max(-4, Math.min(4, (gi - (groups.length - 1) / 2) * 1.3))}deg` } as CSSProperties}>
                        {cards.map((card, i) => <button key={card.id} className={`hand-card ${chosen.some(c => c.id === card.id) ? 'selected' : ''} ${myTurn && playable.has(card.id) ? 'possible' : ''}`}
                            style={{ '--index': i } as CSSProperties} aria-label={`${card.value} de ${card.suit}`} aria-pressed={chosen.some(c => c.id === card.id)} data-card-id={card.id} data-value={card.value}
                            onClick={() => toggle(card.id)} disabled={!myTurn || busy}
                            onPointerDown={e => { e.currentTarget.setPointerCapture(e.pointerId); suppressClick.current = false; gesture.current = { id: card.id, x: e.clientX, y: e.clientY, dragging: false }; }}
                            onPointerMove={move} onPointerUp={release} onPointerCancel={() => { gesture.current = null; setDrag(null); }}>
                            <img src={cardImage(card)} alt="" draggable={false} />
                        </button>)}
                        <span className="stack-count">{cards.length > 1 ? `${cards.length} × ${value}` : value === 2 ? 'COMODÍN' : ''}</span>
                    </div>)}
                </div>
            </div>
            <nav className="hand-navigation" aria-label="Desplazar cartas"><button onClick={() => scroll.current?.scrollBy({ left: -250, behavior: 'smooth' })} aria-label="Cartas anteriores">‹</button><span>{state.yourHand.length} CARTAS · AGRUPADAS POR VALOR</span><button onClick={() => scroll.current?.scrollBy({ left: 250, behavior: 'smooth' })} aria-label="Cartas siguientes">›</button></nav>
        </section>
        <div className="play-controls"><button className="primary-button" disabled={!canPlay} onClick={() => setConfirmation({ cards: chosen, revision: state.revision })}>JUGAR</button><button className="secondary-button" disabled={!canPass} onClick={() => { void onPass(); }}>PASAR</button>{chosen.length > 0 && <button className="clear-selection" onClick={() => setSelected([])}>Limpiar ({chosen.length})</button>}</div>
        {drag && <div className="drag-ghost" style={{ left: drag.x, top: drag.y }}><img src={cardImage(drag.cards[0])} alt=""/><b>{drag.cards.length}</b></div>}
        {confirmation && <GameModal title="¿Jugar estas cartas?" onClose={() => !busy && setConfirmation(null)}>
            <p>{confirmation.cards.length} carta(s) de valor <b>{confirmation.cards[0].value}</b>{confirmation.cards[0].value === 2 ? ' · Después volvés a jugar libremente.' : ''}</p>
            <div className="confirm-cards">{confirmation.cards.map(c => <img key={c.id} src={cardImage(c)} alt={`${c.value}${c.suit}`} />)}</div>
            {confirmation.revision !== state.revision && <p role="alert">La mesa cambió. Cancelá y revisá tu selección.</p>}
            <div className="modal-actions"><button className="secondary-button" disabled={busy} onClick={() => setConfirmation(null)}>CANCELAR</button><button className="primary-button" disabled={busy || confirmation.revision !== state.revision || !myTurn} onClick={() => void submit()}>CONFIRMAR</button></div>
        </GameModal>}
    </main>;
}
