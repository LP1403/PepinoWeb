# Ambiente de mesa — implementación web

Estado revisado: 10 de septiembre de 2026. Implementación estilizada de mesa argentina
con madera, paño verde e iluminación cálida; no fotorrealista.

## Actualización contra el código actual

- Mate: se desplaza según el turno y solo el jugador activo ve el aro verde.
  Implementado por zonas; resta validar anclajes individuales, colisiones y resize.
- Zoom 100–160% con restablecer; espectador al quedarse sin cartas. No hay órbita libre.
- Home/lobby/partida comparten Configuración con audio integrado y botón separado
  de pantalla completa. Logo y controles se posicionan independientemente.
- Caras personalizadas y dorsos por mazo integrados; la caché de selección se
  unificó, pero falta cubrir primer acceso resaltado con carga lenta.
- `Floor.glb` activo como prueba. ROOM importado desactivado; aún existe una pared
  procedural de respaldo. No considerar terminado el entorno ni validado su color.
- Audio: la persistencia de volumen existe; continuidad entre pantallas pendiente
  de prueba y ajuste del runtime. La prueba de visibilidad no cubre ese recorrido.

Ver pendientes y prioridades vigentes en [PLAN_MEJORAS_PARTIDA.md](PLAN_MEJORAS_PARTIDA.md).
La validación detallada de abajo es histórica; hoy se repitieron TypeScript y las
28 comprobaciones de reglas, no toda la matriz visual/multiplayer.

## Implementado

### Mesa y ambiente

- Mesa procedural, paño, costura y marca Pepino.
- Relieve sutil de madera y paño reutilizando mapas existentes.
- Lámparas, vasos, mate con abertura real, yerba y bombilla.
- Bowls con borde y fondo; maníes y hojas con instancing.
- Cámara elevada de lobby y transición de entrada al juego.
- Cartas centrales en el mundo 3D, con perspectiva, iluminación, sombras y vuelo.
- Pila de descarte acotada a seis capas y contador público desde el servidor;
  etiqueta proyectada desde la posición 3D para conservar el anclaje al redimensionar.
- Mate reubicado suavemente según el turno, con aro exclusivo para el jugador local activo.

### Manos y cartas locales

- Asset `pepino-hand-rigged.glb`: 18 huesos conservados desde el FBX, pose de agarre,
  textura a 1024px, aproximadamente 1.7 MB. Licencia en `public/models/ASSET_CREDITS.md`.
- Esqueleto independiente por mano, flexión de falanges y movimiento al jugar.
  Flexión revisada sobre un eje transversal común: los huesos importados tienen
  orientaciones locales diferentes. Dedos curvados en base, falange y punta.
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
  Sala y ronda viven junto a Configuración/Pantalla completa/Salir y el contador circular rival se eliminó porque
  ya figura bajo el nombre.
- Revisado con capturas de escritorio y mobile emulado; pruebas de drag y scroll
  con 2, 4 y 8 jugadores y compilación pasan. Aún faltan antebrazos y una pose de
  dedos más cerrada para aproximar el agarre y el realismo de la referencia.
- Anclaje local al borde inferior del abanico visible; actualización por scroll,
  selección y resize. Las manos se ocultan cuando no hay cartas.
- Cartas locales dibujadas como meshes WebGL ortográficos. Los botones HTML mantienen
  selección, foco, teclado y accesibilidad. Solo se representan cartas visibles,
  con recorte horizontal y texturas compartidas por valor/palo.
- Cartas locales renderizadas con profundidad independiente de las manos: ningún
  dedo puede ocultarlas. La selección queda delante del resto del abanico.
- Selección reforzada con un único borde verde petróleo, sin halo separado;
  las jugables conservan su borde sutil, por lo que ambas señales no se confunden.
- Bordes de jugadas posibles y foco de teclado visibles también con cartas WebGL.
- En mobile no se dibujan manos locales; con más de cuatro jugadores se omiten
  también las manos rivales para reducir decoración/costo.

### Controles y final de partida

- Scroll horizontal con navegación, rueda, mouse arrastrado y touch.
- Extremos de navegación deshabilitados, paso proporcional al ancho, flechas e
  Inicio/Fin por teclado; hover y foco sincronizados con las cartas WebGL.
- Vista horizontal baja: partida dentro de 100dvh, navegación inferior visible,
  acciones al costado de la mano y cantidades de cartas conservadas. Verificada
  en 844×390 y 1280×500, con 4 y 8 jugadores (navegador emulado).
  Con más de cuatro jugadores, el arco se comprime en altura para dejar los
  avatares debajo de la barra superior. No cambia el layout de escritorio normal.
- Render limitado a 60 FPS en automática/alta y 30 FPS en ahorro; la animación
  sigue usando tiempo transcurrido, sin modificar el ritmo del juego. El contador
  de FPS quedó fuera de la interfaz.
