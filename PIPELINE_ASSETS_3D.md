# Pipeline de assets y escenarios 3D para Pepino web (Three.js)

## Objetivo

Pepino puede incorporar modelos, diseños y animaciones 3D creados internamente
con Blender, Unreal Engine u otras herramientas. Los assets se integran en el
cliente web sin modificar la lógica del juego ni el backend.

Este documento se enfoca en el cliente web y su escena Three.js. La ruta recomendada es:

```text
Blender → GLB optimizado → Three.js
```

## Escenarios completos desde Blender

Se puede diseñar en Blender todo el escenario: habitación, mesa, sillas,
lámparas, decoración, mate, vasos, brazos y manos. Three.js puede cargar el
conjunto desde un GLB, conservando la jerarquía y las transformaciones exportadas.
Un único archivo no implica una única malla: los objetos que el juego necesita
controlar deben seguir separados y tener nombres estables.

Organizar la escena de origen en estos grupos:

- **Entorno:** mesa, sillas, habitación y decoración fija.
- **Objetos interactivos:** mate, vasos y mazos independientes, con pivotes útiles.
- **Jugadores:** brazos y manos con rig y animaciones cuando deban moverse.
- **Referencias:** objetos vacíos (`Empty`) que indiquen posiciones de asientos,
  agarre de cartas, mate y zona de jugadas. Incluirlos en la exportación.
- **Cámara:** una cámara de perspectiva de referencia para encuadrar el escenario.
  Three.js debe ajustar el encuadre al tamaño de pantalla y controlar el zoom.

Por ejemplo, cada asiento puede tener referencias llamadas `Seat_01`,
`Seat_01_Cards` y `Seat_01_Mate`. La referencia del mate debe estar a la derecha
del jugador sentado allí, considerando su orientación hacia la mesa.
El código busca estos objetos por nombre y utiliza sus posiciones y orientaciones
para ubicar los elementos dinámicos.

Las cartas de la partida se generan o actualizan desde el estado del juego:
el modelo de Blender aporta el soporte visual y los puntos de agarre, pero no
una mano fija que sustituya las cartas repartidas. También hay que contemplar
la cantidad variable de jugadores y cartas, y ocultar los asientos desocupados
cuando corresponda.

Se puede empezar con un GLB completo. Si luego conviene reutilizar manos o cargar
elementos por separado, exportar el entorno, los objetos y los jugadores en GLB
independientes, manteniendo una escala y referencias comunes.

### Qué requiere adaptación

La importación no reproduce automáticamente un render de Blender. Los materiales
deben ser compatibles con glTF; los materiales procedurales pueden necesitar
hornearse en texturas. La iluminación, sombras y efectos se ajustan en Three.js.
Las animaciones deben exportarse como clips compatibles; los controles del rig
o simulaciones de Blender pueden requerir horneado de su movimiento.

El código sigue conectando los turnos con el movimiento del mate, el reparto,
las animaciones y las interacciones. Importar el escenario no agrega esa lógica
automáticamente ni cambia las reglas o el backend.

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
4. Exportar como `.glb`/`.gltf` para Three.js, incluyendo objetos de referencia y clips necesarios.
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

También puede reemplazarse el escenario completo. En ese caso hay que adaptar
el posicionamiento de cartas, manos y etiquetas a las referencias del modelo,
y revisar las capas de interfaz que actualmente se colocan en coordenadas de
pantalla. Cargar el GLB es el primer paso; conectar esos elementos y validar el
encuadre en PC y mobile forma parte de la integración.

Para un modelo animado, la integración normalmente incluye:

- Cargar el archivo GLB.
- Buscar el objeto o esqueleto correspondiente.
- Crear un `AnimationMixer`.
- Reproducir la animación según una acción del juego.
- Liberar geometrías, materiales, texturas y mixers al desmontar la escena.

## Archivos fuente y exportaciones

Conviene conservar el archivo fuente editable de Blender (`.blend`) junto con
las texturas y una nota sobre la licencia. El `.glb` publicado es el resultado
optimizado para la web, mientras que el archivo fuente permite seguir editando
el asset.

Organización propuesta para las exportaciones (crear las carpetas al incorporar assets):

```text
Front/game-client/public/models/
  environments/   # Escenarios completos o entorno fijo
  props/          # Mate, vasos, bowls y otros objetos reutilizables
  characters/     # Manos y brazos con sus rigs y animaciones
```

Mantener los `.blend` y las texturas de trabajo fuera de `public/`, separados
de las exportaciones optimizadas que descarga el navegador.

## Referencias técnicas

- [Exportación glTF/GLB desde Blender](https://docs.blender.org/manual/en/latest/addons/import_export/scene_gltf2.html).
- [GLTFLoader de Three.js](https://threejs.org/docs/#examples/en/loaders/GLTFLoader).
- [AnimationMixer de Three.js](https://threejs.org/docs/#api/en/animation/AnimationMixer).

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
