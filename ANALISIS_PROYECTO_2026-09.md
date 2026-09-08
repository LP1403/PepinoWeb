# Pepino: revisión del proyecto y dirección web 3D

Fecha: 8 de septiembre de 2026. Revisión del código local en la rama `unity`, incluidos cambios de Three.js sin commit. No se modificó la implementación del juego.

## Conclusión

React + Three.js + el backend .NET existente es una base adecuada para perseguir la composición de las dos capturas de UNO aportadas. El obstáculo principal es la dirección visual y la coordinación de estado/interacción, no una carencia de capacidad 3D. Recomiendo concentrar la próxima alpha en web, conservar Unity y evitar desarrollar dos presentaciones simultáneamente.

Three.js proporciona escenas, cámaras, materiales, texturas, luces y sombras; no proporciona por sí solo el diseño, las reglas ni una interfaz terminada. Referencia técnica: https://threejs.org/manual/en/fundamentals.html

## Alcance y verificaciones

- Inspeccionados los flujos principales del backend, conexión y presentación React, escena Three.js, configuración y componentes de presentación/red de Unity, reglas y estructura del repositorio. Esto no equivale a certificar cada asset o cada camino de ejecución.
- Backend: `dotnet build --no-restore` pasó, sin errores ni advertencias.
- Web: el comando npm del entorno falla por una instalación/ruta de npm incompleta. Ejecutando directamente `node node_modules/typescript/bin/tsc -b`, aparecen tres errores: imports sin uso de CreatorTest y OverheadTable en App.tsx; acceso a deckCount sobre never en GameTable.tsx:135. El último ocurre dentro de una rama que exige que gameMode no exista.
- No se ejecutó una partida ni se capturó el render actual. La comparación visual se basa en las capturas objetivo y en el código de la escena; no se midieron FPS ni parecido por píxel.
- No hay una herramienta Unity MCP disponible en esta sesión. No se verificó Play Mode ni una compilación de Unity.
- Las pruebas HTML de raíz son históricas/manuales. No se encontró una suite automatizada propia de reglas y flujos en los archivos inventariados.

## Qué existe

### Backend: Back/GameServer/GameServer

ASP.NET Core 8 con SignalR. Salas en memoria, creador, selección de mazos, reparto privado, turnos, grupos, comodín, pepineado, pases y ganadores. Cada jugador recibe su mano y solo la cantidad de cartas de los demás. Hay endpoints HTTP de consulta de salas. No hay autenticación ni base de datos configuradas en Program.cs.

GameHub implementa directamente gran parte del juego, mientras GameLogicService tiene otra implementación. GameRoomManager usa esta segunda ruta en algunos métodos. Es una fuente concreta de divergencias: deben converger en un único servicio de reglas y transiciones.

### Web: Front/game-client

React 19, TypeScript, Vite, SignalR y Framer Motion. La dependencia Three.js ya está incorporada. App abre Lobby y luego GameTable; GameTable ahora renderiza GameTable3D, que sincroniza la escena imperativa de game3d/pepinoScene.ts.

Ya existen: cartas 3D con texturas generadas en canvas, selección por raycast, orden por valor/palo, abanicos rivales, última jugada animada, botones y estados de turno. Hay varios prototipos anteriores (PepinoArena, OverheadTable, PlayerHand) que conviene distinguir de la ruta activa.

Hay arte propio en src/assets/cartas, pero la nueva escena genera sus caras/dorsos: la mera presencia de las imágenes no significa que la vista Three.js las use.

### Unity: UnityProject/PepinoUnity3D

Cliente con NetworkManager SignalR, GameManager, lobby, HUD, HandManager, TableManager, OpponentSeatManager, PlayerHandsView y herramientas de editor para configurar/validar presentación. NetworkManager tiene conexión automática y cola hacia el hilo principal.

ProjectSettings/ProjectVersion.txt indica **6000.5.4f1**, diferente de la versión indicada en el contexto antiguo. PepinoUnity3D en raíz contiene referencias/documentación; no es el proyecto abrible.

