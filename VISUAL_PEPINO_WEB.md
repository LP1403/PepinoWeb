# Ambiente de mesa — implementación web

Estado revisado: 9 de septiembre de 2026. Implementación estilizada de mesa argentina
con madera, paño verde e iluminación cálida; no fotorrealista.

## Implementado

### Mesa y ambiente

- Mesa procedural, paño, costura y marca Pepino.
- Relieve sutil de madera y paño reutilizando mapas existentes.
- Lámparas, vasos, mate con abertura real, yerba y bombilla.
- Bowls con borde y fondo; maníes y hojas con instancing.
- Cámara elevada de lobby y transición de entrada al juego.
- Cartas centrales en el mundo 3D, con perspectiva, iluminación, sombras y vuelo.

### Manos y cartas locales

- Asset `pepino-hand-rigged.glb`: 18 huesos conservados desde el FBX, pose de agarre,
  textura a 1024px, aproximadamente 1.7 MB. Licencia en `public/models/ASSET_CREDITS.md`.
- Esqueleto independiente por mano, flexión de falanges y movimiento al jugar.
- Orientación por asiento. Quitadas las mangas de los oponentes: se veían como
  bloques negros. Manos rivales ampliadas de escala 0.8 a 1.35 respecto de la carta.
- Agarre rival en las esquinas inferiores; rotación ZXY corregida para orientar
  las muñecas hacia abajo/afuera y los dedos hacia arriba/centro (punta hacia arriba).
  Profundidad del agarre ajustada al espesor completo del abanico para que la mano
  derecha no quede escondida por las últimas cartas. Verificado en capturas con
  2, 4 y 8 jugadores; falta cerrar más los dedos para lograr una pose natural.
- Cartas y manos comparten un grupo por asiento, con giro en profundidad para
  los laterales. La palma queda detrás y los dedos visibles en el borde inferior.
  No hay simulación de colisiones ni IK para cada carta individual.
- En escritorio se conservó la plantilla original de posiciones de los rivales;
  Sala y ronda viven junto a Audio/Salir y el contador circular se eliminó porque
  ya figura bajo el nombre.
- Revisado con capturas de escritorio y mobile emulado; pruebas de drag y scroll
  con 2, 4 y 8 jugadores y compilación pasan. Aún faltan antebrazos y una pose de
  dedos más cerrada para aproximar el agarre y el realismo de la referencia.
- Anclaje local al borde inferior del abanico visible; actualización por scroll,
  selección y resize. Las manos se ocultan cuando no hay cartas.
- Cartas locales dibujadas como meshes WebGL ortográficos. Los botones HTML mantienen
  selección, foco, teclado y accesibilidad. Solo se representan cartas visibles,
  con recorte horizontal y texturas compartidas por valor/palo.
- Profundidad compartida con las manos; selección en primer plano.
- En mobile no se dibujan manos locales; con más de cuatro jugadores se omiten
  también las manos rivales para reducir decoración/costo.

### Controles y final de partida

- Scroll horizontal con navegación, rueda, mouse arrastrado y touch.
- Drag vertical de carta/grupo, destino visible y confirmación. Las caras originales
  se ocultan durante el drag WebGL y el ghost muestra las cartas arrastradas.
- Drop inválido no envía jugada; una revisión nueva invalida el drop.
- Selección con teclado después de arrastrar corregida.
- Audio ambiental y controles independientes de música/efectos.
- Fin de partida conserva la mesa y muestra ganadores y última jugada en un modal.
  Volver al lobby es explícito; desde allí se puede iniciar otra partida.

### Rendimiento

- Render suspendido al ocultar la pestaña.
- Límite de pixel ratio interno: 1.25 en viewport angosto, 1.75 en escritorio.
- Liberación de esqueletos, materiales, geometrías e instancias al cerrar la escena.
- Canvas: `data-fps`, `data-geometries`, `data-textures`, `data-draw-calls`,
  `data-triangles` y `data-pixel-ratio`. Los conteos de dibujo incluyen las capas.
- Estas métricas no equivalen a memoria total del navegador ni consumo de batería.

## Validación realizada

- TypeScript y build de producción.
- Edge headless: 2, 4 y 8 jugadores; 1440×900 y 390×900; mano de 48 cartas.
- Drag hasta confirmación, cancelación, navegación horizontal y swipe touch emulado.
- Selección de grupo con teclado, Escape, drop inválido, revisión nueva durante drag,
  arrastre horizontal con mouse, confirmación que reduce la mano y resize.
- Integración con backend .NET/SignalR local: cuatro jugadores (un navegador y tres
  clientes automatizados), desconexión/reconexión conservando asiento, partida completa,
  ganadores/última jugada, retorno al lobby y nueva partida. Ejecutado a 1440 y 390 px.
  Pasaron recorridos de 47 y 50 acciones respectivamente, sin errores JavaScript.
- Capturas del final revisadas en ambos tamaños.

## Pendiente externo y opciones

- **Android/iOS físicos:** no disponibles en esta ejecución. Falta medir FPS/memoria,
  temperatura/batería y estabilidad sostenida, así como gestos en Safari iOS real.
  La emulación de viewport/touch no reemplaza esas pruebas.
- **Materiales escaneados (opcional):** no incorporados. El detalle se implementó con
  materiales procedurales. Requiere selección de assets, licencia y evaluación de peso.
- **Agarre de calidad final:** la integración y profundidad están implementadas, pero
  la naturalidad artística sigue siendo revisable; no se afirma un agarre físico exacto.

## Repetir verificaciones

Desde `Front/game-client`, con Vite en 5174:

```sh
node scripts/verify-visual.mjs
node scripts/verify-hand-interactions.mjs
```

Para la integración, iniciar además el backend en localhost:5264:

```sh
node scripts/verify-live-game.mjs
```

Para viewport angosto, definir `QA_WIDTH=390` al ejecutar la integración.
Las suites usan Edge instalado. Las capturas y métricas visuales quedan en
`pepino-visual-qa` dentro del directorio temporal; los finales en
`pepino-live-final-1440.png` y `pepino-live-final-390.png` del mismo directorio temporal.

Exportación reproducible del asset (requiere Vite):

```sh
node scripts/export-rigged-hand.mjs
```

Demos: `http://localhost:5174/?demo=1`, `?demo=1&players=2`,
`?demo=1&players=8&hand=48`. No validan el protocolo del backend por sí solas.

## Protocolo en teléfonos reales

En Android Chrome y Safari iOS: jugar diez minutos con 2, 4 y 8 jugadores; probar
48 cartas, drag, scroll, orientación, segundo plano y reconexión. Registrar dispositivo,
navegador, viewport, FPS tras calentamiento y memoria cuando las herramientas lo
permitan. Comparar geometrías/texturas al entrar y salir repetidamente de salas.
Anotar temperatura y consumo observados. No extrapolar resultados de headless.
