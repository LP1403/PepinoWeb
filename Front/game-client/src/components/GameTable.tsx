import { useState } from 'react';
import { useGameConnection } from '../hooks/useGameConnection';
import GameTable3D from './GameTable3D';
import SceneView from './SceneView';
import GameModal from './GameModal';
import './GameTable3D.css';
export default function GameTable({ roomId, playerName, onLeave }: { roomId: string; playerName: string; onLeave: () => void }) {
    const game = useGameConnection({ roomId, playerName });
    const [leaving, setLeaving] = useState(false);
    const state = game.state;
    const connected = game.status === 'Conectado';
    async function leave() { await game.leave(); onLeave(); }
    async function shareRoom() {
        const url = `${location.origin}/?room=${encodeURIComponent(roomId)}`;
        if (navigator.share) await navigator.share({ title: 'Mesa de Pepino', text: `${playerName} te invitó a jugar Pepino`, url });
        else { await navigator.clipboard.writeText(url); alert('Link de sala copiado.'); }
    }
    return <>
        {state?.isGameStarted ? <GameTable3D state={state} busy={game.busy} connected={connected} onPlay={game.play} onPass={game.pass} onLeave={() => setLeaving(true)} /> :
        <main className="pepino-game lobby-screen"><SceneView />
            <header className="game-topbar"><div className="wordmark">pepino<span>CLUB DE CARTAS</span></div><div className="top-actions"><button className="secondary-button" onClick={() => void shareRoom()}>COMPARTIR SALA</button><button className="secondary-button" onClick={() => void leave()}>SALIR</button></div></header>
            <section className="lobby-panel">
                <span className="eyebrow">{state?.isGameFinished ? 'PARTIDA TERMINADA' : 'ANTES DE REPARTIR'}</span>
                <h1>{state?.isGameFinished ? '¡Bien jugado!' : 'Tu mesa, tus amigos.'}</h1>
                <div className="lobby-room-code"><span>SALA</span><strong>{roomId}</strong><button onClick={() => { void navigator.clipboard?.writeText(roomId); }}>COPIAR CÓDIGO</button></div>
                {!state ? <p role="status">{game.status}</p> : <>
                    {state.notice && <p className="lobby-notice">{state.notice}</p>}
                    {state.isGameFinished && <ol className="winners-list">{state.winners.map(id => <li key={id}>{state.players.find(p => p.connectionId === id)?.name}</li>)}</ol>}
                    <div className="lobby-section-heading"><h2>Jugadores</h2><span>{state.players.length}/8</span></div>
                    <ol className="lobby-players">{state.players.map((p, i) => <li key={p.connectionId}><span className={`mini-avatar seat-${i % 4}`}>{p.name.slice(0,2).toUpperCase()}</span><div><b>{p.name}{p.connectionId === state.yourPlayerId ? ' (vos)' : ''}</b><small>{p.isConnected ? 'Listo para jugar' : 'Reconectando…'}</small></div><span className="seat-number">{String(i + 1).padStart(2,'0')}</span></li>)}</ol>
                    <div className="lobby-section-heading"><h2>Mazos</h2><span>48 cartas cada uno</span></div>
                    <div className="deck-picker">{[1,2,3].map(n => <button key={n} aria-pressed={state.gameMode?.deckCount === n} disabled={!state.isRoomCreator || game.busy || !connected} onClick={() => void game.selectMode(n)}><strong>{n}</strong><span>{n === 1 ? 'MAZO' : 'MAZOS'}</span></button>)}</div>
                    <p className="lobby-description">{state.gameMode ? `${state.gameMode.deckCount * 48} cartas · ${state.gameMode.cardsPerPlayer}–${Math.ceil(state.gameMode.deckCount * 48 / state.players.length)} por persona` : 'El creador elige cuántos mazos repartir.'}</p>
                    <button className="primary-button start-button" disabled={!state.isRoomCreator || !state.gameMode || state.players.length < 2 || state.players.some(p => !p.isConnected) || game.busy || !connected} onClick={() => void game.start()}>{state.isGameFinished ? 'VOLVER A JUGAR' : 'INICIAR PARTIDA'}</button>
                    <p className="lobby-description">{state.players.length < 2 ? 'Falta al menos un jugador. Compartí el código de sala.' : !state.isRoomCreator ? 'El creador de la sala inicia la partida.' : 'El primer jugador con un 3♦ empieza.'}</p>
                </>}
            </section>
            <div className="lobby-slogan"><span>UN COMODÍN.<br/>OTRA OPORTUNIDAD.</span><p>Jugá tus cartas. Cambiá la ronda.</p></div>
        </main>}
        {!connected && state && <div className="connection-banner" role="status">{game.status}</div>}
        {game.error && <div className="error-toast" role="alert"><span>{game.error}</span><button onClick={game.clearError} aria-label="Cerrar error">×</button></div>}
        {leaving && <GameModal title="¿Salir de la partida?" onClose={() => setLeaving(false)}><p>Salir cancela esta partida y devuelve a los demás al lobby.</p><div className="modal-actions"><button className="secondary-button" onClick={() => setLeaving(false)}>SEGUIR</button><button className="primary-button" onClick={() => void leave()}>SALIR</button></div></GameModal>}
    </>;
}
