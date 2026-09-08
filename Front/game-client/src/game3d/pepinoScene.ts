import * as THREE from 'three';
import type { Card, PlayedCards, Player } from '../types/Card';
import { cardCanvas } from './cardArt';
import { seatPositions } from './layout';
import { buildTableEnvironment } from './tableEnvironment';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

export interface PepinoSceneApi {
    setOpponents(players: Player[]): void;
    setLastPlay(play: PlayedCards | null, animate: boolean): void;
    dispose(): void;
}
// Camera/board are world-space. Cards use a screen-space 3D layer so their reading size
// stays invariant when the board changes. UI seats use the same normalized layout.
export function createPepinoScene(container: HTMLElement, lobby = false): PepinoSceneApi {
    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 1.75));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.NeutralToneMapping;
    renderer.toneMappingExposure = 1.1;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.autoClear = false;
    container.appendChild(renderer.domElement);
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x171713);
    scene.fog = new THREE.FogExp2(0x171713, .027);
    const camera = new THREE.PerspectiveCamera(43, 1, .1, 100);
    camera.position.set(0, 5.4, 8.3); camera.lookAt(0, 0, -.3);
    scene.add(new THREE.HemisphereLight(0xf5e8d4, 0x282119, 1.25));
    const sunlight = new THREE.DirectionalLight(0xffe0ad, 2.2);
    sunlight.position.set(-4, 9, 5); sunlight.castShadow = true;
    sunlight.shadow.mapSize.set(1024, 1024);
    Object.assign(sunlight.shadow.camera, { left: -8, right: 8, top: 8, bottom: -8 });
    sunlight.shadow.bias = -.002; scene.add(sunlight);
    const resources = new Set<THREE.BufferGeometry | THREE.Material | THREE.Texture>();
    const own = <T extends THREE.BufferGeometry | THREE.Material | THREE.Texture>(item: T): T => { resources.add(item); return item; };
    buildTableEnvironment(scene, own);
    const overlay = new THREE.Scene();
    overlay.add(new THREE.HemisphereLight(0xffeddb, 0x403529, 2));
    const handLight = new THREE.DirectionalLight(0xffe3c4, 2); handLight.position.set(-2,4,10); overlay.add(handLight);
    const screen = new THREE.OrthographicCamera(-1, 1, 1, -1, .1, 2000);
    screen.position.z = 1000;
    const cardGeometry = own(new THREE.PlaneGeometry(1, 1));
    const textures = new Map<string, THREE.MeshBasicMaterial>();
    function cardMesh(card?: Card) {
        const key = card ? `${card.suit}${card.value}` : 'back';
        if (!textures.has(key)) {
            const tex = own(new THREE.CanvasTexture(cardCanvas(card))); tex.colorSpace = THREE.SRGBColorSpace;
            textures.set(key, own(new THREE.MeshBasicMaterial({ map: tex, transparent: true, depthTest: false, toneMapped: false })));
        }
        return new THREE.Mesh(cardGeometry, textures.get(key)!);
    }
    let width = 1, height = 1, opponents: Player[] = [], lastPlay: PlayedCards | null = null;
    const backs = new THREE.Group(), pile = new THREE.Group(); overlay.add(backs, pile);
    const hands = new THREE.Group(); overlay.add(hands);
    const sleeveGeometry = own(new THREE.CylinderGeometry(.23,.32,1,16));
    const sleeveMaterial = own(new THREE.MeshStandardMaterial({color:0x202a29,roughness:1}));
    let hand: THREE.Object3D | null = null, disposed = false;
    if (!lobby) new GLTFLoader().load('/models/pepino-hand.glb', gltf => {
        gltf.scene.traverse(object => {
            if (object instanceof THREE.Mesh) {
                own(object.geometry);
                const materials = Array.isArray(object.material) ? object.material : [object.material];
                materials.forEach(material => { own(material); if (material.map) own(material.map); });
            }
        });
        if (disposed) { resources.forEach(resource => resource.dispose()); return; }
        hand = gltf.scene; renderOpponents();
    }, undefined, () => { /* Cards stay fully playable if the decorative model fails to load. */ });
    let flights: { mesh: THREE.Mesh; start: THREE.Vector3; end: THREE.Vector3; time: number; rotation: number; order: number }[] = [];
    const point = (x: number, y: number) => new THREE.Vector3(x * width - width / 2, height / 2 - y * height, 1);
    function handPair(x: number, y: number, size: number, local = false, playerId = '') {
        if (!hand) return;
        for (const side of [-1,1]) {
            const model = hand.clone();
            model.position.copy(point(x,y)); model.position.x += side * size * .8; model.position.y += (local?-1:1)*size*.36; model.position.z=-3;
            model.scale.set(side*size*1.5,size*1.5,size*1.5); model.rotation.set(Math.PI/2,0,(local?0:Math.PI)+side*-.5); hands.add(model);
            model.userData = { baseY:model.position.y, playerId, local };
            const sleeve = new THREE.Mesh(sleeveGeometry,sleeveMaterial);
            sleeve.position.copy(model.position); sleeve.position.y+=(local?-1:1)*size*.87; sleeve.position.z=-8;
            sleeve.scale.set(size,size*(local?2:.85),size); sleeve.rotation.z=side*-.08; hands.add(sleeve);
            sleeve.userData = { baseY:sleeve.position.y, playerId, local };
        }
    }
    function renderOpponents() {
        backs.clear();
        hands.clear();
        const seats = seatPositions(opponents.length, width < 600);
        opponents.forEach((p, index) => {
            const seat = seats[index];
            const n = Math.min(p.cardCount, opponents.length > 3 ? 4 : 6);
            const ch = Math.min(height * (opponents.length > 3 ? .085 : .11), width * (opponents.length > 3 ? .13 : .17));
            if (n > 0 && !(width < 600 && opponents.length > 3)) handPair(seat.x,seat.y,ch,false,p.connectionId);
            for (let i = 0; i < n; i++) {
                const m = cardMesh(); m.scale.set(ch * 360 / 520, ch, 1);
                m.position.copy(point(seat.x, seat.y));
                const offset = (i - (n - 1) / 2) * ch * .22;
                m.position.x += Math.cos(seat.rotation) * offset;
                m.position.y += Math.sin(seat.rotation) * offset - Math.abs(i - (n - 1) / 2) * 2;
                m.rotation.z = seat.rotation + (i - (n - 1) / 2) * -.025;
                m.renderOrder = i; backs.add(m);
            }
        });
        if (!lobby && width >= 600) handPair(.5,.86,Math.min(height*.19,width*.16),true);
    }
    function renderPile(animate: boolean) {
        pile.clear(); flights = [];
        if (!lastPlay) return;
        const seats = seatPositions(opponents.length, width < 600);
        const from = opponents.findIndex(p => p.connectionId === lastPlay!.playerId);
        const origin = from >= 0 ? point(seats[from].x, seats[from].y) : point(.5, .84);
        const n = lastPlay.cards.length;
        const ch = Math.min(height * .17, width * .23);
        const spread = Math.min(ch * .47, width * .35 / Math.max(1, n - 1));
        lastPlay.cards.forEach((card, i) => {
            const m = cardMesh(card); m.scale.set(ch * 360 / 520, ch, 1);
            const end = point(.5, .46); end.x += (i - (n - 1) / 2) * spread;
            const rotation = -.065 + (i - (n - 1) / 2) * .025;
            m.position.copy(animate ? origin : end); m.rotation.set(-.5,0,rotation); m.renderOrder = 30 + i;
            pile.add(m);
            if (animate) flights.push({ mesh: m, start: origin.clone(), end, time: performance.now() + i * 55, rotation, order: i });
        });
    }
    function resize() {
        width = container.clientWidth; height = container.clientHeight || 1;
        renderer.setSize(width, height); camera.aspect = width / height;
        camera.fov = width / height < 1 ? 62 : 48;
        camera.position.set(0, lobby ? 10.8 : 5.4, lobby ? 6.8 : 8.3);
        camera.lookAt(0, 0, lobby ? 0 : -.3); camera.updateProjectionMatrix();
        if(lobby && width>900) camera.setViewOffset(width,height,-width*.18,0,width,height);
        else camera.clearViewOffset();
        screen.left = -width / 2; screen.right = width / 2; screen.top = height / 2; screen.bottom = -height / 2; screen.updateProjectionMatrix();
        renderOpponents(); renderPile(false);
    }
    const observer = new ResizeObserver(resize); observer.observe(container);
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
    const enteredAt = performance.now();
    let raf = 0;
    function frame(now: number) {
        if (!lobby && !reducedMotion && now-enteredAt<1000) {
            const t=1-Math.pow(1-Math.min(1,(now-enteredAt)/900),3);
            camera.position.set(0,10.8+(5.4-10.8)*t,6.8+(8.3-6.8)*t);camera.lookAt(0,0,-.3);
        }
        const firstFlight=flights[0];
        hands.children.forEach(model=>{
            const isAuthor=model.userData.playerId===lastPlay?.playerId || (model.userData.local && !opponents.some(p=>p.connectionId===lastPlay?.playerId));
            const progress=firstFlight?Math.max(0,Math.min(1,(now-firstFlight.time)/540)):0;
            model.position.y=model.userData.baseY+(isAuthor?Math.sin(progress*Math.PI)*height*.012:0);
        });
        for (const f of flights) {
            const t = Math.max(0, Math.min(1, (now - f.time) / 540));
            const ease = 1 - Math.pow(1 - t, 3);
            f.mesh.position.lerpVectors(f.start, f.end, ease);
            f.mesh.position.y += Math.sin(t * Math.PI) * height * .09;
            f.mesh.rotation.z = f.rotation + Math.sin(t * Math.PI) * .15;
        }
        flights = flights.filter(f => now < f.time + 540);
        renderer.domElement.dataset.animating = String(flights.length > 0);
        renderer.clear(); renderer.render(scene, camera); renderer.clearDepth(); renderer.render(overlay, screen);
        raf = requestAnimationFrame(frame);
    }
    resize(); raf = requestAnimationFrame(frame);
    return {
        setOpponents(players) { opponents = players; renderOpponents(); },
        setLastPlay(play, animate) { lastPlay = play; renderPile(animate && !reducedMotion); },
        dispose() { disposed = true; cancelAnimationFrame(raf); observer.disconnect(); resources.forEach(r => r.dispose()); renderer.dispose(); renderer.domElement.remove(); }
    };
}
