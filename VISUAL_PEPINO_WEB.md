# Ambiente de mesa — implementación web

Referencia: mesa argentina de madera/paño, iluminación cálida, manos y lobby
alrededor de una mesa. Es una primera implementación 3D; no una reproducción
fotorrealista de las imágenes de referencia.

## Implementado

- Mesa con paño verde, costura, marca Pepino y madera procedural.
- Mate/bombilla, vasos, cuencos y lámparas, sin descargas de terceros.
- Cámara de lobby elevada y entrada animada a la cámara de juego.
- Lobby oscuro, lista funcional y disposición de asientos en escritorio.
- Cartas crema con valores/palos legibles y dorsos verde oscuro.
- Manos importadas de Unity, horneadas a GLB, y mangas simples.
- Animación corta de manos del autor de la jugada y vuelo de cartas desde su asiento.
- Mobile conserva las acciones juntas y reduce decoración: sin manos locales;
  con más de cuatro jugadores tampoco renderiza manos rivales.
- Audio ambiental y controles independientes de música/efectos.

## Pulido pendiente

- Rig de manos con agarre específico y dedos articulados (ahora pose estática).
- Unir las manos locales a una mano 3D real: las cartas locales siguen siendo
  controles HTML para mantener selección, scroll y accesibilidad.
- Cartas centrales siguen en una capa 3D ortográfica inclinada; aún no son
  cartas físicas que reciban sombras sobre el paño.
- Materiales escaneados y más detalle del ambiente si el rendimiento lo permite.
- Medir FPS/memoria y consumo en Android/iOS reales; viewport emulado no equivale
  a dispositivo real.

## Revisar

Dev: `http://localhost:5174/?demo=1`, `?demo=1&players=2`,
`?demo=1&players=8&hand=48`. Las demos no validan el protocolo del backend.
El backend y las reglas no fueron modificados por este cambio visual.
Créditos del asset: `Front/game-client/public/models/ASSET_CREDITS.md`.
