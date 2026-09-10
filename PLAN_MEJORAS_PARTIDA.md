# Plan de mejoras de Pepino

Las ideas se toman como hipótesis de diseño: primero se valida si mejoran la lectura y el ritmo de la partida, y después se decide cuánto invertir en cada una.

## Revisión vigente — 10 de septiembre de 2026

Alcance: cliente web React/Three.js y backend compartido. Revisión contra el
código actual; no se asume que un cambio conversado esté terminado ni que una
prueba antigua cubra modificaciones posteriores. Los bloques originales se
conservan como catálogo de ideas, no como lista de pendientes.

### Ya implementado: mantener y validar, no desarrollar de nuevo

- **Mate y turno:** `pepinoScene.ts::updateMateTarget` cambia la posición del mate
  según el rival activo o lo devuelve al lugar local. La interpolación suaviza el
  movimiento. El aro verde solo se muestra con `yourTurn`, sin pausa/desconexión.
  Yerba y bombilla ya existen. No es solamente rotar la bombilla.
- **Zoom y espectador:** botones de acercar, alejar y restablecer entre 100–160%.
  Al ganar, la clase `spectating` oculta la mano y permite seguir mirando con zoom.
  Esto no incluye cámara libre, órbita ni ingreso externo como espectador.
- **Configuración compartida:** `LobbyControls` reutiliza `GameGraphicsControls`;
  audio dentro del modal y pantalla completa independiente en home/lobby/partida.
- **Jugada directa:** JUGAR y drop válido envían sin confirmación adicional.
  Scroll horizontal, drag vertical, teclado y flechas ya están implementados.
- **Mesa y resultado:** última combinación, descarte por mazo, separación de
  jugadas largas y resultado con última jugada antes de volver al lobby.
- **Lectura:** avatar activo, estado pensando/enviando/pausado/reconectando y
  color de pocas cartas. Falta la dirección de ronda, no todo el indicador de turno.
- **Assets:** caras personalizadas y contracartas por mazo integradas. `Floor.glb`
  se carga para probar materiales. `Structure-room.glb` ya no se solicita.
- **Lobby:** etiquetas de jugadores proyectadas desde posiciones de mesa;
  logo y controles tienen posicionamiento independiente.

### Pendientes concretos encontrados en esta revisión

1. **P1 · Continuidad de audio (S–M).** El motor todavía se destruye al desmontar
   cada `runtimeOnly` entre pantallas. La prueba de audio actual cubre visibilidad
   en una partida, no home → lobby → partida ni cierre del modal. Verificar ese
   recorrido y asegurar continuidad antes de darlo por cerrado.
2. **P1 · Selección de assets (S).** Se unificó la textura seleccionada y normal,
   pero `cardMaterial` aún evita cargar el PNG si el primer acceso es resaltado.
   Probar selección inmediata con carga lenta, deselección y las tres caras
   personalizadas. La comprobación anterior de demo no encontró un 3 de Oro;
   no cuenta como regresión específica aprobada.
3. **P1 · Mate por asiento (S–M).** La reubicación funciona por tres zonas de
   rivales (izquierda/centro/derecha), no por anclaje individual para 2–8 jugadores.
   Validar colisiones con vasos/manos y perspectiva de cada cliente. Al cambiar
   de tamaño, `resize` no recalcula directamente el destino del mate. La rama
   de rotación exige turno local y rival activo a la vez; revisar su intención.
4. **P1 · UI en tamaños chicos (S).** Comprobar logo, controles y panel juntos,
   incluida apertura/cierre del modal y fullscreen; las pruebas de conteo de
   botones no demuestran que la posición sea correcta.
5. **P2 · Piso de prueba (S).** Queda una pared procedural (`fallbackWall`) aunque
   se retiró el ROOM importado. Revisar el resultado aislado de `Floor.glb` antes
   de afirmar fidelidad de color; las luces cálidas y tone mapping afectan el aspecto.
6. **P2 · Agarre final (L).** Manos y perspectiva siguen parciales. Preparar el
   conjunto manos/antebrazos y anclajes en Blender según el pipeline web.

### Orden para retomar

Primero cerrar selección/audio y validar mate/UI en los recorridos anteriores.
Después agregar dirección de ronda si aporta lectura, usando el estado del
servidor; luego continuar assets y agarres. Mantener reacciones, interacciones
decorativas y personalización para después. No cambiar reglas ni volver a
agregar confirmaciones. No crear un indicador porcentual de pensamiento.

