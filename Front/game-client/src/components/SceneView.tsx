import { useEffect, useRef, useState } from 'react';
import type { PlayedCards, Player } from '../types/Card';
import { createPepinoScene } from '../game3d/pepinoScene';
import type { PepinoSceneApi } from '../game3d/pepinoScene';
const empty: Player[] = [];
export default function SceneView({ opponents = empty, play = null, lobby = false }: { lobby?: boolean; opponents?: Player[]; play?: PlayedCards | null }) {
    const host = useRef<HTMLDivElement>(null);
    const api = useRef<PepinoSceneApi | null>(null);
    const sequence = useRef<number | null>(null);
    const [error, setError] = useState(false);
    useEffect(() => {
        try { api.current = createPepinoScene(host.current!, lobby); }
        catch (e) { console.error('No se pudo crear la escena', e); setError(true); }
        return () => { api.current?.dispose(); api.current = null; sequence.current = null; };
    }, [lobby]);
    useEffect(() => { api.current?.setOpponents(opponents); }, [opponents]);
    useEffect(() => {
        const next = play?.sequence ?? 0;
        if (sequence.current === next) return;
        api.current?.setLastPlay(play, sequence.current !== null && next > sequence.current);
        sequence.current = next;
    }, [play]);
    return <div className="scene-view" ref={host} aria-hidden={!error}>{error && <p className="gpu-error">No pudimos iniciar el render 3D. Activá la aceleración gráfica del navegador y recargá.</p>}</div>;
}
