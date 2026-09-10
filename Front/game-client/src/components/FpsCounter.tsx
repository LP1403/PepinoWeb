import {useEffect,useState} from 'react';
import type {RefObject} from 'react';
import {FPS_EVENT,loadShowFps} from '../game3d/graphicsSettings';

export default function FpsCounter({sceneHost}:{sceneHost:RefObject<HTMLDivElement|null>}) {
    const [visible,setVisible]=useState(loadShowFps);
    const [fps,setFps]=useState<string>();
    useEffect(()=>{
        const changed=()=>setVisible(loadShowFps());
        window.addEventListener(FPS_EVENT,changed);
        return()=>window.removeEventListener(FPS_EVENT,changed);
    },[]);
    useEffect(()=>{
        if(!visible || !sceneHost.current)return;
        const host=sceneHost.current;
        const update=()=>setFps(host.querySelector('canvas')?.dataset.fps);
        const observer=new MutationObserver(update);
        observer.observe(host,{childList:true,subtree:true,attributes:true,attributeFilter:['data-fps']});
        update();
        return()=>observer.disconnect();
    },[visible,sceneHost]);
    return visible ? <span className="fps-counter" aria-label="Fotogramas por segundo">{fps ?? '—'} FPS</span> : null;
}
