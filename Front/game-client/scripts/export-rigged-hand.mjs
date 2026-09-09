import { readFileSync, writeFileSync } from 'node:fs';
import { chromium } from '@playwright/test';

// Run from Front/game-client with Vite running on port 5174.
const source = '../../UnityProject/PepinoUnity3D/Assets/FirstPersonHands/MaleHands/';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5174');
    const output = await page.evaluate(async ({ fbx, texture }) => {
        const THREE = await import('/node_modules/three/build/three.module.js');
        const { FBXLoader } = await import('/node_modules/three/examples/jsm/loaders/FBXLoader.js');
        const { GLTFExporter } = await import('/node_modules/three/examples/jsm/exporters/GLTFExporter.js');
        const bytes = Uint8Array.from(atob(fbx), c => c.charCodeAt(0));
        const model = new FBXLoader().parse(bytes.buffer, '');
        const mixer = new THREE.AnimationMixer(model);
        mixer.clipAction(model.animations[0]).play();
        mixer.setTime(625 / 30);
        const map = await new THREE.TextureLoader().loadAsync('data:image/png;base64,' + texture);
        map.colorSpace = THREE.SRGBColorSpace;
        model.traverse(node => {
            if (node.isMesh) node.material = new THREE.MeshStandardMaterial({ map, roughness: .8 });
        });
        model.children.filter(n => n.isLight).forEach(n => model.remove(n));
        model.updateMatrixWorld(true);
        const box = new THREE.Box3().setFromObject(model, true);
        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        const scale = .8870635 / Math.max(size.x, size.y, size.z);
        const root = new THREE.Group();
        root.add(model);
        model.position.sub(center);
        root.scale.setScalar(scale);
        const result = await new GLTFExporter().parseAsync(root, { binary: true, maxTextureSize: 1024 });
        return Array.from(new Uint8Array(result));
    }, {
        fbx: readFileSync(source + 'MaleHand.FBX').toString('base64'),
        texture: readFileSync(source + 'firstPersonHand_textures/HandArmDiff.png').toString('base64'),
    });
    writeFileSync('public/models/pepino-hand-rigged.glb', Buffer.from(output));
    console.log(`Exported rigged hand: ${output.length} bytes`);
} finally { await browser.close(); }
