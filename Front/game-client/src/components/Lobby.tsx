import { useState } from 'react';
import SceneView from './SceneView';
import './GameTable3D.css';
import { PLAYER_NAME_KEY } from '../config/player';
export default function Lobby({ onJoin }: { onJoin: (room: string, name: string) => void }) {
    const params = new URLSearchParams(location.search);
    const linkedRoom = params.get('room') ?? params.get('sala') ?? '';
    const [name, setName] = useState(() => localStorage.getItem(PLAYER_NAME_KEY) ?? '');
    const [room, setRoom] = useState(linkedRoom.toUpperCase().replace(/[^A-Z0-9_-]/g,''));
    return <main className="pepino-game lobby-screen"><SceneView lobby />
        <header className="game-topbar"><div className="wordmark">pepino<span>CLUB DE CARTAS</span></div><span className="alpha-tag">ALPHA · WEB 3D</span></header>
        <section className="lobby-panel entrance-panel"><span className="eyebrow">HECHO PARA JUGAR ENTRE AMIGOS</span><h1>Una mesa.<br/>Muchas revanchas.</h1><p>El juego de cartas donde la misma jugada<br/>puede dejar a otro sin turno.</p>
            <form onSubmit={e => { e.preventDefault(); if (name.trim() && room.trim()) onJoin(room.trim().toUpperCase(), name.trim()); }}>
                <label htmlFor="player-name">TU NOMBRE</label><input id="player-name" autoComplete="nickname" placeholder="¿Cómo te llamás?" value={name} maxLength={24} required onChange={e => setName(e.target.value)} />
                <label htmlFor="room-code">CÓDIGO DE SALA</label><input id="room-code" placeholder="Ej. LOSPIBES" value={room} maxLength={32} required onChange={e => setRoom(e.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g,''))} />
                <small>Usen el mismo código para encontrarse. Si no existe, creamos la sala.</small>
                <button className="primary-button start-button" disabled={!name.trim() || !room.trim()}>ENTRAR A LA MESA <span>→</span></button>
            </form>
            <details className="rules"><summary>Cómo se juega Pepino</summary><p>2–8 jugadores · 1–3 mazos de 48 cartas. El primer asiento con 3♦ empieza. Jugá grupos del mismo número; igualá la cantidad y superá o igualá el valor. Orden: 3 → 12 → 1.</p><p>El 2 es comodín: jugá uno o varios y abrí otra vez libremente. Misma cantidad y valor salta al siguiente: ¡pepineado! Cuando todos pasan, volvés a abrir. No podés pasar al abrir una ronda.</p><p>Ganan los primeros 2 en quedarse sin cartas (3 ganadores con más de 4 jugadores). No hay robo de cartas.</p></details>
        </section>
        <div className="lobby-slogan"><span>LA PRÓXIMA<br/>ES TUYA.</span><p>Naipes españoles. Reglas de la casa.</p></div>
    </main>;
}
