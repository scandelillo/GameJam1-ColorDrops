//using UnityEngine;

//public class CameraFollow : MonoBehaviour
//{
//    [Header("Referencias")]
//    [SerializeField] private Transform target; // El jugador

//    [Header("Configuración")]
//    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -10f); // Distancia y ángulo respecto al jugador
//    [SerializeField] private float smoothSpeed = 3f; // Qué tan suave es el seguimiento

//    // LateUpdate se ejecuta DESPUÉS de todos los Update().
//    // Es crucial para cámaras: así nos aseguramos de que el jugador ya se movió
//    // este frame antes de mover la cámara, evitando que se vea con "tirones" o retrasos raros.
//    private void LateUpdate()
//    {
//        if (target == null) return;

//        // Calculamos dónde debería estar la cámara: la posición del jugador + el offset fijo
//        Vector3 desiredPosition = target.position + offset;

//        // Interpolamos suavemente entre la posición actual de la cámara y la deseada
//        // (en vez de saltar instantáneamente, se siente más natural)
//        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
//    }
//}


using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;

    [Header("Distancia")]
    [SerializeField] private float distance = 20f;
    [SerializeField] private float height = 8f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 0.2f;

    private InputSystem_Actions controls;

    private Vector2 lookInput;
    private float currentYaw;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        controls.Player.Look.performed += OnLookPerformed;
        controls.Player.Look.canceled += OnLookCanceled;
    }

    private void OnDisable()
    {
        controls.Player.Look.performed -= OnLookPerformed;
        controls.Player.Look.canceled -= OnLookCanceled;

        controls.Player.Disable();
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext ctx)
    {
        lookInput = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Rotación horizontal usando el movimiento del mouse
        currentYaw += lookInput.x * rotationSpeed;

        Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 offset = rotation * new Vector3(0f, height, -distance);

        transform.position = target.position + offset;

        transform.LookAt(target.position);
    }
}