### Verificación de esta revisión

TypeScript pasó. Las 28 comprobaciones de `GameServer.RuleTests` pasaron con
`dotnet run --project Back/GameServer/GameServer.RuleTests --no-restore`, incluido
12×1 sobre 12×1, conservación de mesa y rechazo de una carta inferior.
No se repitieron aquí las suites de navegador/integración ni una partida en
teléfonos físicos. Los resultados históricos de abajo se conservan como antecedentes.

## Antecedentes del pulido — 9 de septiembre de 2026

### Avance del pulido continuo

- Cantidad de cartas visible durante turno, pausa y reconexión; señal discreta de
  una/dos cartas y pulso del avatar activo con movimiento reducido respetado.
- Flechas deshabilitadas en extremos, desplazamiento proporcional al viewport,
  navegación por flechas/Inicio/Fin y sincronización de hover/foco con las cartas 3D.
- Audio: restaurar volúmenes después de silenciar, señal de victoria y menor trabajo
  de síntesis cuando los volúmenes están en cero.
- Compartir sala con aviso integrado y cancelación controlada; códigos largos no
  desplazan los controles fuera de pantalla. Entrada tolerante a storage bloqueado.
- Mate reubicado suavemente según el turno. Pila de descarte limitada a seis capas
  visuales y cantidad de cartas jugadas tomadas de `state.tableCards` del servidor.
- Configuración: automática, ahorro (resolución reducida, sin sombras) y alta;
  persistencia y cambio en vivo. Automática/alta limitadas a 60 FPS y ahorro a 30.
  El contador de FPS se quitó de la interfaz. Pantalla completa tiene un botón
  independiente junto a la tuerca cuando el navegador la admite.
- Pantallas horizontales bajas: navegación dentro del viewport, cantidades visibles
  y acciones fuera de la última jugada; comprobado con 4 y 8 jugadores.
- Prueba específica de 12×1 sobre 12×1: confirma salto, mesa conservada y rechazo
  de una carta inferior. Pasaron 28 comprobaciones de reglas, sin cambiar las reglas.

Verificaciones añadidas: `verify-game-polish.mjs`, `verify-storage-fallback.mjs`, `verify-landscape.mjs`,
y `QA_QUALITY=low` para `verify-hand-interactions.mjs`. Sigue pendiente la prueba
sostenida en teléfonos físicos. La partida local automatizada de este lote pasó
48 acciones, reconexión, final y nueva partida; no reproduce por sí sola el reporte
histórico de una partida particular.

La revisión vigente del 10 de septiembre tiene prioridad sobre estos bloques. El
objetivo es que Pepino se sienta como una partida en una mesa: lectura inmediata,
respuesta clara a las acciones y decoración que acompañe el juego.

### Tramo recomendado anterior (consultar el orden vigente arriba)

1. **Lectura del turno y del pepineado (M).** Mostrar al jugador activo, el siguiente
   elegible y una transición breve cuando alguien es salteado. Derivar todo del
   estado del servidor; no asumir que el siguiente asiento siempre juega. Mantener
   visible la cantidad de cartas incluso durante su turno. Evitar indicadores de
   carga porcentual: no sabemos cuánto va a tardar una persona en decidir.
2. **Confiabilidad del caso reportado (S–M).** Ya hay pruebas de igualdad con cuatros
   y el escenario específico 12×1. Falta una reproducción dirigida de eventos/estado
   en cliente; no afirmar que se reprodujo el reporte original.
   La expiración completa de la gracia de desconexión queda cubierta por una prueba
   de integración separada; tarda dos ventanas reales de 60 segundos.
3. **Calidad gráfica y diagnóstico discreto (M).** Presets ahorro/automática/alta
   implementados con persistencia. Evaluar en teléfonos físicos
   antes de aumentar luces, sombras o cantidad de modelos.
4. **Assets de agarre final (L).** Preparar manos con antebrazo y una pose diseñada
   para sostener un abanico. El rig actual permite mejorar dedos, pero agregar
   cilindros como mangas no resolvió la presencia del jugador. Conservar los
   asientos aprobados y la legibilidad de las cartas locales. Ver
   [pipeline de assets](PIPELINE_ASSETS_3D.md).

### Hacer después, con alcance acotado

