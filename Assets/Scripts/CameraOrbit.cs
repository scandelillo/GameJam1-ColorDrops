using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target; // El jugador

    [Header("Distancia y ángulo")]
    [SerializeField] private float distance = 15f; // Qué tan lejos está la cámara del jugador (súbelo para alejar más)
    [SerializeField] private float pitch = 45f;     // Ángulo de inclinación hacia abajo (el "look" isométrico clásico)
    [SerializeField] private float smoothSpeed = 10f; // Qué tan suave sigue la posición del jugador

    [Header("Rotación con mouse")]
    [SerializeField] private float rotationSpeed = 3f; // Qué tan rápido gira la cámara al mover el mouse

    private float currentYaw = 0f; // Ángulo de rotación horizontal acumulado alrededor del jugador
    private InputSystem_Actions controls;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    // LateUpdate: igual que antes, para asegurarnos de que el jugador ya se movió este frame
    private void LateUpdate()
    {
        if (target == null) return;

        // Leemos el delta del mouse (x = movimiento horizontal) desde la Action "Look" del asset
        Vector2 lookDelta = controls.Player.Look.ReadValue<Vector2>();

        // Acumulamos la rotación horizontal según el movimiento del mouse
        currentYaw += lookDelta.x * rotationSpeed * Time.deltaTime;

        // Construimos una rotación combinando el pitch fijo (inclinación) y el yaw acumulado (giro libre)
        Quaternion rotation = Quaternion.Euler(pitch, currentYaw, 0f);

        // Aplicamos esa rotación a un vector "hacia atrás" con la distancia deseada,
        // esto nos da el punto exacto donde debe estar la cámara, orbitando al jugador
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Siempre miramos hacia el jugador, sin importar cómo se haya movido la cámara
        transform.LookAt(target.position);
    }
}