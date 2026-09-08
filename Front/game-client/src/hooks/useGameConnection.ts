import { useCallback, useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';
import type { Card, GameState } from '../types/Card';

export function useGameConnection({ roomId, playerName }: { roomId: string; playerName: string }) {
    const [state, setState] = useState<GameState | null>(null);
    const [status, setStatus] = useState('Conectando…');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    const connection = useRef<HubConnection | null>(null);
    const pending = useRef(false);
    useEffect(() => {
        let stopped = false;
        let retry: ReturnType<typeof setTimeout> | undefined;
        const key = `pepino-session:${roomId}`;
        const token = sessionStorage.getItem(key) ?? crypto.randomUUID();
        sessionStorage.setItem(key, token);
        const url = import.meta.env.VITE_GAME_HUB_URL || `${location.origin}/gamehub`;
        const conn = new HubConnectionBuilder().withUrl(url).withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
            .configureLogging(LogLevel.Warning).build();
        connection.current = conn;
        const join = async () => {
            await conn.invoke('JoinSession', roomId, playerName, token);
            if (!stopped) { setStatus('Conectado'); setError(''); }
        };
        conn.on('GameStateUpdated', (snapshot: GameState) => {
            if (!stopped) setState(previous => previous && snapshot.revision < previous.revision ? previous : snapshot);
        });
        conn.on('Error', (message: string) => { if (!stopped) setError(message); });
        conn.on('PlayerLeft', () => { void conn.invoke('GetGameState', roomId).catch(() => {}); });
        conn.onreconnecting(() => { if (!stopped) setStatus('Reconectando…'); });
        conn.onreconnected(() => { void join().catch(e => { if (!stopped) setError(String(e)); }); });
        const start = async () => {
            try { await conn.start(); if (!stopped) await join(); }
            catch (e) {
                if (stopped) return;
                if (conn.state === HubConnectionState.Connected) { setError(String(e)); setStatus('No se pudo entrar'); }
                else { setStatus('Buscando el servidor…'); retry = setTimeout(() => void start(), 2500); }
            }
        };
        conn.onclose(() => { if (!stopped) { setStatus('Reconectando…'); retry = setTimeout(() => void start(), 2000); } });
        void start();
        return () => { stopped = true; clearTimeout(retry); void conn.stop(); connection.current = null; };
    }, [roomId, playerName]);
    const invoke = useCallback(async (method: string, ...args: unknown[]) => {
        if (pending.current) return false;
        if (connection.current?.state !== HubConnectionState.Connected) { setError('Esperá a recuperar la conexión.'); return false; }
        pending.current = true; setBusy(true); setError('');
        try { await connection.current.invoke(method, roomId, ...args); return true; }
        catch (e) { setError(e instanceof Error ? e.message.replace(/^.*HubException: /, '') : String(e)); return false; }
        finally { pending.current = false; setBusy(false); }
    }, [roomId]);
    return {
        state, status, error, busy, clearError: () => setError(''),
        play: (cards: Card[]) => invoke('PlayCardIds', cards.map(c => c.id)),
        pass: () => invoke('PassTurn'), start: () => invoke('StartGame'),
        selectMode: (count: number) => invoke('SelectGameMode', count),
        leave: () => invoke('LeaveRoom', playerName)
    };
}
