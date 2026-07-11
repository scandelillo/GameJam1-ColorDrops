using UnityEngine;

// Ataque del jugador: al pulsar la tecla de ataque (Barra espaciadora) pide un
// ataque a CombatState. El daño se aplica en el punto de conexión (frame 18),
// golpeando a los enemigos dentro de un radio según el triángulo de color.
[RequireComponent(typeof(EntityColor))]
[RequireComponent(typeof(CombatState))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Ataque")]
    // Radio del golpe alrededor del jugador.
    [SerializeField] private float rangoAtaque = 2f;
    // Daño base antes de aplicar el multiplicador de color.
    [SerializeField] private float danioBase = 15f;
    // Capa(s) donde están los enemigos (para no golpearse a sí mismo).
    [SerializeField] private LayerMask capaEnemigos;

    // Input generado (New Input System).
    private InputSystem_Actions controles;
    // Color del jugador (para el multiplicador).
    private EntityColor color;
    // Máquina de estados: gestiona tiempos, bloqueo de movimiento e interrupción.
    private CombatState combate;

    private void Awake()
    {
        controles = new InputSystem_Actions();
        color = GetComponent<EntityColor>();
        combate = GetComponent<CombatState>();
    }

    private void OnEnable()
    {
        controles.Player.Enable();
        // El golpe se aplica cuando la animación conecta, no al pulsar.
        combate.AlConectarAtaque += Golpear;
    }

    private void OnDisable()
    {
        controles.Player.Disable();
        combate.AlConectarAtaque -= Golpear;
    }

    private void OnDestroy() => controles?.Dispose();

    private void Update()
    {
        // Pedimos el ataque en el frame en que se pulsa la tecla.
        // CombatState decide si procede (cadencia y estado) y anima.
        if (controles.Player.Attack.WasPressedThisFrame())
            combate.SolicitarAtaque();
    }

    // Se llama en el punto de conexión del ataque (frame 18): aplica el daño.
    private void Golpear()
    {
        // Buscamos enemigos dentro del rango (filtrando por su capa).
        Collider[] golpeados = Physics.OverlapSphere(transform.position, rangoAtaque, capaEnemigos);

        foreach (Collider objetivo in golpeados)
        {
            // El enemigo debe tener color (EntityColor) y vida (Health).
            EntityColor colorEnemigo = objetivo.GetComponent<EntityColor>();
            Health vidaEnemigo = objetivo.GetComponent<Health>();
            if (colorEnemigo == null || vidaEnemigo == null) continue;

            // Si ya está muerto, no se le pega ni se le empuja (dejarlo agonizar en paz).
            if (vidaEnemigo.EstaMuerto) continue;

            // Daño según el triángulo: x2 fuerte, x0.5 débil, x0 mismo color.
            float mult = ColorRules.Multiplicador(color.ColorActual, colorEnemigo.ColorActual);
            // Mismo color: ni daño ni empuje.
            if (mult <= 0f) continue;

            vidaEnemigo.RecibirDanio(danioBase * mult);

            // Empujamos al enemigo hacia atrás (lejos del jugador).
            CombatState combateEnemigo = objetivo.GetComponent<CombatState>();
            if (combateEnemigo != null)
                combateEnemigo.Empujar(objetivo.transform.position - transform.position);
        }
    }

    // Dibuja el rango de ataque en el editor al seleccionar el jugador.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
