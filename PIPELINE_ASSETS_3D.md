# Pipeline de assets 3D para Pepino

## Objetivo

Pepino puede incorporar modelos, diseños y animaciones 3D creados internamente
con Blender, Unreal Engine u otras herramientas. Los assets se integran en el
cliente web sin modificar la lógica del juego ni el backend.

La ruta recomendada para la versión web es:

```text
Blender → GLB optimizado → Three.js
```

Si el cliente principal pasa a Unity, el mismo trabajo puede reutilizarse con:

```text
Blender → FBX o GLB → Unity
```

## Formato recomendado para la web

El formato preferido es `.glb` o `.gltf` porque permite conservar en un mismo
asset el modelo, los materiales, las texturas, los esqueletos y las animaciones.

Los archivos del cliente web deben ubicarse en:

```text
Front/game-client/public/models/
```

Después se cargan desde Three.js utilizando `GLTFLoader`:

```ts
new GLTFLoader().load('/models/mi-modelo.glb', gltf => {
    scene.add(gltf.scene);
});
```

## Flujo de trabajo

1. Crear el modelo, las texturas, el rig y las animaciones.
2. Preparar las UVs y los materiales PBR.
3. Aplicar transformaciones y revisar la escala del objeto.
4. Exportar como `.glb`/`.gltf` para la web o `.fbx`/`.glb` para Unity.
5. Colocar el asset en `public/models/`.
6. Cargarlo con `GLTFLoader` y ubicarlo en la escena.
7. Ajustar escala, rotación, posición, iluminación y sombras.
8. Probarlo en escritorio y mobile.
9. Optimizar peso, cantidad de polígonos, texturas y cantidad de objetos.

## Blender

Blender es la opción más conveniente para este proyecto porque permite controlar
en un solo lugar:

- Modelado y topología.
- UVs y texturas.
- Materiales PBR.
- Rigging y esqueletos.
- Animaciones.
- Optimización del modelo final.

Antes de exportar conviene aplicar escala y rotación, comprobar que las normales
sean correctas y confirmar que el origen del objeto esté en una posición útil
para animarlo o colocarlo en la mesa.

## Unreal Engine

Unreal también puede utilizarse para modelar, preparar materiales o producir
animaciones. El proyecto de Unreal no se incorpora directamente al navegador.
Hay que exportar sus assets a un formato compatible, normalmente `.fbx` o `.glb`,
y adaptar los materiales, luces y lógica al cliente web.

La escena, los materiales complejos, las luces y los Blueprints de Unreal no se
trasladan automáticamente a Three.js. El modelo y las animaciones sí pueden
reutilizarse, con los ajustes necesarios.

## Assets posibles para Pepino

- Manos, brazos y antebrazos de los jugadores.
- Cartas, dorsos y mazos.
- Mate, bombilla, vasos y bowls.
- Maníes y otros elementos decorativos.
- Jugadores alrededor de la mesa.
- Sillas, lámparas y objetos del ambiente.
- Animaciones de repartir, jugar, pepinear y tomar mate.

## Recomendaciones de rendimiento

Los modelos deben estar preparados para ejecutarse en navegador, especialmente
en celulares. Conviene:

- Mantener una cantidad razonable de polígonos.
- Reutilizar materiales y texturas cuando sea posible.
- Reducir la resolución de las texturas que no necesiten mucho detalle.
- Usar atlas de texturas cuando varios objetos compartan materiales.
- Exportar únicamente las animaciones necesarias.
- Evitar luces y sombras dinámicas innecesarias.
- Comprimir geometría y texturas antes de publicar.
- Probar el resultado en un celular real, además del emulador.

La calidad visual debe equilibrarse con los FPS, el tiempo de carga y el consumo
de memoria. Un modelo muy detallado puede verse bien en PC y causar problemas en
mobile.

## Integración con el código actual

Los assets pueden reemplazarse progresivamente. El modelo nuevo debe conservar
una posición, escala y orientación conocidas para que la escena pueda seguir
controlándolo. Las reglas, las salas, SignalR y el backend no dependen del asset
visual.

Para un modelo animado, la integración normalmente incluye:

- Cargar el archivo GLB.
- Buscar el objeto o esqueleto correspondiente.
- Crear un `AnimationMixer`.
- Reproducir la animación según una acción del juego.
- Liberar geometrías, materiales, texturas y mixers al desmontar la escena.

## Compatibilidad futura con Unity

Los modelos creados en Blender pueden reutilizarse en Unity. Según el caso se
puede importar `.fbx` o `.glb`, revisar la escala, configurar materiales y
reconectar las animaciones dentro del Animator de Unity.

Conviene conservar el archivo fuente editable de Blender (`.blend`) junto con
las texturas y una nota sobre la licencia. El `.glb` publicado es el resultado
optimizado para la web, mientras que el archivo fuente permite seguir editando
el asset.

## Licencias y créditos

Todo asset externo debe tener una licencia compatible con el proyecto. Para cada
modelo conviene registrar:

- Autor o fuente.
- Licencia.
- URL de origen, si corresponde.
- Modificaciones realizadas.
- Fecha de incorporación.

Los assets creados por nosotros deberían conservarse como propiedad del proyecto
Pepino/RayenCo, junto con sus archivos fuente y versiones exportadas.
