# Plan de Implementación — ColorDrops (Reinicio limpio)

## Objetivo
Rehacer la lógica del juego desde cero: **simple, legible y mantenible**.
Se conservan solo dos scripts que ya funcionan:
- `DroppyAnimationController.cs` — animación + color visual del modelo (Animator + material).
- `IsometricCameraFollow.cs` — cámara isométrica que sigue al jugador.

## Concepto del juego
Arena isométrica. Controlas a **Droppy** (una gota). Droppy tiene un **color** (Rojo/Amarillo/Azul)
que puedes cambiar con las teclas 1/2/3. Aparecen enemigos (gotas) que te persiguen y atacan.
El combate se rige por un **triángulo de colores**; recoger las gotas que sueltan los enemigos te cura.

## Reglas de color (triángulo)
**Rojo → Amarillo → Azul → Rojo** (cada uno es fuerte contra el siguiente).
- Ventaja (fuerte): daño **x2**
- Desventaja (débil): daño **x0.5**
- Mismo color: **no hace daño** (x0)

## Arquitectura de scripts (todos pequeños)

### Núcleo compartido (jugador y enemigo)
1. **DropletColor** (enum): `Rojo = 0, Amarillo = 1, Azul = 2`. Los valores coinciden con los slices del material.
2. **ColorRules** (clase estática): `Multiplicador(atacante, defensor)` → 2 / 0.5 / 0.
3. **Health** (MonoBehaviour): vida actual, `RecibirDanio()`, `Curar()`, `EstaMuerto`, eventos `AlMorir` / `AlCambiarVida`.
4. **EntityColor** (MonoBehaviour): guarda el `DropletColor` actual. `EstablecerColor()` cambia el color
   **y** avisa a `DroppyAnimationController` para el visual.
   → **Es la única fuente del color** (lógica + visual). Esto evita el desajuste "se ve un color pero
   lógicamente es otro" que teníamos antes. Tanto el jugador como el enemigo llevan este componente.

> Nota: las animaciones de daño (`Hurt`) y muerte (`Die`) las dispara el propio `Health`
> dentro de `RecibirDanio()` (tiene una referencia opcional al `DroppyAnimationController`).
> Se decidió así por simplicidad: menos componentes que arrastrar en el Inspector.

6. **CombatState** (MonoBehaviour): máquina de estados de combate compartida (jugador y enemigo).
   Centraliza los **tiempos de ataque** y el **bloqueo de movimiento**. Estados: `Libre / Atacando / Aturdido / Muerto`.

### Combate por tiempos (mecánica clave)
El ataque **no** aplica daño al instante en que se pulsa, sino en el **punto de conexión**:
- **Punto de conexión = frame 18 a 30 fps = 0.6 s** (`tiempoConexion`). En ese instante `CombatState`
  dispara el evento `AlConectarAtaque`, y el script de combate (jugador o enemigo) aplica el daño.
- Al conectar se **revisa de nuevo el rango**: si el objetivo se movió fuera, el golpe falla → **se puede esquivar**.
- Mientras se **ataca** o se está **aturdido**, `PuedeMoverse == false`: el personaje no se mueve.
  El movimiento se reanuda al terminar la animación de ataque (`duracionAtaque`) o el aturdimiento (`duracionAturdido`).
- **Recibir daño interrumpe el ataque**: si el golpe llega *antes* de la conexión, la corrutina se corta
  y el ataque **no hace daño** (`Interrumpir`).
- **Cadencia de ataque** (`cadenciaAtaque`) tanto para el jugador como para el enemigo.
- **Empuje (knockback):** al conectar un golpe con daño, el atacante llama a `CombatState.Empujar()`
  de la víctima, que retrocede `distanciaEmpuje` en `duracionEmpuje` segundos (desplazamiento del
  transform, coincide con el aturdido). Vale para jugador y enemigo. Los golpes de mismo color (x0)
  no empujan.
- **Guardas de muerte:** un muerto no recibe golpes ni empujes (`PlayerCombat` salta enemigos con
  `EstaMuerto`); los enemigos dejan de perseguir/atacar cuando el **jugador** muere; y un personaje
  muerto no puede moverse ni atacar (`CombatState` pasa a estado `Muerto`).

Parámetros ajustables en el Inspector de `CombatState`: `tiempoConexion` (0.6), `duracionAtaque`
(ajústalo al largo del clip), `cadenciaAtaque`, `duracionAturdido`, `distanciaEmpuje`, `duracionEmpuje`.

### Jugador
5. **PlayerController**: movimiento por transform (matemáticas puras), rota hacia donde avanza,
   avisa la velocidad a la animación, y cambia de color con 1/2/3 (llama a `EntityColor.EstablecerColor`).
   Solo se mueve si `CombatState.PuedeMoverse`.
6. **PlayerCombat**: al pulsar atacar llama a `CombatState.SolicitarAtaque()`; el daño (OverlapSphere +
   multiplicador de color) se aplica en `AlConectarAtaque`.

