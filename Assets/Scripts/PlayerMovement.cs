using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    [Header("Animación")]
    [SerializeField] private DroppyAnimationController droppy;

    private Rigidbody rb;
    private InputSystem_Actions controls;
    private Vector2 inputVector;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new InputSystem_Actions();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;
        controls.Player.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        inputVector = ctx.ReadValue<Vector2>();
        Debug.Log($"Input recibido: {inputVector}"); // Debug para verificar
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        inputVector = Vector2.zero;
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        // Obtener direcciones de la cámara
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // Proyectar en el plano horizontal (Y=0)
        camForward.y = 0f;
        camRight.y = 0f;

        // Normalizar para mantener la magnitud correcta
        camForward.Normalize();
        camRight.Normalize();

        // IMPORTANTE: inputVector.y es adelante/atrás (W/S), inputVector.x es izquierda/derecha (A/D)
        moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;

        // Debug para verificar la dirección
        Debug.Log($"Dirección de movimiento: {moveDirection}");

        // Rotación del personaje
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // Actualizar animación
        if (droppy != null)
        {
            droppy.SetSpeed(moveDirection.magnitude);
        }
    }

    private void FixedUpdate()
    {
        if (moveDirection.magnitude > 0.01f)
        {
            Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
    }
}