La presentación es compleja: el bootstrap reajusta la cámara en LateUpdate y la mano tiene su propia cámara/capa y colocación por viewport. Cambios manuales de cámara pueden ser sobrescritos. Además, varias cargas de prefabs/configuración del bootstrap usan AssetDatabase dentro de UNITY_EDITOR: hay que verificar referencias y fallbacks en una build real, no asumir equivalencia con el Editor.

## Hallazgos que deben corregirse

1. **Validación de propiedad de cartas.** GameHub.PlayCards valida valor/cantidad del objeto recibido, pero no verifica IDs únicos ni reconstruye las cartas desde la mano del servidor. Se pueden enviar cartas ajenas, duplicadas o con valores adulterados. Aceptar IDs, comprobar unicidad y pertenencia, y usar exclusivamente las cartas reales del servidor.
2. **Animación y autor desincronizados.** CardsPlayed actualiza lastPlayedCards en el hook, pero el autor que recibe GameTable3D sale de gameState.lastPlayerId, actualizado en otro evento. La escena puede animar con el autor anterior. Su deduplicación solo mira IDs de cartas, por lo que la actualización posterior del autor puede no corregirlo. Usar un evento de jugada atómico con autor, cartas e ID/secuencia.
3. **Identidad del jugador.** La web determina turno por nombre; GameTable intenta identificar al local por nombre y tamaño de mano, con fallback al primer jugador. Nombres repetidos pueden asociar el asiento equivocado. Usar identidad estable y ConnectionId explícito para esta alpha.
4. **PEPINEADO identifica mal al saltado.** MoveToNextTurn avanza dos posiciones y marca/emite PlayerSkipped sobre el jugador que recibe el turno, no sobre el omitido. Además, el banner Three.js usa el nombre del autor para decir que se saltó su turno. Separar autor, víctima y próximo jugador.
5. **Reglas/documentación desalineadas.** Se generan 48 cartas por mazo, pero SelectGameMode y CalculateGameMode calculan metadatos con 40. StartGame en el hub permite un jugador. PassTurn no bloquea al dueño de una nueva ronda cuando aún hay última jugada. Corregir contra REGLAS_PEPINO.md.
6. **Comodín requiere precisión semántica.** El 2 ya evita la restricción de cantidad. Actualmente el backend avanza al siguiente jugador y deja los doses como última jugada; el siguiente debe igualar su cantidad. No implementa que quien tiró el 2 vuelva inmediatamente a abrir otra jugada. La documentación actual describe ese comportamiento, pero mensajes anteriores del dueño expresan otra expectativa. Acordar esta única regla antes de cambiarla; conservar el inicio por primer asiento con 3♦, ya confirmado.
7. **Ciclo de sala.** JoinRoom no bloquea incorporaciones durante partida ni duplicación de la misma conexión. La salida no normaliza correctamente todos los casos de índice de turno/creador. La reconexión automática web no implementa recuperación de asiento/mano. El estado compartido usa Dictionary/List sin serialización por sala. Necesita transiciones centralizadas.
8. **Última jugada ganadora.** El hub puede retornar por final de partida antes de actualizar la mesa y emitir CardsPlayed, omitiendo su presentación. Validar también nueva ronda cuando el último que jugó ya ganó: el actual criterio depende de que su turno vuelva, pero se omiten los ganadores.
9. **Publicación web.** El hook usa http://127.0.0.1:5264/gamehub fijo. En otro dispositivo apuntaría a ese dispositivo. Configurar URL por entorno y conexión segura para despliegue; no hace falta un botón manual para conectar.
10. **Recursos gráficos.** Se retiran meshes sin liberar sus materiales/texturas; dispose final solo libera renderer y eventos. Añadir gestión de recursos y caché compartida de caras/dorso. Three.js requiere liberar explícitamente estos recursos: https://threejs.org/manual/en/cleanup.html

## Comparación con las capturas objetivo

Las capturas muestran una cámara elevada oblicua, una superficie estilizada ocupando casi toda la pantalla, fondo azul luminoso, cartas grandes y UI periférica. No muestran manos humanas ni una cámara a la altura de los ojos. Para esta nueva dirección visual, propongo seguir esas capturas.

