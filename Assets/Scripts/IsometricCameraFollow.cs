using UnityEngine;

// Cámara isométrica que sigue al jugador SIN rotar nunca.
// El ángulo de visión es fijo (se calcula una vez a partir de pitch/yaw);
// la cámara solo se traslada para mantener al objetivo encuadrado.
public class IsometricCameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    // Transform al que sigue la cámara (normalmente el jugador).
    [SerializeField] private Transform target;

    [Header("Ángulo isométrico (fijo, no rota en runtime)")]
    // Inclinación vertical de la cámara hacia abajo. 45 mantiene el encuadre anterior; ~30 es el look isométrico clásico.
    [SerializeField] private float pitch = 45f;
    // Giro horizontal fijo. 0 = vista frontal (como estaba antes); 45 = vista isométrica en diagonal.
    [SerializeField] private float yaw = 0f;

    [Header("Distancia y encuadre")]
    // Qué tan lejos se coloca la cámara del objetivo a lo largo de su eje de visión (súbelo para alejar).
    [SerializeField] private float distance = 15f;
    // Desplazamiento del punto al que apunta (útil para encuadrar un poco por encima del jugador).
    [SerializeField] private Vector3 lookOffset = Vector3.zero;

    [Header("Suavizado")]
    // Tiempo de amortiguación del seguimiento. 0 = pegado al jugador; valores mayores = más suave/rezagado.
    [SerializeField] private float followSmoothTime = 0.15f;

    // Rotación isométrica fija, calculada una sola vez. Nunca cambia durante el juego.
    private Quaternion fixedRotation;
    // Velocidad interna que SmoothDamp necesita conservar entre frames.
    private Vector3 currentVelocity;

    private void Awake()
    {
        // Calculamos la orientación isométrica una vez; el resto del juego solo trasladamos la cámara.
        fixedRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnValidate()
    {
        // Recalcula el ángulo al ajustar pitch/yaw desde el Inspector, para verlo al instante.
        fixedRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void Start()
    {
        if (target == null) return;

        // Encuadramos la cámara ya en su sitio en el primer frame, sin deslizamiento inicial.
        transform.rotation = fixedRotation;
        transform.position = GetDesiredPosition();
    }

    // LateUpdate: se ejecuta después de que el jugador ya se movió este frame.
    private void LateUpdate()
    {
        if (target == null) return;

        // Seguimiento suave de la posición; la rotación se mantiene fija (nunca orbita).
        transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref currentVelocity, followSmoothTime);
        transform.rotation = fixedRotation;
    }

    // Punto donde debería estar la cámara para mantener al objetivo centrado con el ángulo fijo.
    private Vector3 GetDesiredPosition()
    {
        Vector3 focusPoint = target.position + lookOffset;
        // Retrocedemos "distance" a lo largo del eje de visión de la cámara.
        return focusPoint - fixedRotation * Vector3.forward * distance;
    }
}
