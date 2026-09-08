import { useEffect, useRef, useState } from 'react';
import { GameAudio, loadAudioSettings } from '../game3d/gameAudio';
import type { GameState } from '../types/Card';
import './GameAudioControls.css';

export default function GameAudioControls({ state }: { state: GameState }) {
    const [settings, setSettings] = useState(loadAudioSettings);
    const engine = useRef<GameAudio | null>(null);
    const currentPlayer = state.players.find(p => p.isCurrentTurn)?.connectionId;
    const previous = useRef({ sequence: state.lastPlay?.sequence, turn: false, player: currentPlayer, paused: !!state.isPaused });
    useEffect(() => {
        const audio = new GameAudio(loadAudioSettings()); engine.current = audio;
        const unlock = () => { void audio.unlock(); };
        const visibility = () => { if (document.hidden) audio.pause(); else unlock(); };
        const action = (event: Event) => {
            const button = (event.target as Element).closest<HTMLButtonElement>('button');
            if (!button || button.disabled) return;
            if (button.classList.contains('hand-card')) audio.cue('select');
        };
        document.addEventListener('pointerdown', unlock);
        document.addEventListener('keydown', unlock);
        document.addEventListener('click', action);
        document.addEventListener('visibilitychange', visibility);
        return () => {
            document.removeEventListener('pointerdown', unlock);
            document.removeEventListener('keydown', unlock);
            document.removeEventListener('click', action);
            document.removeEventListener('visibilitychange', visibility);
            audio.dispose(); engine.current = null;
        };
    }, []);
    useEffect(() => { engine.current?.update(settings); }, [settings]);
    const myTurn = !state.isPaused && state.players.some(p => p.connectionId === state.yourPlayerId && p.isCurrentTurn);
    useEffect(() => {
        const last = state.lastPlay;
        if (last && last.sequence !== previous.current.sequence) {
            engine.current?.cue(last.isPepineado ? 'pepino' : last.isWildcard ? 'wildcard' : 'play');
        } else if (myTurn && !previous.current.turn) engine.current?.cue('turn');
        else if (!state.isPaused && !previous.current.paused && currentPlayer !== previous.current.player) engine.current?.cue('pass');
        previous.current = { sequence: last?.sequence, turn: myTurn, player: currentPlayer, paused: !!state.isPaused };
    }, [state.lastPlay, myTurn, currentPlayer, state.isPaused]);
    return <details className="audio-settings">
        <summary aria-label="Configurar música y efectos">AUDIO</summary>
        <div className="audio-panel">
            <strong>Ambiente sonoro</strong>
            {(['music', 'effects'] as const).map(key => <label key={key}>
                <span>{key === 'music' ? 'Música chill' : 'Efectos'} <output>{Math.round(settings[key] * 100)}%</output></span>
                <input aria-label={key === 'music' ? 'Volumen de música' : 'Volumen de efectos'} type="range" min="0" max="100" value={Math.round(settings[key] * 100)} onChange={e => setSettings(s => ({ ...s, [key]: Number(e.target.value) / 100 }))} />
            </label>)}
            <button onClick={() => setSettings({ music: 0, effects: 0 })}>SILENCIAR TODO</button>
        </div>
    </details>;
}
