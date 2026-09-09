import * as THREE from 'three';
import type { Card, PlayedCards, Player } from '../types/Card';
import { cardCanvas } from './cardArt';
import { seatPositions } from './layout';
import { buildTableEnvironment } from './tableEnvironment';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { clone as cloneSkeleton } from 'three/examples/jsm/utils/SkeletonUtils.js';
import {GRAPHICS_EVENT,loadGraphicsQuality} from './graphicsSettings';

export interface PepinoSceneApi {
    setOpponents(players: Player[]): void;
    setTurnIndicator(active: boolean): void;
    setLastPlay(play: PlayedCards | null, animate: boolean): void;
    setDiscardCount(count:number):void;
    dispose(): void;
}
// Camera/board are world-space. Cards use a screen-space 3D layer so their reading size
// stays invariant when the board changes. UI seats use the same normalized layout.
export function createPepinoScene(container: HTMLElement, lobby = false): PepinoSceneApi {
    let graphicsQuality=loadGraphicsQuality();
    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 1.75));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.NeutralToneMapping;
    renderer.toneMappingExposure = 1.25;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.autoClear = false;
    renderer.info.autoReset = false;
    container.appendChild(renderer.domElement);
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x171713);
    scene.fog = new THREE.FogExp2(0x171713, .027);
    const camera = new THREE.PerspectiveCamera(43, 1, .1, 100);
    camera.position.set(0, 5.4, 8.3); camera.lookAt(0, 0, -.3);
    scene.add(new THREE.HemisphereLight(0xffedd7, 0x332619, 1.65));
    const sunlight = new THREE.DirectionalLight(0xffe0ad, 2.65);
    sunlight.position.set(-4, 9, 5); sunlight.castShadow = true;
    sunlight.shadow.mapSize.set(1024, 1024);
    Object.assign(sunlight.shadow.camera, { left: -8, right: 8, top: 8, bottom: -8 });
    sunlight.shadow.bias = -.002; scene.add(sunlight);
    const resources = new Set<THREE.BufferGeometry | THREE.Material | THREE.Texture>();
    const own = <T extends THREE.BufferGeometry | THREE.Material | THREE.Texture>(item: T): T => { resources.add(item); return item; };
    const environment=buildTableEnvironment(scene, own);
    let mateAngle=0,mateFrom=0,mateStarted=0,turnIndicator=false;
    const mateFromPosition=new THREE.Vector3(3.75,0,1.7);
    const mateTargetPosition=new THREE.Vector3(3.75,0,1.7);
    let discardLabelDirty=true;
    const overlay = new THREE.Scene();
    const localCards = new THREE.Scene();
    const localMeshes = new Map<string, THREE.Mesh>();
    const handClip = new THREE.Vector4();
    overlay.add(new THREE.HemisphereLight(0xffeddb, 0x403529, 2));
    const handLight = new THREE.DirectionalLight(0xffe3c4, 2); handLight.position.set(-2,4,10); overlay.add(handLight);
    const screen = new THREE.OrthographicCamera(-1, 1, 1, -1, .1, 2000);
    screen.position.z = 1000;
    const cardGeometry = own(new THREE.PlaneGeometry(1, 1));
    const textures = new Map<string, THREE.MeshBasicMaterial>();
    function cardMaterial(card?: Card, highlighted=false) {
        const key = (card ? `${card.suit}${card.value}` : 'back') + (highlighted?'-highlight':'');
        if (!textures.has(key)) {
            const source=cardCanvas(card);
            const art=highlighted ? document.createElement('canvas') : source;
            if(highlighted) {
                art.width=source.width;art.height=source.height;
                const pen=art.getContext('2d')!;
                pen.drawImage(source,0,0);
                pen.strokeStyle='#56a894';pen.lineWidth=8;
                pen.beginPath();pen.roundRect(7,7,346,506,20);pen.stroke();
            }
            const tex = own(new THREE.CanvasTexture(art)); tex.colorSpace = THREE.SRGBColorSpace;
            textures.set(key, own(new THREE.MeshBasicMaterial({ map: tex, transparent: true, alphaTest:.5, depthTest: true, toneMapped: false })));
        }
        return textures.get(key)!;
    }
    function cardMesh(card?: Card) { return new THREE.Mesh(cardGeometry,cardMaterial(card)); }
    let width = 1, height = 1, opponents: Player[] = [], lastPlay: PlayedCards | null = null;
    const pile = new THREE.Group(); scene.add(pile);
    const physicalMaterials = new Map<string, THREE.MeshStandardMaterial>();
    const discard=new THREE.Group();scene.add(discard);
    const discardMaterial=own(new THREE.MeshStandardMaterial({map:cardMaterial().map,transparent:true,alphaTest:.5,roughness:.85,side:THREE.DoubleSide}));
    const hands = new THREE.Group(); overlay.add(hands);
    const sleeveGeometry = own(new THREE.CylinderGeometry(.23,.32,1,16));
    const sleeveMaterial = own(new THREE.MeshStandardMaterial({color:0x202a29,roughness:1}));
    let hand: THREE.Object3D | null = null, disposed = false;
    if (!lobby) new GLTFLoader().load('/models/pepino-hand-rigged.glb', gltf => {
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
    function handPair(x: number, y: number, size: number, local = false, playerId = '', rotation = 0, cardCount = 1, assembly?: THREE.Group) {
        if (!hand) return;
        for (const side of [-1,1]) {
            const model = cloneSkeleton(hand);
            const fingers: THREE.Bone[] = [];
            model.updateMatrixWorld(true);
            model.traverse(node => {
                if (node instanceof THREE.Bone && /^Bone(Pinky|Index|Ring|Middle)/.test(node.name)) {
                    // The imported bones have different local axes. Curl all fingers
                    // around the hand's transverse axis, not each bone's arbitrary X.
                    const axis = new THREE.Vector3(1,0,0).applyQuaternion(node.getWorldQuaternion(new THREE.Quaternion()).invert());
                    const bend = /End/.test(node.name) ? -.48 : /Mid/.test(node.name) && !/Base/.test(node.name) ? -.62 : -.12;
                    node.rotateOnAxis(axis,bend);
                    node.updateWorldMatrix(false,true);
                    node.userData.curlAxis = axis;
                    node.userData.restQuaternion = node.quaternion.clone();
                    fingers.push(node);
                }
            });
            const fanHalfWidth = (360/520 + (cardCount-1)*.22)/2;
            const dx = side * size * (local ? .65 : fanHalfWidth-.17), dy = local ? -size*.36 : -size*.39;
            model.position.copy(assembly ? new THREE.Vector3() : point(x,y));
            model.position.x += Math.cos(rotation)*dx-Math.sin(rotation)*dy;
            model.position.y += Math.sin(rotation)*dx+Math.cos(rotation)*dy; model.position.z=-size*.3;
            const handScale = size * (local ? 1.5 : 1.35);
            model.scale.set(side*handScale,handScale,handScale); model.rotation.set(Math.PI/2,0,(local?side*-.5:Math.PI-side*.85)+rotation); (assembly ?? hands).add(model);
            // Wrists below/outside, fingertips rising towards the centre of the fan.
            if (!local) model.rotation.set(Math.PI/2,0,side*1.05+rotation,'ZXY');
            model.updateMatrixWorld(true);
            const gripBounds = new THREE.Box3().setFromObject(model,true);
            // Keep the palm behind the fan and expose the grip at its lower corners.
            const gripDepth = local ? 13 : size*.16 + (cardCount-1)*.7;
            model.position.z += gripDepth - gripBounds.max.z;
            model.userData = { baseX:model.position.x, baseY:model.position.y, playerId, local, fingers };
            if (!local) continue;
            const sleeve = new THREE.Mesh(sleeveGeometry,sleeveMaterial);
            sleeve.position.copy(model.position); sleeve.position.x-=Math.sin(rotation)*(local?-1:1)*size*.65; sleeve.position.y+=Math.cos(rotation)*(local?-1:1)*size*.65; sleeve.position.z=-8;
            sleeve.scale.set(size,size*(local?1:.4),size); sleeve.rotation.z=side*-.08+rotation; hands.add(sleeve);
            sleeve.userData = { baseX:sleeve.position.x, baseY:sleeve.position.y, playerId, local };
        }
    }
    function renderOpponents() {
        hands.traverse(node => { if (node instanceof THREE.SkinnedMesh) node.skeleton.dispose(); });
        hands.clear();
        const seats = seatPositions(opponents.length, width < 600, width>=601 && height<=550);
        opponents.forEach((p, index) => {
            const seat = seats[index];
            // Hands and cards share the seat orientation, including its foreshortening.
            const assembly = new THREE.Group();
            assembly.position.copy(point(seat.x,seat.y));
            hands.add(assembly);
            const fanRotation = 0;
            const n = Math.min(p.cardCount, opponents.length > 3 ? 4 : 6);
            const ch = Math.min(height * (opponents.length > 3 ? .085 : .11), width * (opponents.length > 3 ? .13 : .17));
            if (n > 0 && !(width < 600 && opponents.length > 3)) handPair(seat.x,seat.y,ch,false,p.connectionId,fanRotation,n,assembly);
            for (let i = 0; i < n; i++) {
                const m = cardMesh(); m.scale.set(ch * 360 / 520, ch, 1);
                m.position.set(0,0,i*.7);
                const offset = (i - (n - 1) / 2) * ch * .22;
                m.position.x += Math.cos(fanRotation) * offset;
                m.position.y += Math.sin(fanRotation) * offset - Math.abs(i - (n - 1) / 2) * 2;
                m.rotation.z = fanRotation + (i - (n - 1) / 2) * -.025;
                m.renderOrder = i; assembly.add(m);
            }
            assembly.rotation.set(-.12,(seat.x-.5)*2.8,seat.rotation*.12);
        });
        if (!lobby && width >= 600) handPair(.5,.86,Math.min(height*.19,width*.16),true);
    }
    function renderPile(animate: boolean) {
        pile.clear(); flights = [];
        if (!lastPlay) return;
        const seats = seatPositions(opponents.length, width < 600, width>=601 && height<=550);
        const from = opponents.findIndex(p => p.connectionId === lastPlay!.playerId);
        const origin = new THREE.Vector3(from >= 0 ? (seats[from].x-.5)*7 : 0, .7, from >= 0 ? -2 : 3);
        const n = lastPlay.cards.length;
        const ch = 1.25;
        const spread = Math.min(.5, 3 / Math.max(1, n - 1));
        lastPlay.cards.forEach((card, i) => {
            const face = cardMesh(card);
            const key = `${card.suit}${card.value}`;
            if (!physicalMaterials.has(key)) physicalMaterials.set(key, own(new THREE.MeshStandardMaterial({map:face.material.map, transparent:true, alphaTest:.5, roughness:.85, side:THREE.DoubleSide})));
            const m = new THREE.Mesh(cardGeometry, physicalMaterials.get(key)!); m.scale.set(ch * 360 / 520, ch, 1);
            m.castShadow = m.receiveShadow = true;
            const end = new THREE.Vector3((i - (n - 1) / 2) * spread, .045+i*.004, -.35);
            const rotation = -.065 + (i - (n - 1) / 2) * .025;
            m.position.copy(animate ? origin : end); m.rotation.set(-Math.PI/2,0,rotation); m.renderOrder = 30 + i;
            pile.add(m);
            if (animate) flights.push({ mesh: m, start: origin.clone(), end, time: performance.now() + i * 55, rotation, order: i });
        });
    }
    function resize() {
        discardLabelDirty=true;
        width = container.clientWidth; height = container.clientHeight || 1;
        const resolution=graphicsQuality==='low'?.85:graphicsQuality==='high'?1.75:width<600?1.25:1.75;
        renderer.setPixelRatio(Math.min(devicePixelRatio,resolution));
        renderer.shadowMap.enabled=graphicsQuality!=='low';
        renderer.domElement.dataset.quality=graphicsQuality;
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
    const localOffset = new THREE.Vector2();
    let handLayoutDirty = true, localHandVisible = true, layoutUntil = 0;
    const invalidateHandLayout = () => { handLayoutDirty = true; layoutUntil=performance.now()+250; };
    const host = container.parentElement!;
    const handObserver = new MutationObserver(invalidateHandLayout);
    // Card selection, clearing a hand and horizontal scrolling can change its visible bounds.
    const handArea = host.querySelector('.hand-area');
    if (handArea) handObserver.observe(handArea, { childList:true, subtree:true, attributes:true, attributeFilter:['class'] });
    host.addEventListener('scroll', invalidateHandLayout, true);
    const cardInteraction=(event:Event)=>{
        if((event.target as Element).closest('.hand-card'))invalidateHandLayout();
    };
    const cardEvents=['pointerover','pointerout','focusin','focusout'];
    cardEvents.forEach(name=>host.addEventListener(name,cardInteraction));
    window.addEventListener('resize', invalidateHandLayout);
    function updateHandAnchor() {
        handLayoutDirty = false;
        const viewport = host.querySelector('.hand-scroll')?.getBoundingClientRect();
        const bounds = container.getBoundingClientRect();
        const present = new Set<string>();
        let left = Infinity, right = -Infinity, bottom = -Infinity;
        if (viewport) host.querySelectorAll<HTMLButtonElement>('.hand-card').forEach((card,index) => {
            const r = card.getBoundingClientRect();
            if (r.right <= viewport.left || r.left >= viewport.right) return;
            const id=card.dataset.cardId!;
            present.add(id);
            let mesh=localMeshes.get(id);
            if (!mesh) {
                mesh=cardMesh({ id, value:Number(card.dataset.value) as Card['value'], suit:card.dataset.suit as Card['suit'] });
                localMeshes.set(id,mesh); localCards.add(mesh);
            }
            mesh.material=cardMaterial({id,value:Number(card.dataset.value) as Card['value'],suit:card.dataset.suit as Card['suit']},card.classList.contains('selected'));
            const matrix=new DOMMatrixReadOnly(getComputedStyle(card.parentElement!).transform);
            const angle=Math.atan2(matrix.b,matrix.a);
            // Local cards are the foreground layer: fingers must never paint over them.
            mesh.position.set((r.left+r.right)/2-bounds.left-width/2,height/2-((r.top+r.bottom)/2-bounds.top),(card.classList.contains('selected')?130:100)+index*.01);
            mesh.scale.set(card.offsetWidth,card.offsetHeight,1);
            mesh.rotation.z=-angle;
            mesh.renderOrder=card.classList.contains('selected')?1000+index:index;
            mesh.visible=!card.classList.contains('dragging-card');
            left = Math.min(left, Math.max(r.left, viewport.left));
            right = Math.max(right, Math.min(r.right, viewport.right));
            bottom = Math.max(bottom, r.bottom);
        });
        localHandVisible = Number.isFinite(left);
        localMeshes.forEach((mesh,id)=>{if(!present.has(id)){localCards.remove(mesh);localMeshes.delete(id);}});
        if(viewport) handClip.set(viewport.left-bounds.left,height-(viewport.bottom-bounds.top),viewport.width,viewport.height);
        host.classList.toggle('local-cards-3d',!!viewport);
        if (localHandVisible) {
            // Keep wrists beneath the lower edge of the actual, clipped HTML fan.
            localOffset.set((left+right)/2-bounds.left-width*.5, height*.86-(bottom-bounds.top-18));
        }
    }
    let sampledAt = performance.now(), sampledFrames = 0,lastRendered=0;
    function frame(now: number) {
        if (disposed || document.hidden) return;
        const frameInterval=1000/(graphicsQuality==='low'?30:60);
        if(now-lastRendered<frameInterval-1) {raf=requestAnimationFrame(frame);return;}
        lastRendered=now;
        if (handLayoutDirty || now<layoutUntil) updateHandAnchor();
        const mateProgress=reducedMotion?1:Math.min(1,(now-mateStarted)/650);
        environment.mate.rotation.y=mateFrom+(mateAngle-mateFrom)*(1-Math.pow(1-mateProgress,3));
        environment.mate.position.lerpVectors(mateFromPosition,mateTargetPosition,1-Math.pow(1-mateProgress,3));
        const mateGlow=environment.mate.userData.glow as THREE.Mesh;
        if(mateGlow) (mateGlow.material as THREE.MeshBasicMaterial).opacity=(!lobby && turnIndicator)? .68+Math.sin(now*.004)*.12 : 0;
        if (!lobby && !reducedMotion && now-enteredAt<1000) {
            const t=1-Math.pow(1-Math.min(1,(now-enteredAt)/900),3);
            camera.position.set(0,10.8+(5.4-10.8)*t,6.8+(8.3-6.8)*t);camera.lookAt(0,0,-.3);
        }
        if(discardLabelDirty || now-enteredAt<1000) {
            camera.updateMatrixWorld();
            const anchor=new THREE.Vector3(-1.5,.02,.38).project(camera);
            host.style.setProperty('--discard-left',`${(anchor.x+1)*width/2}px`);
            host.style.setProperty('--discard-top',`${(1-anchor.y)*height/2+7}px`);
            discardLabelDirty=false;
        }
        const firstFlight=flights[0];
        hands.traverse(model=>{
            if (model.userData.baseX === undefined) return;
            const isAuthor=model.userData.playerId===lastPlay?.playerId || (model.userData.local && !opponents.some(p=>p.connectionId===lastPlay?.playerId));
            const progress=firstFlight?Math.max(0,Math.min(1,(now-firstFlight.time)/540)):0;
            model.visible = !model.userData.local || localHandVisible;
            model.position.x=model.userData.baseX+(model.userData.local?localOffset.x:0);
            model.position.y=model.userData.baseY+(model.userData.local?localOffset.y:0)+(isAuthor?Math.sin(progress*Math.PI)*height*.012:0);
            for (const finger of (model.userData.fingers ?? []) as THREE.Bone[]) {
                finger.quaternion.copy(finger.userData.restQuaternion);
                if (!reducedMotion && isAuthor) finger.rotateOnAxis(finger.userData.curlAxis,Math.sin(progress*Math.PI)*.08);
            }
        });
        for (const f of flights) {
            const t = Math.max(0, Math.min(1, (now - f.time) / 540));
            const ease = 1 - Math.pow(1 - t, 3);
            f.mesh.position.lerpVectors(f.start, f.end, ease);
            f.mesh.position.y += Math.sin(t * Math.PI) * .9;
            f.mesh.rotation.z = f.rotation + Math.sin(t * Math.PI) * .15;
        }
        flights = flights.filter(f => now < f.time + 540);
        renderer.domElement.dataset.animating = String(flights.length > 0);
        renderer.info.reset();
        renderer.clear(); renderer.render(scene, camera); renderer.clearDepth(); renderer.render(overlay, screen);
        if(localMeshes.size) {
            renderer.setScissor(handClip); renderer.setScissorTest(true);
            renderer.clearDepth();
            renderer.render(localCards,screen); renderer.setScissorTest(false);
        }
        sampledFrames++;
        if (now-sampledAt >= 1000) {
            renderer.domElement.dataset.fps = String(Math.round(sampledFrames*1000/(now-sampledAt)));
            renderer.domElement.dataset.sampledAt=String(now);
            renderer.domElement.dataset.geometries = String(renderer.info.memory.geometries);
            renderer.domElement.dataset.textures = String(renderer.info.memory.textures);
            renderer.domElement.dataset.drawCalls = String(renderer.info.render.calls);
            renderer.domElement.dataset.triangles = String(renderer.info.render.triangles);
            renderer.domElement.dataset.pixelRatio = String(renderer.getPixelRatio());
            sampledFrames=0; sampledAt=now;
        }
        raf = requestAnimationFrame(frame);
    }
    const visibility = () => {
        cancelAnimationFrame(raf);
        if (!document.hidden && !disposed) { sampledAt=performance.now(); sampledFrames=0; raf=requestAnimationFrame(frame); }
    };
    document.addEventListener('visibilitychange', visibility);
    const graphicsChanged=(event:Event)=>{
        const next=(event as CustomEvent).detail;
        if(next!=='auto' && next!=='low' && next!=='high')return;
        graphicsQuality=next;resize();invalidateHandLayout();
        sampledAt=performance.now();sampledFrames=0;
    };
    window.addEventListener(GRAPHICS_EVENT,graphicsChanged);
    function updateMateTarget() {
        if (lobby) return;
        const index=opponents.findIndex(player=>player.isCurrentTurn);
        const seats=seatPositions(opponents.length,width<600,width>=601 && height<=550);
        const nextMate = turnIndicator && index >= 0 ? (() => {
            const seat=seats[index];
            if(seat.x<.35)return new THREE.Vector3(-3.8,0,-1.8);
            if(seat.x>.65)return new THREE.Vector3(3.8,0,-1.8);
            return new THREE.Vector3(0,0,-3.35);
        })() : new THREE.Vector3(3.75,0,1.7);
        if(mateTargetPosition.distanceToSquared(nextMate)>.0001) {
            mateFromPosition.copy(environment.mate.position);
            mateTargetPosition.copy(nextMate);
            mateStarted=performance.now();
        }
        if (!turnIndicator || index < 0) return;
        const seat=seats[index];
        const target=new THREE.Vector3((seat.x-.5)*10,0,-3);
        const direction=target.sub(environment.mate.position);
        const angle=Math.atan2(-direction.z,direction.x);
        const delta=Math.atan2(Math.sin(angle-mateAngle),Math.cos(angle-mateAngle));
        if(Math.abs(delta)>.001) {
            mateFrom=environment.mate.rotation.y;
            mateAngle=mateFrom+Math.atan2(Math.sin(angle-mateFrom),Math.cos(angle-mateFrom));
            mateStarted=performance.now();
        }
    }
    resize(); raf = requestAnimationFrame(frame);
    return {
        setDiscardCount(count) {
            discard.clear();
            // A bounded stack represents the public server count; geometry does
            // not grow with every card played during a long multi-deck match.
            const layers=Math.min(6,Math.max(0,count));
            for(let i=0;i<layers;i++) {
                const card=new THREE.Mesh(cardGeometry,discardMaterial);
                card.scale.set(.65,.94,1);
                card.position.set(-1.5+(i%2)*.014,.035+i*.022,-.35);
                card.rotation.set(-Math.PI/2,0,(i%3-1)*.025);
                card.castShadow=card.receiveShadow=true;discard.add(card);
            }
        },
        setOpponents(players) {
            opponents = players; renderOpponents(); updateMateTarget();
        },
        setTurnIndicator(active) { turnIndicator=active; updateMateTarget(); },
        setLastPlay(play, animate) { lastPlay = play; renderPile(animate && !reducedMotion); },
        dispose() { disposed = true; window.removeEventListener(GRAPHICS_EVENT,graphicsChanged); host.classList.remove('local-cards-3d'); cancelAnimationFrame(raf); handObserver.disconnect(); host.removeEventListener('scroll', invalidateHandLayout, true); cardEvents.forEach(name=>host.removeEventListener(name,cardInteraction)); window.removeEventListener('resize', invalidateHandLayout); document.removeEventListener('visibilitychange', visibility); observer.disconnect(); hands.traverse(node => { if (node instanceof THREE.SkinnedMesh) node.skeleton.dispose(); }); scene.traverse(node => { if (node instanceof THREE.InstancedMesh) node.dispose(); }); resources.forEach(r => r.dispose()); renderer.dispose(); renderer.domElement.remove(); }
    };
}
