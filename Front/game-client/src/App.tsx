import { useState } from 'react';
import GameTable from './components/GameTable';
import Lobby from './components/Lobby';
import DemoGame from './components/DemoGame';
import GameAudioControls from './components/GameAudioControls';
import './App.css';
import { PLAYER_NAME_KEY } from './config/player';
import './components/TableTheme.css';
document.title = 'Pepino';

export default function App() {
    const [session, setSession] = useState<{ room: string; name: string } | null>(null);
    const demo = import.meta.env.DEV && new URLSearchParams(location.search).has('demo');
    return <><GameAudioControls runtimeOnly applicationRuntime />
        {demo ? <DemoGame /> : session ? <GameTable roomId={session.room} playerName={session.name} onLeave={() => setSession(null)} />
        : <Lobby onJoin={(room, name) => { try {localStorage.setItem(PLAYER_NAME_KEY, name);} catch { /* Remembering the name is optional. */ } setSession({ room, name }); }} />}</>;
}
