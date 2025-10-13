import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import type { GameMode, Player } from '../types/Card';

interface CreatorTestProps {
    roomId: string;
    playerName: string;
}

export default function CreatorTest({ roomId, playerName }: CreatorTestProps) {
    const [connection, setConnection] = useState<signalR.HubConnection>();
    const [isConnected, setIsConnected] = useState(false);
    const [isRoomCreator, setIsRoomCreator] = useState(false);
    const [gameMode, setGameMode] = useState<GameMode | null>(null);
    const [players, setPlayers] = useState<Player[]>([]);
    const [logs, setLogs] = useState<string[]>([]);

    const addLog = (message: string) => {
        const timestamp = new Date().toLocaleTimeString();
        setLogs(prev => [...prev, `[${timestamp}] ${message}`]);
        console.log(message);
    };

    // Conectar al hub
    useEffect(() => {
        addLog('🔄 Iniciando conexión SignalR...');
        const conn = new signalR.HubConnectionBuilder()
            .withUrl("http://127.0.0.1:5264/gamehub")
            .build();

        setConnection(conn);

        conn.start().then(() => {
            addLog('✅ Conectado a SignalR exitosamente');
            setIsConnected(true);
            addLog(`🎯 Uniéndose a sala: ${roomId} como: ${playerName}`);
            conn.invoke("JoinRoom", roomId, playerName);
        }).catch(err => {
            addLog(`❌ Error de conexión SignalR: ${err}`);
        });

        return () => {
            conn.stop();
        };
    }, [roomId, playerName]);

    // Configurar eventos
    useEffect(() => {
        if (!connection) return;

        connection.on("PlayerJoined", (name: string, count: number) => {
            addLog(`👤 ${name} se unió. Jugadores: ${count}`);
        });

        connection.on("GameStateUpdated", (state: { isRoomCreator: boolean; gameMode: GameMode | null; players: Player[] }) => {
            addLog('🔄 Estado del juego actualizado');
            addLog(`👑 IsRoomCreator: ${state.isRoomCreator}`);
            addLog(`🎯 GameMode: ${state.gameMode ? state.gameMode.deckCount + ' mazos' : 'null'}`);
            addLog(`👥 Jugadores: ${state.players.length}`);

            setIsRoomCreator(state.isRoomCreator);
            setGameMode(state.gameMode);
            setPlayers(state.players);
        });

        connection.on("Error", (msg: string) => {
            addLog(`❌ Error: ${msg}`);
        });

        return () => {
            connection.off("PlayerJoined");
            connection.off("GameStateUpdated");
            connection.off("Error");
        };
    }, [connection]);

    const selectMode = async (deckCount: number) => {
        if (!connection || !isRoomCreator) {
            addLog('❌ No puedes seleccionar modo (no eres creador o no conectado)');
            return;
        }

        try {
            addLog(`🎯 Seleccionando modo: ${deckCount} mazos`);
            await connection.invoke("SelectGameMode", roomId, deckCount);
            addLog('✅ Modo seleccionado enviado');
        } catch (error) {
            addLog(`❌ Error seleccionando modo: ${error}`);
        }
    };

    const startGame = async () => {
        if (!connection || !isRoomCreator || !gameMode) {
            addLog('❌ No puedes iniciar el juego');
            return;
        }

        try {
            addLog('🎮 Iniciando juego...');
            await connection.invoke("StartGame", roomId);
            addLog('✅ Comando de iniciar juego enviado');
        } catch (error) {
            addLog(`❌ Error iniciando juego: ${error}`);
        }
    };

    return (
        <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
            <h1>🥒 Test Creador de Sala - React</h1>

            <div style={{ marginBottom: '20px' }}>
                <h3>📊 Estado Actual</h3>
                <p><strong>Conectado:</strong> {isConnected ? '✅ Sí' : '❌ No'}</p>
                <p><strong>Es Creador:</strong> {isRoomCreator ? '👑 Sí' : '❌ No'}</p>
                <p><strong>Modo de Juego:</strong> {gameMode ? `${gameMode.deckCount} mazos` : 'No seleccionado'}</p>
                <p><strong>Jugadores:</strong> {players.length}</p>
                <p><strong>Jugadores:</strong> {players.map(p => p.name).join(', ')}</p>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3>🎮 Controles</h3>
                <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                    <button
                        onClick={() => selectMode(1)}
                        disabled={!isConnected || !isRoomCreator}
                        style={{ padding: '10px', background: '#4caf50', color: 'white', border: 'none', borderRadius: '5px' }}
                    >
                        🎯 1 Mazo
                    </button>
                    <button
                        onClick={() => selectMode(2)}
                        disabled={!isConnected || !isRoomCreator}
                        style={{ padding: '10px', background: '#4caf50', color: 'white', border: 'none', borderRadius: '5px' }}
                    >
                        🎯 2 Mazos
                    </button>
                    <button
                        onClick={() => selectMode(3)}
                        disabled={!isConnected || !isRoomCreator}
                        style={{ padding: '10px', background: '#4caf50', color: 'white', border: 'none', borderRadius: '5px' }}
                    >
                        🎯 3 Mazos
                    </button>
                </div>

                {isRoomCreator && gameMode && (
                    <button
                        onClick={startGame}
                        style={{
                            padding: '15px 30px',
                            background: '#2196f3',
                            color: 'white',
                            border: 'none',
                            borderRadius: '5px',
                            fontSize: '16px',
                            fontWeight: 'bold'
                        }}
                    >
                        🎮 Iniciar Juego
                    </button>
                )}
            </div>

            <div style={{ marginTop: '20px' }}>
                <h3>📋 Logs</h3>
                <div style={{
                    background: '#f5f5f5',
                    padding: '10px',
                    borderRadius: '5px',
                    maxHeight: '300px',
                    overflowY: 'auto',
                    fontFamily: 'monospace',
                    fontSize: '12px'
                }}>
                    {logs.map((log, index) => (
                        <div key={index}>{log}</div>
                    ))}
                </div>
            </div>
        </div>
    );
} 