import GameAudioControls from './GameAudioControls';
import GameGraphicsControls from './GameGraphicsControls';

export default function LobbyControls() {
    return <div className="top-actions lobby-top-actions">
        <GameAudioControls runtimeOnly />
        <GameGraphicsControls />
    </div>;
}