### Enemigo
7. **EnemyController**: persigue al jugador, ataca al estar cerca (usando `CombatState` para tiempos,
   cadencia e interrupción), usa `Health` + `EntityColor`, y suelta un pickup al morir.
   - **Separación (anti-amontonamiento):** mientras **persigue**, cada enemigo se aparta de los
     enemigos cercanos (estilo boids) usando un registro estático de enemigos vivos. Así **rodean** al
     jugador en vez de encimarse. Parámetros: `radioSeparacion`, `pesoPersecucion`, `pesoSeparacion`.
   - **Plantado en rango:** al alcanzar `rangoAtaque` deja de moverse y solo ataca; ignora los empujes
     de separación (evita el "girar sobre sí mismo" al rodear al jugador).
   - **Aparición:** nace en escala 0, crece al tamaño real (`duracionAparicion`) y queda **inactivo**
     hasta cumplir `esperaInicial` (1 s) — no se mueve ni ataca (da tiempo a verlo llegar).
   - **Muerte:** animación de muerte (`retardoDestruccion`) → encogimiento a escala 0
     (`duracionEncogimiento`) → `Destroy`.

### Sistemas
8. **ArenaBounds**: límites del área jugable (el plano). Va en el objeto del plano y toma el tamaño
   de su Renderer automáticamente. **Todo** movimiento pasa por `LimitarPosicion()`: jugador, enemigos
   y knockback. El spawner también lo usa para no generar fuera.
9. **EnemySpawner** (generación progresiva): tanda inicial de 3 **cerca del centro**; por cada enemigo
   muerto aparecen 2 más, cada tanda **más lejos del centro** (`radioActual += incrementoRadio`, con tope
   en el borde de la arena). Color **aleatorio** por enemigo. Presupuesto total: 60 (`totalMaximo`).
10. **HealthPickup**: al tocarlo el jugador, lo cura (`Health.Curar`) y se destruye.
11. **CameraShake**: sacudida de cámara al recibir daño el jugador (escucha `Health.AlRecibirDanio`).
    No mueve la cámara; expone `OffsetActual` e `IsometricCameraFollow` lo suma sobre su posición base
    (así el `SmoothDamp` no absorbe el temblor).
12. **Límite de cámara al área** (en `IsometricCameraFollow`): la cámara limita su foco para que el
    borde visible del suelo **coincida con el borde del plano** (`ArenaBounds`). Así, al chocar el
    jugador con el límite del plano, la cámara deja de seguirlo y el personaje avanza hasta el borde
    de la pantalla (antes quedaba fijo al centro, sin verse el choque). El alcance visible se **mide**
    proyectando las 4 esquinas del viewport sobre el suelo, y se limita **cada lado por separado**
    (en isométrico se ve más lejos que cerca). Si el plano es más chico que la vista, centra ese eje.
13. **Sistema de pintura** (`PaintCanvas` + `PaintTrail` + `PaintPoison`): los pigmentos dejan un
    rastro permanente de su color sobre el lienzo, y pisar pintura ajena envenena (-1/-2 de vida
    cada 1.5 s, sin animación de daño, solo contracción de escala).
    → Detalle completo en `Plan-Sistema-Pintura.md`.

## Decisión de Input (CONFIRMADA)
Se usa el **New Input System** (estándar de la industria). Se conserva el asset `InputSystem_Actions`
y su clase generada.

### Controles
| Acción | Tecla | Nombre de la acción en el asset |
|---|---|---|
| Mover | **WASD** | `Move` (Vector2) |
| Atacar | **Barra espaciadora** | `Attack` (Button) → rebindear a `<Keyboard>/space` |
| Color Rojo | **1** | `SwitchRed` |
| Color Amarillo | **2** | `SwitchGreen` (nombre heredado; selecciona Amarillo) |
| Color Azul | **3** | `SwitchBlue` |

## Fases de construcción
1. **Input**: ✅ decidido — New Input System. Verificar bindings (WASD / Espacio / 1-2-3).
2. **Núcleo**: `DropletColor`, `ColorRules`, `Health`, `EntityColor`.
3. **Jugador**: ✅ `PlayerController` — WASD (matemáticas puras, sin Rigidbody) + animación + cambio de color 1/2/3. Probar con la cámara.
4. **Combate**: ✅ `PlayerCombat` (ataque en Espacio) + `EnemyController` (persigue/ataca) + daño con multiplicador → probar pelea 1v1.
5. **Sistemas**: ✅ `EnemySpawner` (aparición por tiempo con tope de enemigos vivos) + `HealthPickup` (gota que cura al jugador al tocarla).
6. **UI**: barra de vida y estado (al final).

## Principios
- Un solo dueño por dato (color y vida viven en un componente cada uno).
- Componentes compartidos (`Health`, `EntityColor`) en vez de duplicar lógica.
- Sin herencia ni interfaces innecesarias; MonoBehaviours concretos y directos.
