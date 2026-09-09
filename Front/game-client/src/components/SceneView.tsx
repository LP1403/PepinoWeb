import { useEffect, useRef, useState } from 'react';
import type { Card, PlayedCards, Player } from '../types/Card';
import { createPepinoScene } from '../game3d/pepinoScene';
import type { PepinoSceneApi } from '../game3d/pepinoScene';
const empty: Player[] = [];
const noCards: Card[] = [];
export default function SceneView({ opponents = empty, play = null, lobby = false, yourTurn = false, discardCards=noCards, zoom=1 }: { lobby?: boolean; opponents?: Player[]; play?: PlayedCards | null; yourTurn?: boolean; discardCards?:Card[]; zoom?:number }) {
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
    useEffect(() => { api.current?.setTurnIndicator(yourTurn); }, [yourTurn]);
    useEffect(() => {api.current?.setDiscardCards(discardCards);},[discardCards]);
    useEffect(() => {api.current?.setZoom(zoom);},[zoom]);
    useEffect(() => {
        const next = play?.sequence ?? 0;
        if (sequence.current === next) return;
        api.current?.setLastPlay(play, sequence.current !== null && next > sequence.current);
        sequence.current = next;
    }, [play]);
    return <div className="scene-view" ref={host} aria-hidden={!error}>{error && <p className="gpu-error">No pudimos iniciar el render 3D. Activá la aceleración gráfica del navegador y recargá.</p>}</div>;
}
