import { useState } from 'react'
import './App.css'
import Lobby from './components/Lobby';
import GameTable from './components/GameTable';
import CreatorTest from './components/CreatorTest';

function App() {
  const [roomId, setRoomId] = useState<string | null>(null);
  const [playerName, setPlayerName] = useState<string>("");
  const [useTestMode] = useState<boolean>(false); // Cambiar a false para usar el juego normal

  return (
    <div>
      {useTestMode ? (
        // Modo de prueba para debuggear el creador
        <CreatorTest roomId="test123" playerName="TestPlayer" />
      ) : (
        // Modo normal del juego
        !roomId ? (
          <Lobby onJoin={(room, name) => { setRoomId(room); setPlayerName(name); }} />
        ) : (
          <GameTable roomId={roomId} playerName={playerName} />
        )
      )}
    </div>
  );
}

export default App
