# Plan de Implementación — Sistema de Pintura (trail + veneno)

## Objetivo
El juego son **pigmentos sobre un lienzo**: jugador y enemigos dejan un **rastro de pintura**
permanente por donde pasan, el lienzo se va llenando de color, y pisar pintura ajena
**envenena**. El rastro del jugador cambia junto con su color (1/2/3).

## Reglas de la mecánica
1. **Rastro permanente**: cada pigmento pinta el suelo por donde pasa. Si alguien pasa
   por encima de pintura existente, el color nuevo **reemplaza** al viejo.
2. **Veneno por pintura ajena** (tic cada **1.5 s**):
   - Pintura que **nos hace counter** (fuerte contra nosotros): **-2 de vida**.
   - Pintura contra la que somos fuertes (relación a la mitad): **-1 de vida**.
   - Pintura propia o lienzo sin pintar: **nada**.
3. **Feedback del veneno**: NO se usa la animación de daño. Solo una **contracción de
   escala** (se encoge un poco y vuelve). El veneno tampoco interrumpe ataques, no
   empuja y no sacude la cámara.
4. **El trail del jugador cambia** al cambiar su color (se lee `EntityColor` en cada pincelada).

## Arquitectura (3 scripts nuevos + 1 método en Health)

### PaintCanvas — el lienzo (va en el objeto del plano, junto a `ArenaBounds`)
Mantiene **dos representaciones sincronizadas** del mismo dato:
- **Rejilla lógica** (`sbyte[]`): -1 = sin pintar, 0/1/2 = `DropletColor`.
  → La consulta el veneno (`TryObtenerColor`). Es la **fuente de verdad**.
- **`Texture2D`** que se muestra en un **quad overlay propio** (ver abajo).
  → Es lo que se ve. Se sube a GPU **una vez por frame** solo si hubo pinceladas (`sucio`).

**Decisión clave 1 — mundo → texel por el AABB del plano:** para saber qué texel corresponde
a una posición del mundo se normaliza la posición dentro del **rectángulo XZ del plano**
(`Renderer.bounds`): X del mundo → U, Z del mundo → V. Se descartó el enfoque por raycast +
`hit.textureCoord` porque **falla en silencio** (devuelve (0,0) si el mesh no tiene
Read/Write) → toda la pintura caía en un píxel de la esquina y no se veía nada.

**Decisión clave 2 — quad overlay propio en vez del material del plano:** al principio la
textura se asignaba al material del plano (`mainTexture`/`_BaseMap`), pero el rastro salía
**desalineado** (las UVs reales del plano no coincidían con el mapeo X→U, Z→V → pintura
espejeada, que en los bordes se ve "muy lejos" del personaje). La solución: `PaintCanvas`
genera en runtime su **propio quad** (`CrearOverlay`) sobre el rectángulo del plano, con UVs
que YO controlo (X→U, Z→V) y el MISMO mapeo que al pintar → alineación exacta garantizada.
El quad va a `alturaOverlay` (0.02) por encima del suelo, con material `Sprites/Default`
(unlit, transparente, doble cara, siempre incluido en build). **Ventaja extra:** ya no depende
del material ni de las UVs del plano → funciona con cualquier shader (incluso Shader Graph).
Asume el plano **sin rotar** (igual que `ArenaBounds`).

API: `Pintar(posicionMundo, color)` (círculo de `radioPincel`) y
`TryObtenerColor(posicionMundo, out color)`.

### PaintTrail — el rastro (va en jugador Y enemigo)
- Deja una pincelada cada `distanciaEntrePinceladas` (0.35) unidades recorridas.
- Lee `EntityColor.ColorActual` **en cada pincelada** → el trail del jugador cambia solo.
- Los muertos no pintan.

**Decisión clave — se pinta DETRÁS, no debajo** (`retrasoPincel` = 0.6): si se pintara bajo
los pies, el propio trail taparía al instante la pintura ajena que se está pisando y el
veneno **nunca detectaría nada**. Pintando detrás, el suelo bajo los pies conserva el color
ajeno mientras se cruza. Bonus: el trazo arrancando detrás del personaje se ve natural.

