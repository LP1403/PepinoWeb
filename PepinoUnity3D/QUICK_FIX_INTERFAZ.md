# ⚡ Fix Rápido - Interfaz No Se Ve

## 🎯 Problemas que Reportas

1. ❌ Sala muestra "---" en lugar del ID
2. ❌ No se ven las cartas en tu mano
3. ❌ No hay tablero visible
4. ❌ Interfaz muy básica

## ✅ Soluciones Rápidas (15 minutos)

### Fix 1: Sala No Muestra el ID

**Problema:** El texto muestra "Sala: ---"

**Solución:**

1. **Verifica RoomInfoText:**
   ```
   En Hierarchy: GamePanel → RoomInfoText
   Selecciónalo
   En Inspector:
   - Asegúrate que el componente TextMeshPro esté presente
   - Font Size: 24 o mayor
   - Color: Blanco o visible
   ```

2. **Verifica que GameUI tenga la referencia:**
   ```
   Selecciona GamePanel
   En Inspector → Game UI Script
   Campo "Room Info Text": Debe tener RoomInfoText asignado
   ```

**El GameManager debería actualizar automáticamente este texto cuando te unes.**

### Fix 2: No Se Ven las Cartas

**Problema:** Tus cartas no aparecen en pantalla

**Causas posibles:**

#### Causa A: HandManager mal posicionado

Las cartas pueden estar fuera de la vista de la cámara.

**Solución:**
```
1. Selecciona "HandManager" en Hierarchy
2. En Inspector, Transform:
   - Position: (0, 0, -5)  ← Más cerca de la cámara
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)
```

#### Causa B: Cámara muy lejos

**Solución:**
```
1. Selecciona "Main Camera"
2. Transform:
   - Position: (0, 12, -12)  ← Más cerca
   - Rotation: (35, 0, 0)
```

#### Causa C: CardPrefab no asignado

**Solución:**
```
1. Selecciona "HandManager"
2. En Inspector → Hand Manager Script
3. Campo "Card Prefab": Debe tener el CardPrefab
4. Si está vacío, arrástralo desde Project
```

#### Causa D: Las cartas son invisibles

**Solución:**
```
1. En Project, selecciona "CardPrefab"
2. En Inspector, verifica:
   - Mesh Renderer: ✅ Activo
   - Materials: Debe tener un material asignado
3. Si no tiene material:
   - Arrastra "DefaultCardMaterial" al Mesh Renderer
```

### Fix 3: Crear Mesa Visual

Para ver mejor dónde están las cartas:

**Crear fondo de mesa:**

```
1. GameObject → 3D Object → Plane
2. Nombre: "MesaFondo"
3. Transform:
   - Position: (0, 0, 0)
   - Scale: (3, 1, 3)
4. En Project, crea un material:
   - Create → Material
   - Nombre: "MesaMaterial"
   - Color: Verde oscuro (como mesa de poker)
5. Arrastra "MesaMaterial" al Plane
```

### Fix 4: Verificar Que las Cartas Se Reciben

**Test en Console:**

```
1. Con juego en Play
2. Abre Console (pestaña abajo)
3. Cuando inicies el juego, deberías ver:
   "[HandManager] Creando X cartas en la mano"
   "[HandManager] ..."
```

**Si NO ves estos mensajes:**
- Las cartas no se están creando
- Verifica que el backend esté enviando las cartas
- Verifica que GameManager esté recibiendo el evento

## 🎯 Configuración Óptima Para Empezar

### Cámara:
```
Position: (0, 12, -12)
Rotation: (35, 0, 0)
Field of View: 60
```

### HandManager:
```
Position: (0, 0, -5)
Card Spacing: 1.5
Arc Radius: 8
Arc Angle: 30
```

### TableManager:
```
Position: (0, 0.5, 0)
Table Center: (0, 0.5, 0)
```

### CardPrefab:
```
Scale: (0.7, 1.0, 0.1)
Material: Asignado (cualquier color visible)
Collider: Box Collider presente
Script: Card3DController
```

## 🔍 Debug: ¿Dónde Están las Cartas?

**Test manual:**

1. **Dale Play**
2. **Abre la ventana Scene (junto a Game)**
3. **Con el juego corriendo:**
   - En Scene, busca visualmente las cartas
   - Pueden estar en algún lugar fuera de la cámara
4. **Si las ves en Scene pero no en Game:**
   - Ajusta la cámara para apuntar ahí

## ⚡ Solución Nuclear: Posicionamiento Manual

Si nada funciona, prueba esto:

### Posiciona todo manualmente:

```
Main Camera:
- Position: (0, 10, -10)
- Rotation: (45, 0, 0)
- Look at: (0, 0, 0)

HandManager:
- Position: (0, -2, -8)  ← Bien abajo y cerca
- Esto pone las cartas en primer plano

TableManager:
- Position: (0, 0, 0)  ← Centro absoluto
```

## 🎮 Testing Checklist

Después de ajustar posiciones:

1. **Dale Play ▶️**
2. **Conecta y únete a sala**
3. **Si eres creador, selecciona 1 mazo e inicia**
4. **Mira la Console:**
   ```
   ¿Ves: "[HandManager] Creando X cartas"?
   ¿Ves: "[GameManager] Cartas recibidas: X"?
   ```
5. **Mira la ventana Scene:**
   ```
   ¿Ves objetos de cartas creados?
   ```
6. **Mira la ventana Game:**
   ```
   ¿Ves las cartas en pantalla?
   ```

## 💡 Si Aún No Ves Nada

**Último recurso - Verificar que el juego inicie:**

```
1. En Console (con debug logs activado)
2. Deberías ver al iniciar el juego:
   "[NetworkManager] ✅ Conectado"
   "[GameManager] Inicializando juego"
   "[NetworkManager] 🎴 Cartas recibidas: X"
   "[HandManager] Creando X cartas en la mano"
```

**Si NO ves estos mensajes:**
- El juego no se está iniciando correctamente
- Verifica que seas el creador de la sala
- Verifica que hayas seleccionado un modo de juego
- Verifica que hayas clickeado "Iniciar Juego"

## 📸 Vista Scene vs Game

**Scene View:** Muestra TODO en el mundo 3D (para desarrollo)
**Game View:** Muestra lo que la cámara ve (jugador final)

**Si ves cartas en Scene pero no en Game:**
→ Problema de posición de cámara

**Si no ves cartas en ninguna:**
→ Las cartas no se están creando (problema de código/prefab)

---

**🥒 Empieza ajustando las posiciones de Cámara y HandManager. Esos son los culpables más comunes!**