- **Aviso de pocas cartas (S):** probar un aviso al cruzar el umbral de dos cartas,
  una vez por jugador y ronda. Es una propuesta de experiencia, no una regla nueva.
- **Mate por turno:** desplazamiento y resalte local implementados; pendiente
  validar anclajes por asiento y colisiones, no agregar el resalte nuevamente.
- **Descarte acumulado:** pila y contador implementados. `tableCards` ya llega en
  cada snapshot y permite recuperarlo al reconectar. Un historial por jugada/autor
  requeriría más datos; posponer ese alcance.
- **Reacciones (M):** preferibles antes que chat completo para dar presencia social.
  Necesitan límite de frecuencia en servidor, mute y evitar tapar cartas.
- **Interacción con mate/bowl (M):** un gesto breve sin efecto sobre las reglas.
- **Confirmación (S–M):** quitada por decisión de experiencia. Las jugadas válidas
  desde JUGAR o drag se envían directamente; el servidor conserva la validación.

### Posponer o descartar por ahora

- **Cámara libre (L):** la mesa está en perspectiva, pero manos/cartas usan una capa
  de pantalla. Una órbita libre desalinearía ambas. Primero evaluar un parallax
  limitado o vistas predefinidas con recentrado; luego decidir si aporta al juego.
- **Chat completo (M–L):** esperar hasta resolver reacciones; suma moderación,
  teclado móvil y espacio de interfaz durante una partida.
- **Puntaje y podio entre partidas (M–L):** necesita reglas de puntuación y desempate.
  El resultado de una partida ya muestra ganadores; no inventar un sistema persistente.
- **Bebidas elegibles, paletas, tema claro y luz fría (M):** mantener una dirección
  visual coherente primero. Los presets de rendimiento tienen más valor inmediato.
- **Texturas escaneadas y música definitiva (M):** opcionales, con licencia y peso
  controlados; validar primero la dirección artística en una partida real.
- **Limitar a un comodín:** descartado como corrección. La regla canónica permite
  varios doses. Solo reconsiderar con una decisión explícita de cambiar el juego.

### Cerrado o mejorado en el cliente

Drag vertical, desplazamiento horizontal, resultado con última jugada, bowls,
yerba, música y efectos ya existen. Los controles y el final cuentan con recorridos
automatizados; eso no reemplaza la prueba sostenida en Android/iOS físicos.

En esta revisión: flexión de dedos sobre ejes consistentes del rig; cartas locales
siempre por encima de manos y seleccionadas por delante del resto; borde de jugadas
posibles, borde único de selección y foco restaurados; audio cierra al tocar afuera o con Escape sin cerrarse
al ajustar el volumen. Las posiciones de los asientos se conservan.

## Criterio de prioridad

- **P0 — Corregir primero:** rompe reglas, confunde el estado de la partida o impide continuar.
- **P1 — Próximo pulido:** mejora controles y comprensión durante una partida real.
- **P2 — Inmersión:** suma presencia al entorno sin cambiar la forma de jugar.
- **P3 — Opcional:** ideas para una etapa posterior, cuando el núcleo esté estable.

El esfuerzo es orientativo: **S** pequeño, **M** mediano, **L** grande.

## P0 — Reglas, estado y flujo de partida

| Prioridad | Idea | Esfuerzo | Criterio de aceptación |
|---|---|---:|---|
| P0 | Respetar el pepineado cuando se repite valor y cantidad | M | Si ya hay, por ejemplo, 12×1 en mesa y se juega otro 12×1, se muestra y aplica el salto al siguiente jugador. Agregar una prueba de backend para ese caso. |
| P0 | Mostrar la última jugada al terminar | S | La partida no vuelve inmediatamente al lobby: aparece el resultado, los ganadores, la última combinación y una acción clara para volver o jugar otra. |
| Fuera de alcance | Limitar a un solo comodín | — | Contradice la regla canónica; no implementar como arreglo. |
| P0 | Verificar el flujo de reinicio al terminar y al abandonar | M | La mesa se limpia en el momento correcto, los jugadores vuelven al lobby sin estados viejos y las salas vacías se eliminan. |

## P1 — Controles y lectura durante la partida

