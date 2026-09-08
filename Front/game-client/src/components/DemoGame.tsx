import { useState } from 'react';
import type { Card, GameState } from '../types/Card';
import GameTable3D from './GameTable3D';
function fixture(): GameState {
    const query = new URLSearchParams(location.search);
    const count = Math.min(8, Math.max(2, Number(query.get('players')) || 4));
    const handSize = Math.min(72, Math.max(3, Number(query.get('hand')) || 12));
    const values: Card['value'][] = [2,3,4,4,5,5,5,7,8,9,12,1];
    const hand: Card[] = Array.from({ length: handSize }, (_,i) => ({ id: `demo-${i}`, value: values[i % values.length], suit: (['♠','♥','♦','♣'] as const)[i % 4] }));
    return {
        roomId: 'VISTA-PREVIA', yourPlayerId: 'local', revision: 1, currentTurnIndex: 0,
        players: Array.from({ length: count }, (_, i) => ({ connectionId: i ? `rival-${i}` : 'local', name: ['Vos','Ángela','Katie','Miranda','Nico','Sofi','Leo','Vale'][i], cardCount: i ? 7 + i : handSize, isConnected: true, isCurrentTurn: i === 0, isSkipped: false, hasWon: false })),
        tableCards: [], lastPlayedCards: [{ id: 'pile', value: 4, suit: '♦' }], lastPlayerId: 'rival-1',
        lastPlay: { sequence: 1, cards: [{ id: 'pile', value: 4, suit: '♦' }], playerId: 'rival-1', playerName: 'Ángela', isPepineado: false },
        isGameStarted: true, isGameFinished: false, isPaused: false, notice: null,
        gameMode: { deckCount: 1, cardsPerPlayer: 12, maxWinners: 2 }, winners: [], roundNumber: 1, yourHand: hand, isRoomCreator: true, isNewRound: false
    };
}
export default function DemoGame() {
    const [state, setState] = useState(fixture);
    return <GameTable3D state={state} onLeave={() => { location.href = '/'; }} onPass={async () => { setState(s => ({ ...s, revision: s.revision + 1, isNewRound: true, lastPlayedCards: [], roundNumber: s.roundNumber + 1 })); return true; }} onPlay={async cards => {
        setState(s => ({ ...s, revision: s.revision + 1, yourHand: s.yourHand.filter(c => !cards.some(x => x.id === c.id)), lastPlayedCards: cards[0].value === 2 ? [] : cards, isNewRound: cards[0].value === 2,
            lastPlay: { sequence: (s.lastPlay?.sequence ?? 0) + 1, cards, playerId: 'local', playerName: 'Vos', isPepineado: false, isWildcard: cards[0].value === 2 } })); return true;
    }} />;
}
