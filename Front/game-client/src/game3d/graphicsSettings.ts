export type GraphicsQuality='auto'|'low'|'high';
export const GRAPHICS_EVENT='pepino-graphics-change';
const KEY='pepino-graphics-v1';
export function loadGraphicsQuality():GraphicsQuality {
    try {const saved=localStorage.getItem(KEY);return saved==='low'||saved==='high'?saved:'auto';}
    catch{return 'auto';}
}
export function saveGraphicsQuality(quality:GraphicsQuality) {
    try{localStorage.setItem(KEY,quality);}catch{/* Session setting still works. */}
    window.dispatchEvent(new CustomEvent(GRAPHICS_EVENT,{detail:quality}));
}
