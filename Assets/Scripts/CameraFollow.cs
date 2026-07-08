using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target; // El jugador

    [Header("Configuración")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -10f); // Distancia y ángulo respecto al jugador
    [SerializeField] private float smoothSpeed = 3f; // Qué tan suave es el seguimiento

    // LateUpdate se ejecuta DESPUÉS de todos los Update().
    // Es crucial para cámaras: así nos aseguramos de que el jugador ya se movió
    // este frame antes de mover la cámara, evitando que se vea con "tirones" o retrasos raros.
    private void LateUpdate()
    {
        if (target == null) return;

        // Calculamos dónde debería estar la cámara: la posición del jugador + el offset fijo
        Vector3 desiredPosition = target.position + offset;

        // Interpolamos suavemente entre la posición actual de la cámara y la deseada
        // (en vez de saltar instantáneamente, se siente más natural)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}