- **Cámara:** composición fija durante la partida. Actualmente arrastrar mueve la cámara y la rueda cambia zoom; eso compite directamente con manipular/desplazar cartas.
- **Mano local:** abanico en primer plano, números legibles, elevación de selección y agrupación por valor. Actualmente vive en coordenadas del mundo y se reparte hasta en tres filas; su lectura depende de cámara, mesa y luces.
- **Rivales:** abanicos diferenciados alrededor de la superficie y avatar/nombre junto a cada asiento. Actualmente son cartas tumbadas en una elipse, mientras los nombres forman una lista independiente arriba a la derecha; en móvil esa lista desaparece.
- **Jugada central:** grande, estable y legible; para dobles/triples, solapado controlado que conserve cada valor visible. Actualmente la colocación tiene jitter aleatorio y rotación X de 0.95 radianes, por lo que no queda apoyada plana sobre la mesa.
- **Arte e iluminación:** escenario estilizado claro, contraste y sombras suaves. La escena actual usa fieltro, borde metálico dorado, niebla oscura, luces verdes y viñeta: comunica una estética distinta.
- **HUD:** avatares y turno cerca del jugador correspondiente; controles fuera de la mano. El dock actual anclado abajo no reserva espacio con la mano 3D.
- **Efectos:** trayectoria desde el asiento correcto, pequeño impacto, señales de nueva ronda y pepineado legibles y breves. El lerp actual es una base, no una secuencia de animación coordinada con el protocolo.

No trasladar acciones ajenas a Pepino: mazo para robar, botón de cantar UNO o temporizador no son requisitos por aparecer en la referencia.

## Propuesta de implementación

Mantener .NET como autoridad, React para lobby/HUD/modal accesible y Three.js para escenario/cartas/efectos. Conservar la integración imperativa actual inicialmente; cambiar a otra abstracción no arregla la composición por sí mismo.

1. **Base verificable:** corregir TypeScript, fijar identidad/eventos y validación del backend. Agregar pruebas de reglas para triples, As, comodín, pepineado, pases, ganadores y desconexión.
2. **Escena de referencia reproducible:** modo de desarrollo con estado fijo, cuatro jugadores, mano y jugada conocidas; presets de 2/4/8 jugadores y manos grandes. Cámara bloqueada y dimensiones de viewport predefinidas.
3. **Composición:** escenario claro, cámara elevada, mano local en capa de presentación independiente con tamaño proyectado controlado; cartas centrales dimensionadas por su lectura en pantalla; rivales con dorsos contrastados y avatares anclados a su asiento.
4. **Interacción:** click/touch selecciona; grupos iguales consecutivos; rueda/arrastre desplaza la mano, no la cámara. Arrastrar una selección a la mesa abre confirmación; JUGAR/PASAR fuera del área de cartas. Resaltar sutilmente combinaciones legales solo en tu turno.
5. **Partida real y acabado:** cola de eventos con actor explícito, viaje de cartas e impacto; nueva ronda, pepineado, victoria, sonido, reconexión y estados de espera. Precarga de texturas y liberación de recursos.
6. **Verificación visual repetible:** capturas a resoluciones fijas, comparación lado a lado con referencias y pruebas con dos clientes reales. Automatizar límites visibles, ausencia de solapados y origen de animaciones; revisión visual adicional para calidad artística. No dar por terminado por compilar.

Para dos jugadores/tres mazos hay 144 cartas, hasta 72 por mano. No cabe una reproducción literal del pequeño abanico de UNO: agrupar repetidos y permitir desplazamiento sin reducir a miniaturas. Probar también 8 jugadores; las capturas solo resuelven cuatro.

## Criterio de aceptación de la próxima alpha

- Números locales y jugada central legibles en 1280×720 y 1920×1080; botones sin taparlos.
- Rival identificable por nombre, asiento y cantidad; nunca muestra sus caras privadas.
- Doble/triple conserva cantidad legible; jugada rival parte de su asiento.
- Selección inválida no habilita JUGAR y el servidor también la rechaza.
- Cámara estable durante interacción y transiciones; cambios de tamaño conservan zonas seguras.
- Casos de 2/4/8 jugadores y 1/2/3 mazos cubiertos; mano grande navegable.
- Sin errores de compilación ni consola durante partida de prueba, con medición posterior de rendimiento en dispositivos objetivo.

Firebase, assets finales y Unity siguen pendientes; no son requisitos para validar primero esta dirección visual.