### PaintPoison — el veneno (va en jugador Y enemigo)
- **Basado en estado, no en reloj global**: cada frame muestrea la pintura bajo los pies.
  Al **pisar** pintura ajena hace un tic **inmediato**; mientras **siga encima**, otro tic
  cada `intervalo` (1.5 s); al **salir** (o igualar el color) se reinicia y la próxima
  pisada vuelve a doler al instante. Esto evita el problema del reloj global (parecía que
  el veneno "solo pega una vez": el primer tic tardaba hasta 1.5 s y para entonces ya
  habías salido de la pintura ajena, en parte porque tu propio trail la va cubriendo).
- Relación de color: `ColorRules.Multiplicador(pintura, miColor)` → x2 = counter (**-2**),
  x0.5 = relación débil (**-1**), x0 = pintura propia (nada).
- Aplica el daño con `Health.RecibirDanioVeneno` (silencioso) y dispara DOS feedbacks:
  1. **Contracción de escala**: corrutina que encoge a `escalaContraccion` (0.85) y restaura
     la escala EXACTA de partida en `duracionContraccion` (0.22 s). Si la entidad muere a
     medias, se restaura y se corta (la animación de muerte manda sobre la escala). Como el
     intervalo (1.5) es mucho mayor que la contracción (0.22), nunca se encadenan dos.
  2. **Destello de color** (`DroppyAnimationController.FlashDamage`): baja un poquito el
     `_Blend` hacia un color vecino y vuelve, SIN cambiar el color lógico. Es sutil
     (`damageFlashDepth` 0.3, `damageFlashDuration` 0.18). Un cambio de color real
     (1/2/3) tiene prioridad y corta el destello (`StopDamageFlash`).

### Health.RecibirDanioVeneno — daño silencioso
Baja la vida y gestiona la muerte **igual** que el daño normal (dispara `AlMorir` y la
animación de muerte), pero **no** reproduce la animación de daño **ni** dispara
`AlRecibirDanio`. Consecuencia deliberada: el veneno no interrumpe el ataque
(`CombatState.Interrumpir` escucha `AlRecibirDanio`), no empuja y no sacude la cámara
(`CameraShake` también escucha ese evento). `AlCambiarVida` SÍ se dispara (para la UI).

## Montaje en Unity
1. **Plano** (el de `ArenaBounds`): añadir `PaintCanvas`. Ya **no importa** el material del
   plano: la pintura se dibuja en un quad overlay propio que el script crea encima. El fondo
   del lienzo es opaco blanco (`colorFondo`); si quieres que se vea tu plano por debajo, pon
   el **alfa de `colorFondo` en 0** (fondo transparente, solo se ven las pinceladas).
2. **Player.prefab**: añadir `PaintTrail` + `PaintPoison`.
3. **Enemy.prefab**: añadir `PaintTrail` + `PaintPoison`.

## Parámetros ajustables
| Script | Parámetro | Valor | Qué controla |
|---|---|---|---|
| PaintCanvas | `resolucion` | 256 | Nitidez del trazo (512 = más fino, más costo) |
| PaintCanvas | `radioPincel` | 0.45 | Grosor del trail |
| PaintCanvas | `pinturaRojo/Amarillo/Azul` | — | Paleta de la pintura |
| PaintTrail | `distanciaEntrePinceladas` | 0.35 | Continuidad del trazo (< diámetro del pincel) |
| PaintTrail | `retrasoPincel` | 0.6 | Qué tan atrás cae la pincelada |
| PaintPoison | `intervalo` | 1.5 | Cadencia del veneno |
| PaintPoison | `danioDebil` / `danioFuerte` | 1 / 2 | Vida por tic según la relación |
| PaintPoison | `escalaContraccion` / `duracionContraccion` | 0.85 / 0.22 | Feedback visual |

## Posibles partes siguientes (la misión continúa)
- Manchas especiales: salpicadura al morir un enemigo (splat grande de su color).
- Porcentaje del lienzo pintado por color (medidor / condición de victoria).
- Curarse al caminar sobre pintura propia, o VFX/SFX por pincelada.
