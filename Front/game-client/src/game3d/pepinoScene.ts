import * as THREE from 'three';
import type { Card, PlayedCards, Player } from '../types/Card';
import { cardCanvas } from './cardArt';
import { seatPositions } from './layout';

export interface PepinoSceneApi {
    setOpponents(players: Player[]): void;
    setLastPlay(play: PlayedCards | null, animate: boolean): void;
    dispose(): void;
}
// Camera/board are world-space. Cards use a screen-space 3D layer so their reading size
// stays invariant when the board changes. UI seats use the same normalized layout.
export function createPepinoScene(container: HTMLElement): PepinoSceneApi {
    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 1.75));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.NeutralToneMapping;
    renderer.toneMappingExposure = .9;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.autoClear = false;
    container.appendChild(renderer.domElement);
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(43, 1, .1, 100);
    camera.position.set(0, 10, 9.3); camera.lookAt(0, 0, -.1);
    scene.add(new THREE.HemisphereLight(0xe4fbff, 0x607e40, 1.9));
    const sunlight = new THREE.DirectionalLight(0xfff5d4, 1.6);
    sunlight.position.set(-4, 9, 5); sunlight.castShadow = true;
    sunlight.shadow.mapSize.set(1024, 1024);
    Object.assign(sunlight.shadow.camera, { left: -8, right: 8, top: 8, bottom: -8 });
    sunlight.shadow.bias = -.002; scene.add(sunlight);
    const resources = new Set<THREE.BufferGeometry | THREE.Material | THREE.Texture>();
    const own = <T extends THREE.BufferGeometry | THREE.Material | THREE.Texture>(item: T): T => { resources.add(item); return item; };
    function mesh(geometry: THREE.BufferGeometry, color: number, x: number, y: number, z: number) {
        const result = new THREE.Mesh(own(geometry), own(new THREE.MeshStandardMaterial({ color, roughness: .9, flatShading: true })));
        result.position.set(x, y, z); result.castShadow = true; result.receiveShadow = true; scene.add(result); return result;
    }
    const island = mesh(new THREE.CylinderGeometry(5.5, 4.9, .65, 12), 0x8bbd47, 0, -.42, 0);
    island.scale.z = .91;
    const top = mesh(new THREE.CylinderGeometry(5.37, 5.48, .2, 64), 0xacd55d, 0, -.03, 0);
    top.scale.z = .91;
    const inner = mesh(new THREE.CircleGeometry(3.7, 64), 0xbedf79, 0, .077, -.1);
    inner.rotation.x = -Math.PI / 2; inner.scale.y = .85;
    const ring = mesh(new THREE.TorusGeometry(3.76, .025, 6, 96), 0xdff0a2, 0, .08, -.1);
    ring.rotation.x = -Math.PI / 2; ring.scale.y = .85;
    for (let i = 0; i < 13; i++) {
        const a = i * Math.PI * 2 / 13;
        const rock = mesh(new THREE.DodecahedronGeometry(.2 + (i % 3) * .08, 0), 0xe4dfa5, Math.cos(a) * 4.8, .1, Math.sin(a) * 4.35);
        rock.scale.y = .35; rock.rotation.y = a;
        if (i % 3 === 0) {
            mesh(new THREE.IcosahedronGeometry(.55, 0), 0x68a646, Math.cos(a) * 5.1, .38, Math.sin(a) * 4.45);
            mesh(new THREE.IcosahedronGeometry(.38, 0), 0x8bc653, Math.cos(a) * 5.25, .4, Math.sin(a) * 4.6);
        }
    }
    const overlay = new THREE.Scene();
    const screen = new THREE.OrthographicCamera(-1, 1, 1, -1, .1, 100);
    screen.position.z = 30;
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
    let flights: { mesh: THREE.Mesh; start: THREE.Vector3; end: THREE.Vector3; time: number; rotation: number; order: number }[] = [];
    const point = (x: number, y: number) => new THREE.Vector3(x * width - width / 2, height / 2 - y * height, 1);
    function renderOpponents() {
        backs.clear();
        const seats = seatPositions(opponents.length, width < 600);
        opponents.forEach((p, index) => {
            const seat = seats[index];
            const n = Math.min(p.cardCount, opponents.length > 3 ? 4 : 6);
            const ch = Math.min(height * (opponents.length > 3 ? .125 : .17), width * (opponents.length > 3 ? .15 : .18));
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
    }
    function renderPile(animate: boolean) {
        pile.clear(); flights = [];
        if (!lastPlay) return;
        const seats = seatPositions(opponents.length, width < 600);
        const from = opponents.findIndex(p => p.connectionId === lastPlay!.playerId);
        const origin = from >= 0 ? point(seats[from].x, seats[from].y) : point(.5, .84);
        const n = lastPlay.cards.length;
        const ch = Math.min(height * .205, width * .23);
        const spread = Math.min(ch * .47, width * .35 / Math.max(1, n - 1));
        lastPlay.cards.forEach((card, i) => {
            const m = cardMesh(card); m.scale.set(ch * 360 / 520, ch, 1);
            const end = point(.5, width < 600 && opponents.length > 3 ? .50 : .455); end.x += (i - (n - 1) / 2) * spread;
            const rotation = -.065 + (i - (n - 1) / 2) * .025;
            m.position.copy(animate ? origin : end); m.rotation.z = rotation; m.renderOrder = 30 + i;
            pile.add(m);
            if (animate) flights.push({ mesh: m, start: origin.clone(), end, time: performance.now() + i * 55, rotation, order: i });
        });
    }
    function resize() {
        width = container.clientWidth; height = container.clientHeight || 1;
        renderer.setSize(width, height); camera.aspect = width / height;
        camera.fov = width / height < 1 ? 58 : 43; camera.updateProjectionMatrix();
        screen.left = -width / 2; screen.right = width / 2; screen.top = height / 2; screen.bottom = -height / 2; screen.updateProjectionMatrix();
        renderOpponents(); renderPile(false);
    }
    const observer = new ResizeObserver(resize); observer.observe(container);
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
    let raf = 0;
    function frame(now: number) {
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
        dispose() { cancelAnimationFrame(raf); observer.disconnect(); resources.forEach(r => r.dispose()); renderer.dispose(); renderer.domElement.remove(); }
    };
}
