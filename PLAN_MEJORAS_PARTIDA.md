# Plan de mejoras de Pepino

Las ideas se toman como hipótesis de diseño: primero se valida si mejoran la lectura y el ritmo de la partida, y después se decide cuánto invertir en cada una.

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
| P0 | No permitir acumular comodines innecesariamente | S | El cliente no permite seleccionar más de un 2 cuando uno ya cumple la función del comodín. El backend sigue siendo la validación final. |
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

## Orden recomendado de trabajo

### Bloque 1 — Confiabilidad

1. Reproducir y cubrir el caso de pepineado repetido en backend y cliente.
2. Corregir la pantalla de fin de partida para mostrar resultado y última jugada.
3. Revisar comodines y reinicios de ronda.
4. Confirmar limpieza de salas, desconexiones y reconexiones.

### Bloque 2 — Interacción principal

1. Mantener swipe horizontal y drag vertical sin conflictos.
2. Decidir si el drag and drop confirma directamente o mantiene una confirmación opcional.
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

- **Drag and drop:** si soltar una combinación válida juega directamente o abre una confirmación opcional en ajustes.
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
| Pepineado | Parcial | El backend calcula `IsPepineado`, salta al jugador correcto y ya hay una prueba específica. Falta reproducir el caso reportado de 12×1 y agregar una prueba de integración cliente-servidor si vuelve a fallar. |
| Resultado y última jugada | Parcial | El backend conserva `LastPlay` antes de finalizar y el cliente muestra la última combinación durante la mesa; el lobby muestra ganadores. Falta confirmar que la pantalla final conserve la jugada visible el tiempo suficiente antes de permitir volver a jugar. |
| Comodines | Decisión pendiente | La regla canónica actual permite jugar el 2 como comodín libre. La idea de limitar a un solo comodín contradice esa regla y no debe implementarse sin cambiar primero la regla del juego y sus pruebas. |
| Reinicio y salas huérfanas | Hecho en código | El backend limpia la partida al abandonar, da 60 segundos de gracia a una desconexión y elimina la sala cuando queda vacía. Falta una prueba de integración con desconexión, reconexión y expiración. |
| Drag and drop | Hecho en código / QA pendiente | Hay arrastre vertical de una carta o grupo, ghost visual, zona de drop y validación antes de abrir la confirmación. Probar en PC con una carta, grupo, drop válido e inválido, y en mobile con touch portrait y landscape. |
| Swipe horizontal de la mano | Hecho en código / QA pendiente | `hand-scroll` usa overflow horizontal, touch action y rueda vertical como desplazamiento horizontal; el gesto horizontal suprime el click para no seleccionar cartas. Probar mouse, trackpad, touch y una mano que exceda el ancho. |
| Confirmación de jugada | Hecho en código | Tanto JUGAR como soltar abren confirmación. Queda decidir si el drop válido debe jugar directamente para reducir pasos. |
| Turno y jugador activo | Parcial | Hay avatar resaltado, etiqueta `TU TURNO`, `TURNO DE ...`, estado de pausa y texto “está pensando…”. Falta un indicador claro de dirección de la ronda y un pulso más visible sobre la mano activa. |
| Carga / espera | Parcial | Se muestran “está pensando…” y “Reconectando…”. No hay una animación de espera específica. |
| Audio | Parcial | Existen música procedural, efectos separados, volúmenes, mute, persistencia local y cues para selección, jugada, comodín, pepineado, turno y pase. El panel es un `<details>` y todavía no cierra explícitamente al hacer click fuera. |
| Última jugada y descarte | Parcial | Se muestra la última combinación y hay una pila visual animada en la escena. No existe historial consultable ni contador del acumulado completo. |
| Mate con yerba | Parcial | El mate, la yerba y la bombilla ya existen como decoración fija. Falta rotarlo y resaltarlo según el jugador activo. |
| Bowl de maníes | Hecho en código | Ya hay dos bowls procedurales con maníes en la escena. Falta decidir si se agrega interacción. |
| Bebidas por jugador | Pendiente | La escena tiene vasos decorativos, pero no una bebida asociada a cada asiento ni selección por jugador. |
| Manos y orientación | Parcial | Ya se carga un modelo de manos y se generan manos para rivales y jugador local. La orientación se calcula por asiento, pero necesita revisión visual en cada posición y sigue siendo un área con riesgo de espejado. |
| Perspectiva de cartas | Parcial | Las cartas propias conservan legibilidad en una capa 3D de pantalla y las cartas jugadas se inclinan; falta una perspectiva coherente para las manos rivales. |
| Cámara y entorno movible | Pendiente | La cámara es fija y no encontré controles de órbita, zoom o recentrado. |
| Música de ambiente | Hecho en código / ajuste pendiente | Hay ambiente cálido original generado con Web Audio y control independiente. Falta validación estética en partida real. |
| Chat, reacciones, podio y puntaje | Pendiente | No encontré una primera versión de estas funciones; el lobby sí muestra ganadores al finalizar. |
| Advertencia de pocas cartas | Pendiente | No encontré aviso específico para jugadores cerca de ganar. |
| Interacciones del entorno | Pendiente | No encontré acciones sobre mate, bowls o bebidas; la escena decorativa no afecta la partida. |
| Ajustes gráficos, FPS, temas y luz | Pendiente | No encontré un panel de calidad, FPS, tema claro/oscuro ni presets de iluminación. |

## QA manual pendiente para controles

1. **PC con mouse:** desplazar la mano horizontalmente; seleccionar una carta; seleccionar un grupo; arrastrar una carta hacia arriba; soltar dentro y fuera de la zona; verificar que el swipe no seleccione ni abra una jugada.
2. **PC con trackpad:** repetir el desplazamiento horizontal y comprobar que la rueda vertical mueva la mano sin mover la página.
3. **Mobile touch:** repetir tap, swipe horizontal y drag vertical en portrait y landscape; verificar que el gesto horizontal no active el drag y que una carta o grupo pueda llegar a la zona de drop.
4. **Estados de partida:** repetirlo durante el turno propio, durante el turno rival, con partida pausada y después de una actualización de estado mientras se arrastra.
5. **Cierre:** comprobar que un drop válido no envíe cartas con una revisión vieja y que una jugada inválida no cambie la selección de forma inesperada.

La auditoría de código confirma qué existe, pero no reemplaza estas pruebas en un navegador de escritorio y en un teléfono real. El primer bloque recomendado es ejecutar esta matriz y, si aparece una falla, convertir cada caso en una prueba automatizada o una corrección concreta.
