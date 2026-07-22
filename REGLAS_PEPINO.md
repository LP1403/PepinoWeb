# Reglas del juego Pepino

Documento canónico de reglas para humanos (jugadores y desarrollo).  
Si el código y este doc discrepan en un detalle fino, **hoy manda el backend**  
(`Back/GameServer/GameServer/Services/CardService.cs` + `Hubs/GameHub.cs`),  
pero el objetivo es que ambos queden alineados a este texto.

Inventado / jugado en mesa por el dueño del repo y amigos.  
Resumen UI existente: Lobby web → “Ver Reglas del Juego”.

---

## 1. Resumen (como en el front)

| Tema | Regla |
|------|--------|
| Objetivo | Quedarse sin cartas |
| Pepino de Oro | El **3♦** define quién inicia la partida |
| Jugadas | 1 hasta N cartas del **mismo valor** |
| Turnos | El siguiente debe igualar cantidad y jugar valor **≥** al de la mesa |
| PEPINEADO | Misma jugada (valor + cantidad) = se salta al siguiente |
| Victoria | Quien se queda sin cartas gana (puede haber varios ganadores) |

---

## 2. Mazo y temática

- Naipes tipo español, **4 palos**:
  - ♠ Policías  
  - ♥ Médicos  
  - ♦ Soldados  
  - ♣ Bufones  
- Valores **1–12** por palo (48 cartas por mazo en código actual).
- Partidas de **2–8** jugadores.
- **1–3 mazos** según modo elegido por el creador de la sala (no siempre automático).

### Profesiones por palo

Igual que en el Lobby web:

- ♠ Policías  
- ♥ Médicos  
- ♦ Soldados  
- ♣ Bufones  

---

## 3. Pepino de Oro (quién empieza)

- El **3♦** se llama **Pepino de Oro**.
- Quien tenga al menos un Pepino de Oro **empieza la partida** (tiene el primer turno).

### Varios mazos = varios Pepinos de Oro

Con 2 mazos hay **dos** 3♦; con 3 mazos hay **tres**.

**Regla confirmada** (`CardService.FindPepinoOroPlayer`):

1. Tras repartir, recorre los jugadores en orden de asiento (índice 0, 1, 2…).
2. El **primer jugador que tenga ≥1 carta 3♦** inicia.
3. Si por un bug nadie tuviera 3♦, inicia el jugador 0.

Si dos (o más) jugadores tienen Pepino de Oro, **no** arrancan todos: arranca solo el de menor índice en la mesa.

La primera jugada de la partida es libre en cuanto a valor (mesa vacía / nueva ronda),  
salvo que más adelante se documente “obligatorio sacar el Pepino” — **hoy no está forzado en código**.

---

## 4. Jerarquía de valores

Para comparar jugadas:

- El **2** es el más bajo en la escala de comparación (comodín de mesa; ver §5).
- Luego: `3 < 4 < 5 < … < 12`.
- El **1 (As)** es el más alto.

En código: comparación `2 → 0`, `1 → 13`, resto = valor facial.

---

## 5. Comodín (2)

Documentación histórica / CLAUDE: “el 2 permite jugada libre en cualquier momento”.

**Código actual:** el 2 solo vale `0` en comparación; **no** está implementado como “siempre jugable contra cualquier cosa”.  
Tratar esto como **deuda**: o se implementa el comodín libre, o se baja de las reglas escritas.

---

## 6. Cómo se juega un turno

1. En tu turno elegís **1…N cartas del mismo valor** (mismo número; el palo puede variar).
2. Si la mesa está vacía o es **nueva ronda** (dio la vuelta y te toca de nuevo como “dueño” de la última jugada): podés tirar cualquier grupo válido.
3. Si hay jugada en mesa:
   - Debés tirar **la misma cantidad** de cartas.
   - El valor debe ser **≥** al valor de la jugada anterior (comparación §4).
4. En lugar de jugar, podés **pasar** (si no es la primera jugada de la ronda / mesa vacía — el backend bloquea pasar sin jugada previa).

---

## 7. PEPINEADO

Si jugás **exactamente la misma combinación** que la anterior:

- mismo **valor**, y  
- misma **cantidad** de cartas,

entonces es **PEPINEADO**: se **salta el turno** del siguiente jugador.

(Detalle de implementación del “salto” vive en `GameHub`; si se ve off-by-one en playtests, se corrige ahí y se anota acá.)

---

## 8. Victoria y fin de partida

- Al quedarte en **0 cartas**, ganás (evento `PlayerWon`).
- Cantidad de ganadores objetivo:
  - **2** si hay ≤4 jugadores en la partida,
  - **3** si hay más de 4.
- La partida termina cuando se alcanza ese cupo de ganadores (`MaxWinners`).

---

## 9. Mazos / modo de juego

- El **creador** de la sala elige 1, 2 o 3 mazos antes de `StartGame`.
- Más mazos ⇒ más cartas en total ⇒ manos más grandes; también **más Pepinos de Oro** (uno por mazo).
- Cálculos históricos de “40 cartas/mazo” en algunos textos están **desactualizados** frente al mazo 1–12 × 4 palos = **48** del `CardService` actual. Preferir 48 en docs nuevas.

---

## 10. Fuente de verdad en el repo

| Qué | Dónde |
|-----|--------|
| Reglas escritas (este archivo) | [`REGLAS_PEPINO.md`](REGLAS_PEPINO.md) |
| Implementación autoritativa | `Back/GameServer/GameServer/Services/CardService.cs`, `Hubs/GameHub.cs` |
| Resumen UI web | `Front/game-client/src/components/Lobby.tsx` |
| Contexto agentes | `CLAUDE.md` (apunta acá) |

---

## 11. Pendientes de reglas / producto (relacionados)

- Alinear comodín 2 (§5) doc ↔ código.
- Texturas Pepino (frente/dorso) en Unity.
- Firebase Auth.
- Mostrar las mismas reglas en Unity (panel tipo Lobby web).
