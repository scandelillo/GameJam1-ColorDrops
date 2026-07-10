using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del jugador con MATEMÁTICAS PURAS: desplaza el transform directamente en Update,
// sin usar la física del Rigidbody. El input se lee por polling cada frame (más simple y fiable
// que el enfoque por eventos performed/canceled).
//
// Se conserva un Rigidbody (en kinematic) porque los pickups usan OnTriggerEnter, y para que un
// trigger dispare hace falta un Rigidbody en al menos uno de los dos objetos. Aquí no se usa para mover.
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementIsometric : MonoBehaviour
{
    [Header("Movimiento")]
    // Velocidad de desplazamiento en unidades por segundo.
    [SerializeField] private float moveSpeed = 5f;
    // Qué tan rápido gira el personaje para encarar su dirección de avance.
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Referencias")]
    // Cámara usada para que WASD sea relativo a la vista. Si se deja vacío, se toma Camera.main.
    [SerializeField] private Transform cameraTransform;
    // Controlador de animación (idle/walk).
    [SerializeField] private DroppyAnimationController droppy;

    // Asset de input generado.
    private InputSystem_Actions controls;
    // Dirección de movimiento calculada este frame (sobre el plano del suelo).
    private Vector3 moveDirection;

    private void Awake()
    {
        controls = new InputSystem_Actions();

        // Si no se asignó cámara en el Inspector, usamos la principal.
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Dejamos el Rigidbody en kinematic para que la física NO pelee con el movimiento por transform.
        Rigidbody cuerpo = GetComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        // 1. Leemos el input de movimiento (WASD / stick) directamente este frame.
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();

        // 2. Lo convertimos en una dirección 3D relativa a la cámara, aplanada al suelo.
        moveDirection = CalcularDireccionRelativaCamara(input);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            // 3. Movimiento con matemáticas puras: posición += dirección * velocidad * tiempo.
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // 4. Giramos suavemente al personaje hacia donde avanza.
            Quaternion rotacionObjetivo = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, rotationSpeed * Time.deltaTime);
        }

        // 5. Avisamos a la animación la velocidad actual (0 = idle, >0 = caminar).
        if (droppy != null)
            droppy.SetSpeed(moveDirection.magnitude);
    }

    // Proyecta el input 2D sobre los ejes de la cámara (aplanados al suelo) para un control isométrico correcto.
    private Vector3 CalcularDireccionRelativaCamara(Vector2 input)
    {
        // Zona muerta: sin input, sin dirección.
        if (input.sqrMagnitude < 0.01f) return Vector3.zero;

        // Sin cámara de referencia, movemos directamente en los ejes del mundo.
        if (cameraTransform == null)
            return new Vector3(input.x, 0f, input.y).normalized;

        // Tomamos los ejes de la cámara y los aplanamos (ignoramos su inclinación isométrica).
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // input.y = adelante/atrás (W/S); input.x = derecha/izquierda (D/A).
        return (camForward * input.y + camRight * input.x).normalized;
    }
}
