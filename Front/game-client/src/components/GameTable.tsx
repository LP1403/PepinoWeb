import { useEffect, useState } from 'react';
import { useGameConnection } from '../hooks/useGameConnection';
import GameTable3D from './GameTable3D';
import SceneView from './SceneView';
import GameModal from './GameModal';
import { cardImage } from '../game3d/cardArt';
import './GameTable3D.css';
export default function GameTable({ roomId, playerName, onLeave }: { roomId: string; playerName: string; onLeave: () => void }) {
    const game = useGameConnection({ roomId, playerName });
    const [leaving, setLeaving] = useState(false);
    const [acknowledgedFinal, setAcknowledgedFinal] = useState<number | null>(null);
    const [visibleNotice, setVisibleNotice] = useState<string | null>(null);
    const state = game.state;
    const showFinal = !!state?.isGameFinished && !!state.lastPlay && acknowledgedFinal !== state.lastPlay.sequence;
    const connected = game.status === 'Conectado';
    useEffect(() => {
        if (!state?.notice) { setVisibleNotice(null); return; }
        setVisibleNotice(state.notice);
    }, [state?.notice]);
    useEffect(() => {
        if (!visibleNotice) return;
        const timer = window.setTimeout(() => setVisibleNotice(null), 3600);
        return () => window.clearTimeout(timer);
    }, [visibleNotice]);
    async function leave() { await game.leave(); onLeave(); }
    async function shareRoom() {
        const url = `${location.origin}/?room=${encodeURIComponent(roomId)}`;
        try {
            if (navigator.share) await navigator.share({ title: 'Mesa de Pepino', text: `${playerName} te invitó a jugar Pepino`, url });
            else { await navigator.clipboard.writeText(url); setVisibleNotice('Link de sala copiado.'); }
        } catch (error) {
            if (error instanceof DOMException && error.name==='AbortError') return;
            setVisibleNotice(`No se pudo compartir el link. Pasá el código de sala: ${roomId}`);
        }
    }
    return <>
        {state && (state.isGameStarted || showFinal) ? <GameTable3D state={state} busy={game.busy} connected={connected} onPlay={game.play} onPass={game.pass} onLeave={() => setLeaving(true)} /> :
        <main className="pepino-game lobby-screen"><SceneView lobby />
            <header className="game-topbar"><div className="wordmark"><strong>PEPINO</strong><i className="logo-cucumber" aria-hidden="true" /></div></header>
            <section className="lobby-panel">
                <span className="eyebrow">{state?.isGameFinished ? 'PARTIDA TERMINADA' : 'ANTES DE REPARTIR'}</span>
                <h1>{state?.isGameFinished ? '¡Bien jugado!' : 'Tu mesa, tus amigos.'}</h1>
                <div className="lobby-room-code"><span>SALA</span><strong>{roomId}</strong><button onClick={() => void shareRoom()}>COMPARTIR SALA</button></div>
                {!state ? <p role="status">{game.status}</p> : <>
                    {visibleNotice && <p className="lobby-notice lobby-notice-temporary" role="status">{visibleNotice}</p>}
                    {state.isGameFinished && <ol className="winners-list">{state.winners.map(id => <li key={id}>{state.players.find(p => p.connectionId === id)?.name}</li>)}</ol>}
                    <div className="lobby-section-heading"><h2>Jugadores</h2><span>{state.players.length}/8</span></div>
                    <ol className="lobby-players">{state.players.map((p, i) => <li key={p.connectionId}><span className={`mini-avatar seat-${i % 4}`}>{p.name.slice(0,2).toUpperCase()}</span><div><b>{p.name}{p.connectionId === state.yourPlayerId ? ' (vos)' : ''}</b><small>{p.isConnected ? 'Listo para jugar' : 'Reconectando…'}</small></div><span className="seat-number">{String(i + 1).padStart(2,'0')}</span></li>)}</ol>
                    <div className="lobby-section-heading"><h2>Mazos</h2><span>Elegí cómo jugar</span></div>
                    <div className="deck-picker">{[1,2,3].map(n => <button key={n} aria-pressed={state.gameMode?.deckCount === n} disabled={!state.isRoomCreator || game.busy || !connected} onClick={() => void game.selectMode(n)}><strong>{n}</strong><span>{n === 1 ? 'MAZO' : 'MAZOS'}</span></button>)}</div>
                    <p className="lobby-description">Más mazos = manos más grandes.</p>
                    <button className="primary-button start-button" disabled={!state.isRoomCreator || !state.gameMode || state.players.length < 2 || state.players.some(p => !p.isConnected) || game.busy || !connected} onClick={() => void game.start()}>{state.isGameFinished ? 'VOLVER A JUGAR' : 'INICIAR PARTIDA'}</button>
                    <p className="lobby-description">{state.players.length < 2 ? 'Falta al menos un jugador. Compartí el código de sala.' : !state.isRoomCreator ? 'El creador de la sala inicia la partida.' : 'El primer jugador con un 3♦ empieza.'}</p>
                </>}
                    <button className="lobby-leave" onClick={() => void leave()}>SALIR DE LA SALA</button>
                </section>
            <div className="lobby-seating" aria-label="Asientos de la mesa">{state?.players.map((p,i) => {
                const angle = Math.PI * 2 * i / state.players.length - Math.PI / 2;
                return <div className="lobby-place" key={p.connectionId} style={{left:`${50+Math.cos(angle)*38}%`,top:`${50+Math.sin(angle)*37}%`}}><span className="mini-avatar">{p.name.slice(0,2).toUpperCase()}</span><span>{p.name}{p.connectionId===state.yourPlayerId?' (vos)':''}</span></div>;
            })}</div>
        </main>}
        {!connected && state && <div className="connection-banner" role="status">{game.status}</div>}
        {showFinal && state?.lastPlay && <GameModal title="¡Partida terminada!" onClose={()=>setAcknowledgedFinal(state.lastPlay!.sequence)}>
            <ol className="winners-list">{state.winners.map(id=><li key={id}>{state.players.find(p=>p.connectionId===id)?.name}</li>)}</ol>
            <p>Última jugada de {state.lastPlay.playerName}</p>
            <div className="confirm-cards">{state.lastPlay.cards.map(c=><img key={c.id} src={cardImage(c)} alt={`${c.value} de ${c.suit}`} />)}</div>
            <div className="modal-actions"><button className="primary-button" onClick={()=>setAcknowledgedFinal(state.lastPlay!.sequence)}>VOLVER AL LOBBY</button></div>
        </GameModal>}
        {game.error && <div className="error-toast" role="alert"><span>{game.error}</span><button onClick={game.clearError} aria-label="Cerrar error">×</button></div>}
        {leaving && <GameModal title="¿Salir de la partida?" onClose={() => setLeaving(false)}><p>Salir cancela esta partida y devuelve a los demás al lobby.</p><div className="modal-actions"><button className="secondary-button" onClick={() => setLeaving(false)}>SEGUIR</button><button className="primary-button" onClick={() => void leave()}>SALIR</button></div></GameModal>}
    </>;
}
