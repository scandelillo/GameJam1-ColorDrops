using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementIsometric : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Referencias")]
    [SerializeField] private DroppyAnimationController droppy;

    [Header("Opciones de Movimiento")]
    [SerializeField] private bool useWorldSpace = true; // true = independiente de cámara, false = relativo a cámara
    [SerializeField] private bool useRigidbody = true; // true = usar físicas, false = mover transform directamente

    // Componentes
    private Rigidbody rb;
    private InputSystem_Actions controls;

    // Variables de input
    private Vector2 inputVector;
    private Vector3 moveDirection;

    // Variables para movimiento suave
    private Vector3 smoothMoveVelocity;
    [SerializeField] private float smoothTime = 0.1f;

    private void Awake()
    {
        // Obtener componentes
        rb = GetComponent<Rigidbody>();
        controls = new InputSystem_Actions();

        // Si no hay referencia al controlador de animación, intentar encontrarlo
        if (droppy == null)
        {
            droppy = GetComponent<DroppyAnimationController>();
        }
    }

    private void OnEnable()
    {
        // Activar el sistema de input
        controls.Player.Enable();

        // Suscribirse a eventos de movimiento
        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        // Desuscribirse y desactivar input
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;
        controls.Player.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        inputVector = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        inputVector = Vector2.zero;
    }

    private void Update()
    {
        // Calcular dirección de movimiento
        CalculateMoveDirection();

        // Rotar el personaje hacia la dirección de movimiento
        RotateCharacter();

        // Actualizar animación
        UpdateAnimation();

        // Debug (opcional)

    }

    private void FixedUpdate()
    {
        // Aplicar movimiento en FixedUpdate para consistencia con físicas
        ApplyMovement();
    }

    private void CalculateMoveDirection()
    {
        if (inputVector.magnitude < 0.01f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (useWorldSpace)
        {
            // MOVIMIENTO INDEPENDIENTE DE CÁMARA (MUNDO)
            // En vista isométrica, queremos que:
            // - W (inputVector.y > 0) = mover hacia arriba en el mundo (eje Z positivo)
            // - S (inputVector.y < 0) = mover hacia abajo en el mundo (eje Z negativo)
            // - A (inputVector.x < 0) = mover hacia la izquierda (eje X negativo)
            // - D (inputVector.x > 0) = mover hacia la derecha (eje X positivo)

            moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

            // Normalizar para mantener velocidad constante en diagonales
            moveDirection.Normalize();
        }
        else
        {
            // MOVIMIENTO RELATIVO A LA CÁMARA (opcional)
            // Esto usa la orientación de la cámara para el movimiento
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;

            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;

                camForward.y = 0f;
                camRight.y = 0f;

                camForward.Normalize();
                camRight.Normalize();

                moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;
            }
            else
            {
                // Fallback a movimiento en mundo si no hay cámara
                moveDirection = new Vector3(inputVector.x, 0f, inputVector.y).normalized;
            }
        }
    }

    private void RotateCharacter()
    {
        // Solo rotar si hay movimiento significativo
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Rotación suave hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        if (useRigidbody && rb != null)
        {
            // Usar Rigidbody para movimiento con físicas
            Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Movimiento directo del transform (sin físicas)
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void UpdateAnimation()
    {
        if (droppy != null)
        {
            // Pasar la velocidad al controlador de animación
            float speed = moveDirection.magnitude;
            droppy.SetSpeed(speed);
        }
    }

    private void DebugInput()
    {
        // Mostrar información de debug en consola (opcional)
        if (inputVector.magnitude > 0.1f)
        {
            Debug.Log($"Input: {inputVector} | Dirección: {moveDirection} | Velocidad: {moveDirection.magnitude * moveSpeed}");
        }
    }

    // Métodos públicos para controlar el movimiento desde otros scripts
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetMovementMode(bool worldSpace)
    {
        useWorldSpace = worldSpace;
    }

    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    public bool IsMoving()
    {
        return moveDirection.sqrMagnitude > 0.01f;
    }
}