import * as THREE from 'three';

/** Original procedural assets: no third-party model or texture downloads. */
export function buildTableEnvironment(scene: THREE.Scene, own: <T extends THREE.BufferGeometry | THREE.Material | THREE.Texture>(resource: T) => T) {
    let seed = 47;
    const random = () => { seed = (1664525 * seed + 1013904223) >>> 0; return seed / 4294967296; };
    function texture(felt: boolean) {
        const canvas = document.createElement('canvas'); canvas.width = canvas.height = 512;
        const ctx = canvas.getContext('2d')!;
        ctx.fillStyle = felt ? '#244638' : '#69452d'; ctx.fillRect(0, 0, 512, 512);
        for (let i = 0; i < (felt ? 32000 : 4200); i++) {
            ctx.fillStyle = random() > .5 ? `rgba(240,218,166,${felt ? .08 : .035})` : `rgba(10,8,5,${felt ? .12 : .1})`;
            const x = random() * 512, y = random() * 512;
            ctx.fillRect(x, y, felt ? 1 : 10 + random() * 100, felt ? 1 : .5 + random());
        }
        const tex = own(new THREE.CanvasTexture(canvas)); tex.colorSpace = THREE.SRGBColorSpace;
        tex.wrapS = tex.wrapT = THREE.RepeatWrapping; tex.repeat.set(felt ? 4 : 2, felt ? 4 : 2);
        return tex;
    }
    const wood = own(new THREE.MeshStandardMaterial({ map: texture(false), roughness: .64 }));
    const felt = own(new THREE.MeshStandardMaterial({ map: texture(true), roughness: 1 }));
    const brass = own(new THREE.MeshStandardMaterial({ color: 0xa28b54, metalness: .65, roughness: .4 }));
    const dark = own(new THREE.MeshStandardMaterial({ color: 0x201912, roughness: .65 }));
    function add(g: THREE.BufferGeometry, m: THREE.Material, x: number, y: number, z: number) {
        const mesh = new THREE.Mesh(own(g), m); mesh.position.set(x,y,z); mesh.castShadow = mesh.receiveShadow = true; scene.add(mesh); return mesh;
    }
    const table = add(new THREE.CylinderGeometry(5.5, 5.45, .32, 64), wood, 0, -.19, 0); table.scale.z = .8;
    const cloth = add(new THREE.CylinderGeometry(4.55, 4.55, .025, 8), felt, 0, -.015, 0); cloth.scale.z = .81; cloth.rotation.y = Math.PI / 8;
    const seam = add(new THREE.TorusGeometry(4.45, .014, 5, 8), brass, 0, .008, 0); seam.rotation.x = -Math.PI / 2; seam.rotation.z = Math.PI / 8; seam.scale.y = .81;
    const logo = document.createElement('canvas'); logo.width = 1024; logo.height = 256;
    const c = logo.getContext('2d')!; c.textAlign = 'center'; c.fillStyle = '#b9b084'; c.font = '700 115px Georgia'; c.fillText('PEPINO',512,138); c.font = '24px Arial'; c.fillText('ENTRE AMIGOS · UNA MÁS Y NOS VAMOS',512,204);
    const logoTex = own(new THREE.CanvasTexture(logo)); logoTex.colorSpace = THREE.SRGBColorSpace;
    const mark = add(new THREE.PlaneGeometry(2.5,.625), own(new THREE.MeshBasicMaterial({map:logoTex,transparent:true,opacity:.22,depthWrite:false})),0,.018,-.8); mark.rotation.x = -Math.PI/2;
    add(new THREE.BoxGeometry(100,.2,100), dark, 0,-2.3,0);
    add(new THREE.BoxGeometry(25,9,.3), own(new THREE.MeshStandardMaterial({color:0x28201b,roughness:1})),0,1,-8);
    for (const x of [-6,6]) {
        add(new THREE.CylinderGeometry(.7,.9,.12,32),wood,x,-.5,-5.6);
        add(new THREE.CylinderGeometry(.045,.08,1.8,12),brass,x,.4,-5.6);
        add(new THREE.CylinderGeometry(.35,.65,.75,32,1,true),own(new THREE.MeshStandardMaterial({color:0xd8b076,emissive:0xffad54,emissiveIntensity:.4,side:THREE.DoubleSide,roughness:1})),x,1.5,-5.6);
        const lamp = new THREE.PointLight(0xffb466,15,12,2); lamp.position.set(x,1.4,-5.5); scene.add(lamp);
    }
    // Mate, yerba and bombilla, kept outside the playable felt.
    const mate = add(new THREE.SphereGeometry(.32,24,16),wood,3.75,.29,1.7); mate.scale.y = 1.1;
    const mouth = add(new THREE.TorusGeometry(.24,.035,8,32),brass,3.75,.55,1.7); mouth.rotation.x=-Math.PI/2;
    const yerba=add(new THREE.CircleGeometry(.235,24),own(new THREE.MeshStandardMaterial({color:0x55502a,roughness:1})),3.75,.55,1.7); yerba.rotation.x=-Math.PI/2;
    const straw=add(new THREE.CylinderGeometry(.025,.025,.8,10),brass,3.84,.87,1.7); straw.rotation.z=-.22;
    for (const [x,z] of [[-3.9,1.7],[3.8,-1.8],[-3.8,-1.8]]) {
        add(new THREE.CylinderGeometry(.26,.28,.045,24),dark,x,.025,z);
        add(new THREE.CylinderGeometry(.22,.19,.58,24,1,true),own(new THREE.MeshStandardMaterial({color:0xb6a48d,transparent:true,opacity:.3,roughness:.16,side:THREE.DoubleSide,depthWrite:false})),x,.34,z);
        add(new THREE.CylinderGeometry(.199,.18,.28,24),own(new THREE.MeshStandardMaterial({color:0x28160c,roughness:.23})),x,.2,z);
    }
    for (const [x,z] of [[-3,2.7],[3,-2.8]]) {
        add(new THREE.CylinderGeometry(.4,.25,.12,24,1,true),dark,x,.07,z);
        const snack=own(new THREE.MeshStandardMaterial({color:0xb78c4c,roughness:.95}));
        for(let i=0;i<18;i++) { const angle=random()*Math.PI*2,r=random()*.3; const nut=add(new THREE.SphereGeometry(.055,8,6),snack,x+Math.cos(angle)*r,.12+random()*.05,z+Math.sin(angle)*r);nut.scale.z=1.7;nut.rotation.y=angle; }
    }
}
