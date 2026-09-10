export type GraphicsQuality='auto'|'low'|'high';
export const GRAPHICS_EVENT='pepino-graphics-change';
const KEY='pepino-graphics-v1';
export const FPS_EVENT='pepino-fps-change';
let sessionFps:boolean|undefined;
export function loadShowFps():boolean {
    if(sessionFps!==undefined)return sessionFps;
    try{return localStorage.getItem('pepino-show-fps')==='true';}catch{return false;}
}
export function saveShowFps(show:boolean) {
    sessionFps=show;
    try{localStorage.setItem('pepino-show-fps',String(show));}catch{/* Keep session preference. */}
    window.dispatchEvent(new CustomEvent(FPS_EVENT,{detail:show}));
}
export function loadGraphicsQuality():GraphicsQuality {
    try {const saved=localStorage.getItem(KEY);return saved==='low'||saved==='high'?saved:'auto';}
    catch{return 'auto';}
}
export function saveGraphicsQuality(quality:GraphicsQuality) {
    try{localStorage.setItem(KEY,quality);}catch{/* Session setting still works. */}
    window.dispatchEvent(new CustomEvent(GRAPHICS_EVENT,{detail:quality}));
}
