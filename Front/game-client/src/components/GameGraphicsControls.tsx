import {useEffect,useRef,useState} from 'react';
import GameModal from './GameModal';
import {loadGraphicsQuality,saveGraphicsQuality} from '../game3d/graphicsSettings';
import type {GraphicsQuality} from '../game3d/graphicsSettings';
import GameAudioControls from './GameAudioControls';
import type {GameState} from '../types/Card';

export default function GameGraphicsControls({state}:{state:GameState}) {
    const [open,setOpen]=useState(false);
    const [quality,setQuality]=useState(loadGraphicsQuality);
    const [fullscreen,setFullscreen]=useState(!!document.fullscreenElement);
    const [message,setMessage]=useState('');
    const button=useRef<HTMLButtonElement>(null);
    const fullscreenButton=useRef<HTMLButtonElement>(null);
    useEffect(()=>{
        const update=()=>setFullscreen(!!document.fullscreenElement);
        document.addEventListener('fullscreenchange',update);
        return()=>document.removeEventListener('fullscreenchange',update);
    },[]);
    async function toggleFullscreen() {
        setMessage('');
        try {
            if(document.fullscreenElement)await document.exitFullscreen();
            else await (fullscreenButton.current ?? button.current)?.closest<HTMLElement>('.pepino-game')?.requestFullscreen();
        } catch {setMessage('El navegador no permitió activar la pantalla completa.');}
    }
    return <>
        <button ref={button} className="graphics-button" aria-label="Ajustes gráficos" title="Ajustes gráficos" onClick={()=>setOpen(true)}>⚙</button>
        {document.fullscreenEnabled && <button ref={fullscreenButton} className="graphics-button fullscreen-button" aria-label={fullscreen?'Salir de pantalla completa':'Pantalla completa'} title={fullscreen?'Salir de pantalla completa':'Pantalla completa'} onClick={()=>void toggleFullscreen()}>⛶</button>}
        {open && <GameModal title="Configuración" onClose={()=>setOpen(false)}>
            <section className="graphics-section">
            <h3>Calidad de imagen</h3>
            <label className="graphics-quality">
                <select aria-label="Calidad de imagen" value={quality} onChange={event=>{
                    const next=event.target.value as GraphicsQuality;setQuality(next);saveGraphicsQuality(next);
                }}>
                    <option value="auto">Automática</option>
                    <option value="low">Ahorro</option>
                    <option value="high">Alta</option>
                </select>
            </label>
            <p>{quality==='low'?'Reduce resolución y desactiva sombras.':quality==='high'?'Mayor nitidez; puede consumir más batería.':'Ajusta la resolución al tamaño de pantalla.'}</p>
            </section>
            <GameAudioControls state={state} embedded />
            {message && <p role="status">{message}</p>}
            <div className="modal-actions"><button className="secondary-button" onClick={()=>setOpen(false)}>VOLVER A LA MESA</button></div>
        </GameModal>}
    </>;
}
