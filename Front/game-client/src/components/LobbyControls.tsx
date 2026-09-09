import { useEffect, useState } from 'react';
import GameAudioControls from './GameAudioControls';

export default function LobbyControls() {
    const [fullscreen, setFullscreen] = useState(!!document.fullscreenElement);
    useEffect(() => {
        const update = () => setFullscreen(!!document.fullscreenElement);
        document.addEventListener('fullscreenchange', update);
        return () => document.removeEventListener('fullscreenchange', update);
    }, []);
    async function toggleFullscreen() {
        try {
            if (document.fullscreenElement) await document.exitFullscreen();
            else await document.querySelector<HTMLElement>('.lobby-screen')?.requestFullscreen();
        } catch { /* El navegador puede bloquear fullscreen hasta una interacción. */ }
    }
    return <div className="top-actions lobby-top-actions">
        <GameAudioControls />
        <GameAudioControls runtimeOnly />
        {document.fullscreenEnabled && <button className="graphics-button fullscreen-button" aria-label={fullscreen ? 'Salir de pantalla completa' : 'Pantalla completa'} title={fullscreen ? 'Salir de pantalla completa' : 'Pantalla completa'} onClick={() => void toggleFullscreen()}>⛶</button>}
    </div>;
}
