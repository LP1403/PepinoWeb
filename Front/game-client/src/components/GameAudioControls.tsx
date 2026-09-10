import { useEffect, useRef, useState } from 'react';
import { disposeSharedGameAudio, getSharedGameAudio, loadAudioSettings } from '../game3d/gameAudio';
import type { GameAudio } from '../game3d/gameAudio';
import type { GameState } from '../types/Card';
import './GameAudioControls.css';

export default function GameAudioControls({ state, embedded = false, runtimeOnly = false, applicationRuntime = false }: { state?: GameState; embedded?: boolean; runtimeOnly?: boolean; applicationRuntime?: boolean }) {
    const [settings, setSettings] = useState(loadAudioSettings);
    const engine = useRef<GameAudio | null>(null);
    const previousLevels = useRef(settings.music || settings.effects ? settings : {music:.22,effects:.35});
    const hasWon = state?.players.find(p=>p.connectionId===state.yourPlayerId)?.hasWon ?? false;
    const panel = useRef<HTMLDetailsElement>(null);
    useEffect(() => {
        const outside = (event: PointerEvent) => {
            if (panel.current?.open && !panel.current.contains(event.target as Node)) panel.current.open=false;
        };
        const escape = (event: KeyboardEvent) => {
            if (event.key==='Escape' && panel.current?.open) {
                panel.current.open=false;
                panel.current.querySelector('summary')?.focus();
            }
        };
        document.addEventListener('pointerdown',outside);
        document.addEventListener('keydown',escape);
        return () => {
            document.removeEventListener('pointerdown',outside);
            document.removeEventListener('keydown',escape);
        };
    }, []);
    const currentPlayer = state?.players.find(p => p.isCurrentTurn)?.connectionId;
    const previous = useRef({ sequence: state?.lastPlay?.sequence, turn: false, player: currentPlayer, paused: !!state?.isPaused, won:hasWon });
    useEffect(() => {
        if (!applicationRuntime) {
            engine.current = getSharedGameAudio();
            return () => { engine.current = null; };
        }
        const audio = getSharedGameAudio(); engine.current = audio;
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
            disposeSharedGameAudio(); engine.current = null;
        };
    }, [applicationRuntime]);
    useEffect(() => { if (!runtimeOnly) getSharedGameAudio().update(settings); }, [settings, runtimeOnly]);
    const myTurn = !!state && !state.isPaused && state.players.some(p => p.connectionId === state.yourPlayerId && p.isCurrentTurn);
    useEffect(() => {
        if (!runtimeOnly || applicationRuntime) return;
        engine.current = getSharedGameAudio();
        const last = state?.lastPlay;
        if (hasWon && !previous.current.won) engine.current?.cue('win');
        else if (last && last.sequence !== previous.current.sequence) {
            engine.current?.cue(last.isPepineado && last.skippedPlayerId ? 'pepino' : last.isWildcard ? 'wildcard' : 'play');
        } else if (myTurn && !previous.current.turn) engine.current?.cue('turn');
        else if (!state?.isPaused && !previous.current.paused && currentPlayer !== previous.current.player) engine.current?.cue('pass');
        previous.current = { sequence: last?.sequence, turn: myTurn, player: currentPlayer, paused: !!state?.isPaused, won:hasWon };
    }, [runtimeOnly, state?.lastPlay, myTurn, currentPlayer, state?.isPaused, hasWon]);
    if (runtimeOnly) return null;
    const audioContent = <div className="audio-panel">
            {(['music', 'effects'] as const).map(key => <label key={key}>
                <span>{key === 'music' ? 'Música' : 'Efectos'} <output>{Math.round(settings[key] * 100)}%</output></span>
                <input aria-label={key === 'music' ? 'Volumen de música' : 'Volumen de efectos'} type="range" min="0" max="100" value={Math.round(settings[key] * 100)} onChange={e => setSettings(s => ({ ...s, [key]: Number(e.target.value) / 100 }))} />
            </label>)}
            <button onClick={() => {
                if(settings.music || settings.effects) {previousLevels.current=settings;setSettings({music:0,effects:0});}
                else setSettings(previousLevels.current);
            }}>{settings.music || settings.effects ? 'SILENCIAR TODO' : 'REACTIVAR AUDIO'}</button>
        </div>;
    if(embedded) return <section className="audio-settings-embedded" aria-labelledby="audio-section-title">
        <h3 id="audio-section-title">Audio</h3>{audioContent}
    </section>;
    return <details className="audio-settings" ref={panel}>
        <summary aria-label="Configurar música y efectos">AUDIO</summary>{audioContent}
    </details>;
}