| Prioridad | Idea | Esfuerzo | Criterio de aceptación |
|---|---|---:|---|
| P1 | Drag and drop de cartas | M | Arrastrar una carta o un grupo válido hasta una zona de juego muestra el destino y permite soltarlo. En mobile debe convivir con el swipe de la mano. |
| P1 | Deslizar la mano con mouse como en touch | S | Arrastrar horizontalmente desplaza las cartas sin seleccionar ni iniciar un drag vertical. La scrollbar permanece oculta. |
| P1 | Destacar al jugador que tiene el turno | M | Brillo o pulso suave en su avatar y/o mano, con una indicación de dirección de ronda. No debe tapar las cartas ni molestar. |
| P1 | Mostrar quién va a jugar y hacia dónde avanza la ronda | M | Indicador breve y persistente cerca de la mesa, especialmente al cambiar de turno. |
| P1 | Muestra de carga cuando alguien está por jugar | S | Estado “pensando” o animación discreta mientras llega la acción del servidor. Debe diferenciarse de una desconexión. |
| P1 | Mejorar audio y su panel de controles | S | Música y efectos separados, volumen visible, mute claro y cierre al hacer click fuera del popup. |
| P1 | Sonidos de acciones | M | Efectos sutiles para repartir, seleccionar, soltar, jugar, pasar, pepinear y cambio de turno. Deben poder apagarse por separado. |

## P2 — Mesa y ambiente

| Prioridad | Idea | Esfuerzo | Criterio de aceptación |
|---|---|---:|---|
| P2 | Mostrar el mazo de cartas acumuladas | M | La pila de descarte o cartas jugadas queda visible en mesa, con cantidad y acceso a consultar el historial si hace falta. |
| P2 | Mate con yerba y rotación por jugador | M | El mate tiene contenido visible, gira hacia el jugador activo y se resalta de forma sutil en su turno. |
| P2 | Maníes en bowl | S | Agregar bowl y cantidad suficiente de maníes al ambiente sin competir con las cartas. |
| P2 | Bebida por jugador | M | Cada asiento puede tener vaso, birra o fernet. Primero conviene resolverlo como decoración fija; la selección personalizada puede esperar. |
| P2 | Manos que sostienen las cartas | L | Reemplazar la sensación de cartas flotando por manos coherentes con el asiento, orientación y cantidad de cartas. Requiere revisar modelos, cámara y anclajes. |
| P2 | Corregir manos espejadas | M | Las manos y cartas deben respetar la orientación de cada jugador y verse naturales desde la cámara correspondiente. |
| P2 | Perspectiva de cartas | M | Las cartas de rivales y las de la mano propia siguen la perspectiva de la mesa, sin perder legibilidad. |
| P2 | Cámara 3D de la mesa | L | Poder mover, rotar o acercar la cámara sin perder la zona de juego ni los controles. En mobile debe existir un gesto simple y un botón para recentrar. |
| P2 | Interacción con el entorno | M | Clickear mate, maníes o bebida produce una animación y un sonido breve. No debe afectar el turno ni bloquear controles. |
| P2 | Música acorde al entorno | M | Música ambiental cálida, con ritmo sutil, sin piano protagonista y con licencia clara o audio propio. Debe poder apagarse. |

## P3 — Sistemas y personalización

| Prioridad | Idea | Esfuerzo | Criterio de aceptación |
|---|---|---:|---|
| P3 | Chat de sala | M | Primera versión: mensajes cortos como globos temporales. Luego se puede sumar un panel de chat completo. |
| P3 | Reacciones con emojis y sonidos | M | Aplausos, risas y reacciones rápidas con cooldown, sin spam y con mute de efectos. |
| P3 | Advertencia de jugador cerca de ganar | S | Aviso visible cuando alguien queda con pocas cartas, sin revelar información que las reglas no permitan. |
| P3 | Puntaje, suma de cartas y podio | M | Definir antes si las cartas restantes suman puntos, cómo se desempata y cómo se calcula el podio entre rondas. |
| P3 | Ajuste de gráficos | M | Calidad baja/media/alta, resolución de sombras y cantidad de efectos. Debe guardarse localmente. |
| P3 | FPS | S | Mostrar FPS solo dentro de un panel avanzado o debug; no dejarlo visible en la interfaz normal. |
| P3 | Tema claro/oscuro | M | Variación completa y legible de la interfaz. La mesa 3D puede conservar una iluminación propia si el tema claro no funciona bien. |
| P3 | Luz cálida/fría | S | Presets de iluminación que no cambien la legibilidad de cartas, botones o estados de turno. |

## Bloques originales de referencia (incluyen trabajo ya implementado)

### Bloque 1 — Confiabilidad

