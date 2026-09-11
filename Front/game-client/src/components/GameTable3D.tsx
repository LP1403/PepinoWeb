import { useEffect, useMemo, useRef, useState } from 'react';
import type { CSSProperties, PointerEvent } from 'react';
import type { Card, GameState } from '../types/Card';
import { CardService } from '../services/CardService';
import { cardImage } from '../game3d/cardArt';
import { seatPositions } from '../game3d/layout';
import SceneView from './SceneView';
import GameGraphicsControls from './GameGraphicsControls';
import GameAudioControls from './GameAudioControls';
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
export default function GameTable3D({ state, busy: parentBusy = false, connected = true, onPlay, onPass, onLeave }: GameTable3DProps) {
    const actionInFlight = useRef(false);
    const [sending, setSending] = useState(false);
    const busy = parentBusy || sending;
    const [selected, setSelected] = useState<string[]>([]);
    const [zoom,setZoom] = useState(1);
    const [showTurnOrder,setShowTurnOrder] = useState(false);
    const [portrait, setPortrait] = useState(innerWidth < 600);
    const [shortLandscape,setShortLandscape]=useState(innerWidth>=601 && innerHeight<=550);
    useEffect(() => { const resize = () => {setPortrait(innerWidth < 600);setShortLandscape(innerWidth>=601 && innerHeight<=550);}; addEventListener('resize', resize); return () => removeEventListener('resize', resize); }, []);
    const [drag, setDrag] = useState<{ x: number; y: number; cards: Card[] } | null>(null);
    const gesture = useRef<{ id: string; x: number; y: number; dragging: boolean; cards: Card[]; revision: number } | null>(null);
    const dropTarget = useRef<HTMLDivElement>(null);
    const suppressClick = useRef(false);
    useEffect(() => {
        const cancel = (event: KeyboardEvent) => {
            const target = event.target as HTMLElement | null;
            if (event.key !== 'Escape' || document.querySelector('dialog[open]') || target?.closest('input, textarea, select, [contenteditable="true"]')) return;
            gesture.current = null;
            pan.current = null;
            setDrag(null);
            setSelected([]);
        };
        addEventListener('keydown', cancel);
        return () => removeEventListener('keydown', cancel);
    }, []);


    const previousPlay = useRef(state.lastPlay?.sequence ?? 0);
    const [effect, setEffect] = useState('');
    const scroll = useRef<HTMLDivElement>(null);
    const [handEdges,setHandEdges]=useState({start:true,end:false});
    useEffect(()=>{
        const el=scroll.current;
        if(!el)return;
        const update=()=>{
            const start=el.scrollLeft<=1,end=el.scrollLeft+el.clientWidth>=el.scrollWidth-1;
            setHandEdges(previous=>previous.start===start && previous.end===end ? previous : {start,end});
        };
        const observer=new ResizeObserver(update);
        observer.observe(el);
        if(el.firstElementChild)observer.observe(el.firstElementChild);
        el.addEventListener('scroll',update,{passive:true});update();
        return()=>{observer.disconnect();el.removeEventListener('scroll',update);};
    },[state.yourHand.length]);
    function moveHand(direction:number) {
        const el=scroll.current;if(!el)return;
        el.scrollBy({left:direction*el.clientWidth*.7,behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'});
    }
    const pan = useRef<{x:number;y:number;left:number;active:boolean}|null>(null);
    const local = state.players.find(p => p.connectionId === state.yourPlayerId);
    const meIndex = state.players.findIndex(p => p.connectionId === state.yourPlayerId);
    const opponents = useMemo(() => [...state.players.slice(meIndex + 1), ...state.players.slice(0, meIndex)], [state.players, meIndex]);
    const seats = seatPositions(opponents.length, portrait, shortLandscape);
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
        if (event.isPepineado && event.skippedPlayerId) setEffect(event.skippedPlayerId === state.yourPlayerId ? `${event.playerName} te pepineó` : `${event.playerName} pepineó a ${event.skippedPlayerName ?? 'otro jugador'}`);
        else if (event.isWildcard) setEffect(`${event.playerName} jugó un comodín`);
        else setEffect('');

        const timer = setTimeout(() => setEffect(''), 2600); return () => clearTimeout(timer);
    }, [state.lastPlay?.sequence, state.yourPlayerId]);

    function toggle(id: string) {
        if (suppressClick.current) { suppressClick.current = false; return; }
        if (!myTurn || busy) return;
        setSelected(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
    }
    function move(e: PointerEvent<HTMLButtonElement>) {
        const g = gesture.current; if (!g || !myTurn || busy) return;
        const dx = e.clientX - g.x;
        const dy = g.y - e.clientY;
        if (!g.dragging && Math.abs(dx) > 10 && Math.abs(dx) > Math.abs(dy)) {
            suppressClick.current = true;
            gesture.current = null;
            return;
        }
        if (!g.dragging && g.y - e.clientY > 18) {
            g.dragging = true; e.currentTarget.setPointerCapture(e.pointerId); suppressClick.current = true;
        }
        if (g.dragging) {
            setDrag({ x: e.clientX, y: e.clientY, cards: g.cards });
        }
    }
    function release(e: PointerEvent<HTMLButtonElement>) {
        const g = gesture.current, zone = dropTarget.current?.getBoundingClientRect();
        if (g?.dragging && zone && myTurn && !busy && g.revision === state.revision && e.clientX >= zone.left && e.clientX <= zone.right && e.clientY >= zone.top && e.clientY <= zone.bottom) {
            const valid = CardService.validatePlay(g.cards, state.lastPlayedCards, !state.lastPlayedCards.length, !!state.isNewRound);
            if (valid.isValid) void playCards(g.cards);
            else setSelected(g.cards.map(card => card.id));
        }
        gesture.current = null; setDrag(null);
    }
    async function playCards(cards: Card[]) {
        if (!cards.length || !myTurn || busy || actionInFlight.current) return;
        actionInFlight.current = true;
        setSending(true);
        try { if (await onPlay(cards)) setSelected([]); }
        finally { actionInFlight.current = false; setSending(false); }
    }
    async function passTurn() {
        if (!canPass || actionInFlight.current) return;
        actionInFlight.current = true;
        setSending(true);
        try { if (await onPass()) setSelected([]); }
        finally { actionInFlight.current = false; setSending(false); }
    }
    const helper = !connected ? 'Recuperando la conexión con la mesa…' : busy ? 'Enviando tu jugada…' : state.isPaused ? 'Partida pausada · esperando reconexión (hasta 60 s)' : local?.hasWon ? '¡Ya estás entre los ganadores!' : !myTurn ? `${turn?.name ?? 'Otro jugador'} está pensando…` : chosen.length ? (validation.isValid ? `${chosen.length} carta${chosen.length > 1 ? 's' : ''} lista${chosen.length > 1 ? 's' : ''}` : validation.reason) : state.isNewRound ? 'Nueva ronda · Juega libremente' : 'Elegí tus cartas · los bordes dorados indican jugadas posibles';
    return <main className={`pepino-game${local?.hasWon ? ' spectating' : ''}${opponents.length>3 ? ' crowded-table' : ''}`} data-testid="game" data-turn={myTurn} data-revision={state.revision}>
        <GameAudioControls state={state} runtimeOnly />
        <SceneView opponents={opponents} yourTurn={myTurn} play={state.lastPlay} zoom={zoom} discardCards={state.tableCards.filter(c=>!state.lastPlay?.cards.some(last=>last.id===c.id))} />
        <nav className="table-zoom" aria-label="Zoom de mesa">
            {local?.hasWon && <span>ESPECTANDO</span>}
            <button aria-label="Alejar mesa" disabled={zoom<=1} onClick={()=>setZoom(z=>Math.max(1,z-.15))}>−</button>
            <button aria-label="Restablecer zoom" onClick={()=>setZoom(1)}>{Math.round(zoom*100)}%</button>
            <button aria-label="Acercar mesa" disabled={zoom>=1.6} onClick={()=>setZoom(z=>Math.min(1.6,z+.15))}>+</button>
        </nav>
        <header className="game-topbar"><div className="wordmark"><strong>PEPINO</strong><i className="logo-cucumber" aria-hidden="true" /></div>
            <div className="top-actions"><div className="room-tag" title={`Sala ${state.roomId} · Ronda ${state.roundNumber}`}>SALA <b>{state.roomId}</b><span>RONDA {state.roundNumber}</span></div><GameGraphicsControls state={state} zoom={opponents.length>3 ? zoom : undefined} onZoom={setZoom}/><button onClick={onLeave}>SALIR</button></div>
        </header>
        {opponents.map((p,i) => <div key={p.connectionId} className={`seat-badge seat-${i % 4} ${p.isCurrentTurn && !state.isPaused && connected ? 'active' : ''}`} style={{ left: `${seats[i].avatarX * 100}%`, top: `${seats[i].avatarY * 100}%` }} data-testid="opponent">
            <div className="avatar">{p.name.slice(0,2).toUpperCase()}</div>
            <span className="seat-name">{p.name}</span>
            <small className={!p.hasWon && p.cardCount>0 && p.cardCount<=2 ? 'few-cards' : ''}>{p.hasWon ? '★ Ganador' : `${p.cardCount} carta${p.cardCount===1?'':'s'}`}</small>
            <small className="seat-status">{!p.isConnected ? 'Reconectando…' : !connected ? 'Sin conexión' : state.isPaused ? 'En pausa' : p.isCurrentTurn ? 'JUGANDO' : p.isSkipped ? 'PEPINEADO' : ''}</small>
        </div>)}
        <div className="pile-caption" data-testid="last-play">{state.lastPlay ? <><b>{state.lastPlay.cards.length} × {state.lastPlay.cards[0].value}</b><span>{state.lastPlay.playerName}</span></> : <span>El 3♦ decide quién empieza</span>}</div>
        {state.tableCards.length>0 && <div className="discard-count">{state.tableCards.length} carta{state.tableCards.length===1?'':'s'} jugada{state.tableCards.length===1?'':'s'}</div>}
        {effect && <div className="play-effect" role="status"><strong>{state.lastPlay?.isPepineado ? '¡PEPINEADO!' : 'COMODÍN'}</strong><span>{effect}</span></div>}
        {showTurnOrder && <GameModal title="Orden de turnos" onClose={()=>setShowTurnOrder(false)}>
            <p>La ronda sigue este orden de asientos y vuelve al primero. Los ganadores dejan de jugar; el pepineado salta un turno y el comodín permite abrir otra vez.</p>
            <ol className="turn-order">{state.players.map(p=><li key={p.connectionId} aria-current={p.isCurrentTurn ? 'step' : undefined}>
                <b>{p.name}{p.connectionId===state.yourPlayerId?' (vos)':''}</b>
                <span>{p.hasWon?'Ganador':!p.isConnected?'Reconectando':state.isPaused?'En pausa':p.isCurrentTurn?'Turno actual':p.isSkipped?'Pepineado':'En mesa'}</span>
            </li>)}</ol>
            <div className="modal-actions"><button className="secondary-button" onClick={()=>setShowTurnOrder(false)}>VOLVER A LA MESA</button></div>
        </GameModal>}
        {drag && <div className="drop-target" ref={dropTarget}>{CardService.validatePlay(drag.cards, state.lastPlayedCards, !state.lastPlayedCards.length, !!state.isNewRound).isValid ? `Soltá para jugar ${drag.cards.length} carta${drag.cards.length > 1 ? 's' : ''}` : 'Esta combinación no se puede jugar'}</div>}
        <div className={`seat-badge local-seat seat-0 ${myTurn ? 'active' : ''}`}><div className="avatar">{local?.name.slice(0,2).toUpperCase()}<span className="seat-count">{state.yourHand.length}</span></div><span className="seat-name">{local?.name}</span><small>VOS</small></div>
        {local?.hasWon && <button className="turn-status spectator-turn" aria-label="Ver orden de turnos" onClick={()=>setShowTurnOrder(true)}>{!connected ? 'RECONECTANDO' : state.isPaused ? 'PARTIDA EN PAUSA' : `TURNO DE ${turn?.name.toUpperCase() ?? '…'}`}</button>}
        <section className="hand-area" aria-label="Tu mano">
            <div className="play-controls"><button className="primary-button" disabled={!canPlay} onClick={() => void playCards(chosen)}>JUGAR</button><button className="secondary-button" disabled={!canPass} onClick={() => { void passTurn(); }}>PASAR</button>{chosen.length > 0 && <button className="clear-selection" onClick={() => setSelected([])}>Limpiar ({chosen.length})</button>}</div>
            <div className="hand-helper turn-readout" aria-live="polite">
                <button className={`turn-status ${myTurn ? 'your-turn' : ''}`} aria-label="Ver orden de turnos" onClick={()=>setShowTurnOrder(true)}>
                    {!connected ? 'RECONECTANDO' : state.isPaused ? 'PARTIDA EN PAUSA' : busy ? 'ENVIANDO JUGADA' : local?.hasWon ? 'ESPECTANDO' : myTurn ? 'TU TURNO' : `TURNO DE ${turn?.name.toUpperCase() ?? '…'}`}
                </button>
                {(myTurn && !busy || !connected || state.isPaused) && <span className="turn-detail">{myTurn && !chosen.length ? (state.isNewRound ? 'Nueva ronda · Elegí tus cartas' : 'Elegí tus cartas para jugar') : helper}</span>}
            </div>
            <div className={`hand-scroll${handEdges.start ? '' : ' more-left'}${handEdges.end ? '' : ' more-right'}`} ref={scroll} onWheel={e => { if (scroll.current) scroll.current.scrollLeft += e.deltaY; }}
                onPointerDown={e=>{if(e.pointerType==='mouse' && e.button===0) pan.current={x:e.clientX,y:e.clientY,left:e.currentTarget.scrollLeft,active:false};}}
                onPointerMove={e=>{
                    const p=pan.current;
                    if(!p || !(e.buttons&1)) return;
                    const dx=e.clientX-p.x,dy=e.clientY-p.y;
                    if(!p.active && Math.abs(dy)>10 && Math.abs(dy)>Math.abs(dx)){pan.current=null;return;}
                    if(!p.active && Math.abs(dx)>10 && Math.abs(dx)>Math.abs(dy)){
                        p.active=true;gesture.current=null;suppressClick.current=true;
                        e.currentTarget.setPointerCapture(e.pointerId);
                    }
                    if(p.active){e.preventDefault();e.currentTarget.scrollLeft=p.left-dx;}
                }}
                onPointerUp={()=>{pan.current=null;}}
                onPointerCancel={()=>{pan.current=null;}}
                onLostPointerCapture={()=>{pan.current=null;}}>
                <div className="hand-fan">
                    {groups.map(([value, cards], gi) => <div className="value-stack" key={value} style={{ '--cards': cards.length, '--tilt': `${Math.max(-4, Math.min(4, (gi - (groups.length - 1) / 2) * 1.3))}deg` } as CSSProperties}>
                        {cards.map((card, i) => <button key={card.id} className={`hand-card ${chosen.some(c => c.id === card.id) ? 'selected' : ''} ${myTurn && playable.has(card.id) ? 'possible' : ''} ${drag?.cards.some(c => c.id === card.id) ? 'dragging-card' : ''}`}
                            style={{ '--index': i } as CSSProperties} aria-label={`${card.value} de ${card.suit}`} aria-pressed={chosen.some(c => c.id === card.id)} data-card-id={card.id} data-value={card.value} data-suit={card.suit}
                            onClick={e => { if(e.detail===0) suppressClick.current=false; toggle(card.id); }} disabled={!myTurn || busy}
                            onKeyDown={e=>{
                                if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;
                                const buttons=Array.from(scroll.current?.querySelectorAll<HTMLButtonElement>('.hand-card:not(:disabled)') ?? []);
                                const index=buttons.indexOf(e.currentTarget);
                                const next=e.key==='Home'?0:e.key==='End'?buttons.length-1:index+(e.key==='ArrowRight'?1:-1);
                                e.preventDefault();
                                const target=buttons[Math.max(0,Math.min(buttons.length-1,next))];
                                target?.focus({preventScroll:true});target?.scrollIntoView({block:'nearest',inline:'nearest'});
                                if(scroll.current && (e.key==='Home' || e.key==='End'))scroll.current.scrollLeft=e.key==='Home'?0:scroll.current.scrollWidth;
                            }}
                            onPointerDown={e => { if (!e.isPrimary || e.button !== 0) return; suppressClick.current = false; gesture.current = { id: card.id, x: e.clientX, y: e.clientY, dragging: false, cards: chosen.some(c => c.id === card.id) ? chosen : [card], revision: state.revision }; }}
                            onPointerMove={move} onPointerUp={release} onPointerCancel={() => { gesture.current = null; setDrag(null); }}>
                            <img src={cardImage(card, card.deckIndex)} alt="" draggable={false} />
                        </button>)}
                        <span className="stack-count">{cards.length > 1 ? `${cards.length} × ${value}` : value === 2 ? 'COMODÍN' : ''}</span>
                    </div>)}
                </div>
            </div>
            <nav className="hand-navigation" aria-label="Desplazar cartas"><button disabled={handEdges.start} onClick={() => moveHand(-1)} aria-label="Cartas anteriores"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19 12H5m6-6-6 6 6 6" /></svg></button><span>{state.yourHand.length} CARTAS</span><button disabled={handEdges.end} onClick={() => moveHand(1)} aria-label="Cartas siguientes"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14m-6-6 6 6-6 6" /></svg></button></nav>
        </section>
        {drag && <div className="drag-ghost drag-group" aria-label={`Arrastrando ${drag.cards.length} cartas`} style={{ left: drag.x, top: drag.y }}>
            {drag.cards.map((card,i) => <img key={card.id} src={cardImage(card, card.deckIndex)} alt={`${card.value} de ${card.suit}`} style={{ transform: `translateX(${(i-(drag.cards.length-1)/2)*Math.min(32,220/Math.max(1,drag.cards.length-1))}px) rotate(${(i-(drag.cards.length-1)/2)*Math.min(4,20/Math.max(1,drag.cards.length-1))}deg)`, zIndex:i }} />)}<b>{drag.cards.length} carta{drag.cards.length > 1 ? 's' : ''}</b>
        </div>}
    </main>;
}