- Cantidad de cartas persistente bajo el nombre, estado de turno/pausa separado,
  pulso suave del avatar activo y color de aviso cuando quedan una o dos cartas.
- Drag vertical de carta/grupo, destino visible y envío directo de jugadas válidas. Las caras originales
  se ocultan durante el drag WebGL y el ghost muestra las cartas arrastradas.
- Drop inválido no envía jugada; una revisión nueva invalida el drop.
- Selección con teclado después de arrastrar corregida.
- Audio ambiental y controles independientes de música/efectos.
- Audio integrado dentro de Configuración en home, lobby y partida; la barra de
  partida incluye tuerca, pantalla completa y Salir. El modal mantiene mute,
  volúmenes, cierre afuera y Escape.
- Al ocultar la pestaña se suspenden el contexto y el temporizador de audio;
  el sonido de pepineado requiere un jugador efectivamente salteado.
- El audio se presenta como sección del modal de Configuración, sin popup anidado
  ni botón AUDIO separado. Ajustar el volumen conserva el modal abierto.
- Reactivar audio restaura los volúmenes anteriores; cue de victoria y síntesis
  reducida cuando los canales están silenciados.
- Compartir sala usa avisos integrados, tolera cancelación y errores del navegador.
- Fin de partida conserva la mesa y muestra ganadores y última jugada en un modal.
  Volver al lobby es explícito; desde allí se puede iniciar otra partida.

### Rendimiento

- Render suspendido al ocultar la pestaña.
- Límite de pixel ratio interno: 1.25 en viewport angosto, 1.75 en escritorio.
- Liberación de esqueletos, materiales, geometrías e instancias al cerrar la escena.
- Canvas: `data-fps`, `data-geometries`, `data-textures`, `data-draw-calls`,
  `data-triangles` y `data-pixel-ratio`. Los conteos de dibujo incluyen las capas.
- Estas métricas no equivalen a memoria total del navegador ni consumo de batería.
- Panel gráfico accesible durante la partida: automática, ahorro y alta. Ahorro
  usa pixel ratio máximo .85, desactiva sombras y limita a 30 FPS. La elección se
  guarda cuando storage lo permite. Pantalla completa tiene un botón independiente
  junto a la tuerca.
- Pantalla completa opcional mediante acción explícita del jugador, cuando el
  navegador admite la API; entrada/salida probadas en Edge headless.

## Validación realizada

- TypeScript y build de producción.
- Edge headless: 2, 4 y 8 jugadores; 1440×900 y 390×900; mano de 48 cartas.
- Drag con envío directo, navegación horizontal y swipe touch emulado.
- Selección de grupo con teclado, Escape, drop inválido, revisión nueva durante drag,
  arrastre horizontal con mouse, jugada directa que reduce la mano y resize.
- Integración con backend .NET/SignalR local: cuatro jugadores (un navegador y tres
  clientes automatizados), desconexión/reconexión conservando asiento, partida completa,
  ganadores/última jugada, retorno al lobby y nueva partida. Ejecutado a 1440 y 390 px.
  Pasaron recorridos de 47 y 50 acciones respectivamente, sin errores JavaScript.
- Capturas del final revisadas en ambos tamaños.

## Pendiente externo y opciones

- **Android/iOS físicos:** no disponibles en esta ejecución. Falta medir FPS/memoria,
  temperatura/batería y estabilidad sostenida, así como gestos en Safari iOS real.
  La emulación de viewport/touch no reemplaza esas pruebas.
- **Materiales importados:** `Floor.glb` en prueba junto con mesa procedural.
  Falta validar exportación, color bajo las luces del juego y peso de texturas.
- **Agarre de calidad final:** la integración y profundidad están implementadas, pero
  la naturalidad artística sigue siendo revisable; no se afirma un agarre físico exacto.
  Se descartó una prueba de mangas cilíndricas porque se veían como tubos cortados.
  Está autorizado cambiar pose, manos y antebrazos como conjunto para mejorar el
  resultado; no conservar el modelo actual a costa del agarre. Revisar siempre las
  posiciones aprobadas, cartas por delante en mano propia y controles sin invasión.

La priorización del próximo trabajo está en [PLAN_MEJORAS_PARTIDA.md](PLAN_MEJORAS_PARTIDA.md).

## Repetir verificaciones

Desde `Front/game-client`, con Vite en 5174:

```sh
node scripts/verify-visual.mjs
node scripts/verify-hand-interactions.mjs
node scripts/verify-game-polish.mjs
node scripts/verify-landscape.mjs
node scripts/verify-audio-lifecycle.mjs
```

Para la integración, iniciar además el backend en localhost:5264:

```sh
node scripts/verify-live-game.mjs
node scripts/verify-storage-fallback.mjs
node scripts/verify-disconnect-expiry.mjs
```

Para viewport angosto, definir `QA_WIDTH=390` al ejecutar la integración.
La prueba de expiración tarda al menos 60 segundos: usa la gracia real del servidor.
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