1. Reproducir y cubrir el caso de pepineado repetido en backend y cliente.
2. Corregir la pantalla de fin de partida para mostrar resultado y última jugada.
3. Revisar comodines y reinicios de ronda.
4. Confirmar limpieza de salas, desconexiones y reconexiones.

### Bloque 2 — Interacción principal

1. Mantener swipe horizontal y drag vertical sin conflictos.
2. Mantener envío directo desde drag and drop, decisión ya cerrada.
3. Destacar turno, dirección de ronda y jugador pensando.
4. Cerrar popup de audio haciendo click fuera.
5. Incorporar efectos de sonido mínimos.

### Bloque 3 — Mesa legible

1. Hacer visible el mazo acumulado.
2. Corregir orientación y anclaje de manos/cartas.
3. Agregar mate con yerba, bowl de maníes y bebidas como decoración.
4. Ajustar cámara y perspectiva.

### Bloque 4 — Inmersión y comunidad

1. Música definitiva.
2. Interacciones del entorno.
3. Reacciones.
4. Chat.
5. Podio y puntaje.

## Decisiones que conviene cerrar antes de desarrollar

- **Drag and drop — cerrado:** soltar una combinación válida juega directamente.
- **Mazo acumulado:** si muestra solo la pila visual, el historial completo o ambos.
- **Puntaje:** qué valor tienen las cartas restantes y cuándo se reinicia el podio.
- **Cerca de ganar:** cuántas cartas activan la advertencia y si todos los jugadores la reciben.
- **Cámara:** libertad total de movimiento o un conjunto de ángulos predefinidos.
- **Chat y reacciones:** límite de frecuencia, moderación y persistencia; inicialmente deberían ser efímeros y solo de la sala.

## Regla de implementación

Las reglas siguen viviendo en el backend. El cliente puede anticipar validaciones y animaciones, pero nunca debe decidir por sí solo un pepineado, un ganador, una cantidad válida de cartas o el reinicio de una ronda.

## Auditoría del estado actual

Estados usados: **Hecho en código** significa que la funcionalidad ya existe; **Parcial** significa que existe una primera versión, pero falta cerrar comportamiento o experiencia; **Pendiente** significa que no encontré una implementación utilizable.

| Área | Estado actual | Evidencia y próximo control |
|---|---|---|
| Pepineado | Implementado / regresión de reglas probada | Caso 12×1 sobre 12×1 cubierto junto con salto, mesa conservada y rechazo de inferior. El reporte histórico no se reprodujo de punta a punta. |
| Resultado y última jugada | Implementado / integración local probada | `GameTable` conserva la escena final y muestra ganadores y última jugada hasta volver explícitamente al lobby. La suite local probó final y nueva partida en ambos tamaños. |
| Comodines | Decisión cerrada | La regla permite varios doses. No limitar a uno como corrección. |
| Reinicio y salas huérfanas | Implementado / integración probada | Desconexión, pausa, reconexión conservando asiento, reset después de 60 segundos, transferencia de creador y eliminación de sala vacía verificadas con `verify-disconnect-expiry.mjs`. |
| Drag and drop | Implementado / QA histórica | Carta/grupo, válido/inválido, revisión vieja y envío directo. Touch físico queda pendiente. |
| Swipe horizontal de la mano | Implementado / QA automatizada | Mouse, navegación y touch emulado probados con mano larga. Quedan trackpad y dispositivos físicos. |
| Confirmación de jugada | Eliminada | Tanto JUGAR como soltar envían directamente las jugadas válidas; el servidor conserva la validación. |
| Turno y jugador activo | Parcial | Avatar con pulso suave, contador persistente y estado separado, pausa sin pulso. Pendiente dirección de ronda; no predecir el siguiente turno antes de resolver comodines/pepineado. |
| Carga / espera | Implementado | Texto distingue envío, jugador pensando, reconexión y pausa. No agregar porcentaje ficticio de espera de una persona. |
| Audio | Parcial | Música/efectos y volumen persistido dentro de Configuración. Falta cubrir continuidad entre pantallas: el runtime se desmonta y destruye el motor. |
| Última jugada y descarte | Implementado / historial pospuesto | Última combinación, pila acotada y contador desde `tableCards` del servidor. No hay historial agrupado por jugada/autor. |
| Mate con yerba | Movimiento y resalte implementados / anclajes por validar | Todos ven su reubicación según el turno; solo quien juega ve el aro. Destinos por zonas, pendientes colisiones, resize y 2–8 asientos. |
| Bowl de maníes | Hecho en código | Ya hay dos bowls procedurales con maníes en la escena. Falta decidir si se agrega interacción. |
| Bebidas por jugador | Pendiente | La escena tiene vasos decorativos, pero no una bebida asociada a cada asiento ni selección por jugador. |
| Manos y orientación | Parcial | Ya se carga un modelo de manos y se generan manos para rivales y jugador local. La orientación se calcula por asiento, pero necesita revisión visual en cada posición y sigue siendo un área con riesgo de espejado. |
| Perspectiva de cartas | Parcial | Manos y abanicos rivales comparten giro por asiento; cartas centrales en mundo 3D y propias en capa de pantalla legible. No es una escena apta aún para cámara libre. |
| Cámara y entorno movible | Parcial | Zoom 100–160%, restablecer y vista de espectador al ganar implementados. Órbita libre y gestos de cámara pendientes/pospuestos. |
| Música de ambiente | Hecho en código / ajuste pendiente | Hay ambiente cálido original generado con Web Audio y control independiente. Falta validación estética en partida real. |
| Chat, reacciones, podio y puntaje | Pendiente | No encontré una primera versión de estas funciones; el lobby sí muestra ganadores al finalizar. |
| Advertencia de pocas cartas | Implementado | Color destacado al tener una o dos cartas, usando únicamente el contador público. No repite toasts ni agrega una regla. |
| Interacciones del entorno | Pendiente | No encontré acciones sobre mate, bowls o bebidas; la escena decorativa no afecta la partida. |
| Ajustes gráficos, FPS, temas y luz | Implementado / temas pospuestos | Configuración de calidad ahorro/automática/alta y pantalla completa junto a la tuerca; el contador de FPS se quitó de la interfaz. Cambio repetido de presets no aumenta geometrías en las pruebas. Temas e iluminación alternativa pospuestos. |

