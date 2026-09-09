import { useState } from 'react';
import type { Card, GameState } from '../types/Card';
import GameTable3D from './GameTable3D';
function fixture(): GameState {
    const query = new URLSearchParams(location.search);
    const count = Math.floor(Math.min(8, Math.max(2, Number(query.get('players')) || 4)));
    const handSize = Math.floor(Math.min(72, Math.max(3, Number(query.get('hand')) || 12)));
    const turnIndex=Math.floor(Math.min(count-1,Math.max(0,Number(query.get('turn')) || 0)));
    const rivalHand=query.has('rivalHand') ? Math.floor(Math.min(72,Math.max(1,Number(query.get('rivalHand')) || 1))) : null;
    const values: Card['value'][] = [2,3,4,4,5,5,5,7,8,9,12,1];
    const played: Card[]=Array.from({length:Math.max(1,Math.min(12,Number(query.get('played'))||1))},(_,i)=>({id:`pile-${i}`,value:4,suit:'♦',deckIndex:i%3}));
    const hand: Card[] = Array.from({ length: handSize }, (_,i) => ({ id: `demo-${i}`, value: values[i % values.length], suit: (['♠','♥','♦','♣'] as const)[i % 4] }));
    return {
        roomId: query.get('demoRoom')?.slice(0,32) || 'VISTA-PREVIA', yourPlayerId: 'local', revision: 1, currentTurnIndex: turnIndex,
        players: Array.from({ length: count }, (_, i) => ({ connectionId: i ? `rival-${i}` : 'local', name: ['Vos','Ángela','Katie','Miranda','Nico','Sofi','Leo','Vale'][i], cardCount: i ? rivalHand ?? 7+i : handSize, isConnected: true, isCurrentTurn: i === turnIndex, isSkipped: false, hasWon: false })),
        tableCards: Array.from({length:Math.min(144,Math.max(1,Number(query.get('discard'))||1))},(_,i)=>({id:`discard-${i}`,value:4,suit:'♦'} as Card)), lastPlayedCards: [{ id: 'pile', value: 4, suit: '♦' }], lastPlayerId: 'rival-1',
        lastPlay: { sequence: 1, cards: played, playerId: 'rival-1', playerName: 'Ángela', isPepineado: false },
        isGameStarted: true, isGameFinished: false, isPaused: query.get('paused')==='1', notice: null,
        gameMode: { deckCount: 1, cardsPerPlayer: 12, maxWinners: 2 }, winners: [], roundNumber: 1, yourHand: hand, isRoomCreator: true, isNewRound: false
    };
}
export default function DemoGame() {
    const [state, setState] = useState(fixture);
    return <GameTable3D state={state} onLeave={() => { location.href = '/'; }} onPass={async () => { setState(s => ({ ...s, revision: s.revision + 1, isNewRound: true, lastPlayedCards: [], roundNumber: s.roundNumber + 1 })); return true; }} onPlay={async cards => {
        setState(s => ({ ...s, revision: s.revision + 1, tableCards:[...s.tableCards,...cards], yourHand: s.yourHand.filter(c => !cards.some(x => x.id === c.id)), lastPlayedCards: cards[0].value === 2 ? [] : cards, isNewRound: cards[0].value === 2,
            lastPlay: { sequence: (s.lastPlay?.sequence ?? 0) + 1, cards, playerId: 'local', playerName: 'Vos', isPepineado: false, isWildcard: cards[0].value === 2 } })); return true;
    }} />;
}
