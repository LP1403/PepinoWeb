# Assets de cartas

Los assets visuales de las cartas viven separados de los modelos 3D y de la lógica de
palos. La estructura prevista es:

```text
cards/
  front/
    special/              # comodín, Pepino de Oro y otras cartas únicas
    diamond/              # cartas del palo ♦ ya ilustradas
    spade/                # futuras cartas del palo ♠
    heart/                # futuras cartas del palo ♥
    club/                 # futuras cartas del palo ♣
  backs/
    deck-1.png
    deck-2.png
    deck-3.png
```

Cada carta conserva su `suit` y `value` del juego. `deckIndex` solo identifica la
apariencia del mazo del que salió, empezando en `0`; no cambia reglas ni validaciones.

Para agregar una carta frontal, usar nombres simples y estables, por ejemplo
`front/diamond/1-gaucho.png`. Para los dorsos, mantener un archivo por mazo. Los
clientes reciben el índice del dorso de los rivales, pero nunca sus valores ni su mano.
