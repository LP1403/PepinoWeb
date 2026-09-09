import { useState } from 'react';
import SceneView from './SceneView';
import GameModal from './GameModal';
import './GameTable3D.css';
import { PLAYER_NAME_KEY } from '../config/player';
import { APP_VERSION } from '../config/version';
import LobbyControls from './LobbyControls';
export default function Lobby({ onJoin }: { onJoin: (room: string, name: string) => void }) {
    const params = new URLSearchParams(location.search);
    const linkedRoom = params.get('room') ?? params.get('sala') ?? '';
    const [name, setName] = useState(() => { try {return localStorage.getItem(PLAYER_NAME_KEY) ?? '';} catch {return '';} });
    const [room, setRoom] = useState(linkedRoom.toUpperCase().replace(/[^A-Z0-9_-]/g,''));
    const [rulesOpen, setRulesOpen] = useState(false);
    return <main className="pepino-game lobby-screen entry-screen"><SceneView lobby />
        <header className="game-topbar"><div className="wordmark"><strong>PEPINO</strong><i className="logo-cucumber" aria-hidden="true" /></div><LobbyControls /></header>
        <section className="lobby-panel entrance-panel"><span className="eyebrow">ENTRAR A LA MESA</span><h1>Sentate a jugar</h1><p>Elegí tu nombre y la sala. Tus amigos te esperan.</p>
            <form onSubmit={e => { e.preventDefault(); if (name.trim() && room.trim()) onJoin(room.trim().toUpperCase(), name.trim()); }}>
                <label htmlFor="player-name">TU NOMBRE</label><input id="player-name" autoComplete="nickname" placeholder="¿Cómo te llamás?" value={name} maxLength={24} required onChange={e => setName(e.target.value)} />
                <label htmlFor="room-code">CÓDIGO DE SALA</label><input id="room-code" autoComplete="off" autoCapitalize="characters" spellCheck={false} enterKeyHint="go" placeholder="Ej. LOSPIBES" value={room} maxLength={32} required onChange={e => setRoom(e.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g,''))} />
                <button className="primary-button start-button" disabled={!name.trim() || !room.trim()}>JUGAR <span>→</span></button>
            </form>
            <button type="button" className="rules-link" onClick={() => setRulesOpen(true)}>Cómo se juega Pepino <span>→</span></button>
        </section>
        <div className="lobby-slogan"><span>Te vas a ir<br/>pepineado.</span></div>
        <footer className="brand-footer"><span>© 2026 RayenCo</span><span className="version-footer">v{APP_VERSION}</span></footer>
        {rulesOpen && <GameModal title="Cómo se juega Pepino" onClose={() => setRulesOpen(false)}>
            <p>2–8 jugadores · 1–3 mazos de 48 cartas. El primer asiento con 3♦ empieza. Jugá grupos del mismo número; igualá la cantidad y superá o igualá el valor. Orden: 3 → 12 → 1.</p>
            <p>El 2 es comodín: jugá uno o varios y abrí otra vez libremente. Misma cantidad y valor salta al siguiente: ¡pepineado! Cuando todos pasan, volvés a abrir. No podés pasar al abrir una ronda.</p>
            <p>Ganan los primeros 2 en quedarse sin cartas (3 ganadores con más de 4 jugadores). No hay robo de cartas.</p>
            <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => setRulesOpen(false)}>Cerrar</button></div>
        </GameModal>}
    </main>;
}
