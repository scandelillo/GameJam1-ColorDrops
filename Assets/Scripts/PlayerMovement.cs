using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private InputSystem_Actions controls;
    private Vector2 inputVector;   // Guarda el input crudo (x, y) que viene del Input System
    private Vector3 moveDirection; // Guarda la dirección final ya convertida a espacio del mundo, según la cámara

    // Awake se ejecuta una sola vez, apenas el objeto es creado/cargado en la escena,
    // antes que cualquier Start() o Update(). Obtenemos componentes e instanciamos el Input System.
    private void Awake()
    {
        // Guardamos la referencia al Rigidbody del jugador para poder moverlo por física más adelante
        rb = GetComponent<Rigidbody>();

        // Creamos una instancia de la clase de Input Actions generada automáticamente
        // (esto NO habilita los controles todavía, solo los prepara)
        controls = new InputSystem_Actions();

        // Si no arrastraste una cámara manualmente en el Inspector, buscamos la Main Camera automáticamente
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

   
    private void OnEnable()
    {
        controls.Player.Enable(); // Activa el Action Map "Player" (sin esto, los inputs no se leen)

        // Nos suscribimos: cuando la acción "Move" se dispara (tecla presionada/mantenida),
        // se ejecuta el método OnMovePerformed
        controls.Player.Move.performed += OnMovePerformed;

        // Cuando se suelta la tecla (o el input vuelve a cero), se ejecuta OnMoveCanceled
        controls.Player.Move.canceled += OnMoveCanceled;
    }


    private void OnDisable()
    {
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;
        controls.Player.Disable(); // Apaga el Action Map cuando el objeto ya no está activo
    }

    // Este método se ejecuta automáticamente cada vez que el input "Move" tiene un valor
    // (por ejemplo, mientras mantienes presionada una tecla de WASD)
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        // Leemos el valor del input como un Vector2 (x = horizontal, y = vertical)
        // WASD: W/S mueven el eje Y, A/D mueven el eje X
        inputVector = ctx.ReadValue<Vector2>();
    }

    // Este método se ejecuta cuando se suelta la tecla (el input vuelve a "cero")
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        inputVector = Vector2.zero; // El jugador deja de moverse
    }

    // Update se ejecuta una vez por frame. Aquí calculamos HACIA DÓNDE se debe mover el personaje,
    // pero todavía no lo movemos (eso pasa en FixedUpdate).
    private void Update()
    {
        // Tomamos los vectores "adelante" y "derecha" de la cámara
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // Ponemos la componente Y en 0 para que la inclinación de la cámara isométrica
        // no afecte el movimiento (si no hacemos esto, el personaje intentaría moverse
        // hacia arriba/abajo según el ángulo de la cámara)
        camForward.y = 0f;
        camRight.y = 0f;

        // Normalizamos para que la magnitud del vector sea siempre 1
        // (evita que el personaje se mueva más rápido en diagonal)
        camForward.Normalize();
        camRight.Normalize();

        // Combinamos el input del jugador con la orientación de la cámara:
        // - inputVector.y (W/S) se aplica sobre el "adelante" de la cámara
        // - inputVector.x (A/D) se aplica sobre el "derecha" de la cámara
        // El resultado es la dirección real en la que se debe mover el personaje en el mundo
        moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;

        // Si el personaje se está moviendo, lo rotamos suavemente para que "mire" hacia
        // la dirección de movimiento (si no hay movimiento, se queda mirando donde estaba)
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Slerp interpola suavemente entre la rotación actual y la deseada,
            // en vez de rotar instantáneamente (se ve más natural)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    // FixedUpdate se ejecuta a intervalos de tiempo fijos (no depende del framerate).
    // Es el lugar correcto para mover objetos con Rigidbody, porque el motor de físicas
    // también trabaja en estos intervalos fijos.
    private void FixedUpdate()
    {
        // Calculamos la nueva posición: posición actual + dirección * velocidad * tiempo
        Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        // MovePosition mueve el Rigidbody respetando colisiones y físicas
        // (a diferencia de mover el transform directamente, que las ignora)
        rb.MovePosition(targetPosition);
    }
}