## QA manual pendiente para controles

1. **PC con mouse:** desplazar la mano horizontalmente; seleccionar una carta; seleccionar un grupo; arrastrar una carta hacia arriba; soltar dentro y fuera de la zona; verificar que el swipe no seleccione ni abra una jugada.
2. **PC con trackpad:** repetir el desplazamiento horizontal y comprobar que la rueda vertical mueva la mano sin mover la página.
3. **Mobile touch:** repetir tap, swipe horizontal y drag vertical en portrait y landscape; verificar que el gesto horizontal no active el drag y que una carta o grupo pueda llegar a la zona de drop.
4. **Estados de partida:** repetirlo durante el turno propio, durante el turno rival, con partida pausada y después de una actualización de estado mientras se arrastra.
5. **Cierre:** comprobar que un drop válido no envíe cartas con una revisión vieja y que una jugada inválida no cambie la selección de forma inesperada.

La parte automatizada de esta matriz está cubierta por `verify-visual.mjs` y
`verify-hand-interactions.mjs`. La integración local figura en
`verify-live-game.mjs`. Mantener la prueba manual en teléfonos físicos y trackpad;
convertir las fallas reproducibles en casos de regresión.

## Observabilidad agregada al backend

El backend registra en Render eventos estructurados con sala, jugador, ronda, revisión y duración:

- `room_created`, `player_joined`, `player_reconnected`, `player_disconnected`, `player_left` y `room_deleted`.
- `game_started` con cantidad de jugadores, mazos y primer turno.
- `cards_played` con cartas por valor y palo, jugada anterior, comodín, pepineado, jugador salteado, turnos antes y después, ronda, ganadores y duración.
- `turn_passed` con turnos, ronda, cartas vigentes, revisión y duración.
- `game_reset` cuando una salida o desconexión expirada cancela una partida.
- `hub_rejected` para acciones rechazadas por reglas y `hub_failed` para errores inesperados.
- `state_broadcast` con nivel `Debug`, para medir duración y tamaño lógico del estado sin llenar los logs normales.

En Render se pueden buscar `room=PRIMARDOS`, `pepineado=true`, `game_reset`, `hub_rejected` o `hub_failed`. Para recibir los eventos de diagnóstico `Debug`, configurar temporalmente la variable `Logging__LogLevel__GameServer=Debug` en el servicio y volver a desplegar. Conviene dejarla en `Information` durante el uso normal para no agregar ruido ni costo innecesario.
