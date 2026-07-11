using UnityEngine;

// Control del jugador:
//  - Movimiento con WASD usando MATEMÁTICAS PURAS (se mueve el transform directamente, sin Rigidbody).
//  - Cambio de color con las teclas 1/2/3.
// El input se lee con el New Input System (InputSystem_Actions).
[RequireComponent(typeof(EntityColor))]
[RequireComponent(typeof(CombatState))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    // Velocidad de desplazamiento en unidades por segundo.
    [SerializeField] private float velocidad = 5f;
    // Qué tan rápido gira el personaje hacia su dirección de avance.
    [SerializeField] private float velocidadGiro = 12f;

    [Header("Referencias")]
    // Visual del modelo (animación idle/caminar). Suele estar en el hijo.
    [SerializeField] private DroppyAnimationController visual;

    // Asset de input generado (New Input System).
    private InputSystem_Actions controles;
    // Componente de color, para cambiarlo con 1/2/3.
    private EntityColor color;
    // Máquina de estados: nos dice si el jugador puede moverse ahora.
    private CombatState combate;
    // Dirección de movimiento calculada este frame.
    private Vector3 direccion;

    private void Awake()
    {
        // Creamos el input y buscamos referencias.
        controles = new InputSystem_Actions();
        color = GetComponent<EntityColor>();
        combate = GetComponent<CombatState>();

        // Si no se asignó el visual a mano, lo buscamos en este objeto o sus hijos.
        if (visual == null)
            visual = GetComponentInChildren<DroppyAnimationController>();
    }

    private void OnEnable() => controles.Player.Enable();
    private void OnDisable() => controles.Player.Disable();
    private void OnDestroy() => controles?.Dispose();

    private void Update()
    {
        Mover();
        LeerCambioDeColor();
    }

    // Movimiento con matemáticas puras: desplaza el transform según la entrada.
    private void Mover()
    {
        // Si está atacando, aturdido o muerto, no se mueve (queda quieto).
        if (!combate.PuedeMoverse)
        {
            if (visual != null)
                visual.SetSpeed(0f);
            return;
        }

        // Leemos WASD como vector 2D (x = izquierda/derecha, y = adelante/atrás).
        Vector2 entrada = controles.Player.Move.ReadValue<Vector2>();

        // Lo convertimos en una dirección sobre el plano del suelo (ejes del mundo).
        direccion = new Vector3(entrada.x, 0f, entrada.y);
        // Normalizamos si supera 1 para no ir más rápido en diagonal.
        if (direccion.sqrMagnitude > 1f)
            direccion.Normalize();

        // Desplazamos la posición: posición += dirección * velocidad * tiempo.
        // El resultado se limita al área jugable (el plano).
        transform.position = ArenaBounds.LimitarPosicion(transform.position + direccion * velocidad * Time.deltaTime);

        // Si nos movemos, giramos suavemente para encarar la dirección de avance.
        if (direccion.sqrMagnitude > 0.0001f)
        {
            Quaternion giroObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, giroObjetivo, velocidadGiro * Time.deltaTime);
        }

        // Avisamos la velocidad a la animación (0 = idle, >0 = caminar).
        if (visual != null)
            visual.SetSpeed(direccion.magnitude);
    }

    // Cambio de color con las teclas 1/2/3 (WasPressedThisFrame = solo el frame de la pulsación).
    private void LeerCambioDeColor()
    {
        // 1 -> Rojo, 2 -> Amarillo (acción heredada "SwitchGreen"), 3 -> Azul.
        if (controles.Player.SwitchRed.WasPressedThisFrame()) color.EstablecerColor(DropletColor.Rojo);
        if (controles.Player.SwitchGreen.WasPressedThisFrame()) color.EstablecerColor(DropletColor.Amarillo);
        if (controles.Player.SwitchBlue.WasPressedThisFrame()) color.EstablecerColor(DropletColor.Azul);
    }
}
