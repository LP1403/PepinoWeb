import { useState } from 'react'
import './App.css'
import Lobby from './components/Lobby';
import GameTable from './components/GameTable';
import CreatorTest from './components/CreatorTest';
import OverheadTable from './components/OverheadTable';
import PepinoArena from './components/PepinoArena';

function App() {
  const [roomId, setRoomId] = useState<string | null>(null);
  const [playerName, setPlayerName] = useState<string>("");
  const [useTestMode] = useState<boolean>(false); // Set true to preview the demo

  return (
    <div>
      {useTestMode ? (
        // Modo de prueba: mostrar rework PepinoArena (inspirado en composición UNO)
        <PepinoArena
          bottomPlayerId="p1"
          players={[
            { id: 'p1', name: 'Tú', cards: 9, avatarColor: '#2b6cb0' },
            { id: 'p2', name: 'Alex', cards: 5, avatarColor: '#b15b2b' },
            { id: 'p3', name: 'Sam', cards: 7, avatarColor: '#6b2bb1' },
            { id: 'p4', name: 'Rio', cards: 4, avatarColor: '#2bb18d' },
            { id: 'p5', name: 'Maya', cards: 6, avatarColor: '#b12b72' }
          ]}
        />
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
