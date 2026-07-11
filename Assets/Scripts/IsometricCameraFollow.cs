using UnityEngine;

// Cámara isométrica que sigue al jugador SIN rotar nunca.
// El ángulo de visión es fijo (se calcula una vez a partir de pitch/yaw);
// la cámara solo se traslada para mantener al objetivo encuadrado.
// Además, limita su recorrido al área del plano (ArenaBounds): al llegar el
// borde visible al borde del plano, la cámara se detiene y el jugador puede
// seguir avanzando hasta el borde de la pantalla (así coinciden ambos límites).
[RequireComponent(typeof(Camera))]
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

    [Header("Límite al área del plano")]
    // Si está activo, la cámara no muestra más allá del borde del plano (ArenaBounds):
    // deja de seguir al jugador cuando el borde visible toca el borde del plano.
    [SerializeField] private bool limitarAlArea = true;
    // Cámara usada para medir qué porción de suelo se ve (auto: la de este objeto).
    [SerializeField] private Camera camara;

    [Header("Sacudida (opcional)")]
    // Componente de sacudida. Si está, su desplazamiento se suma ENCIMA del seguimiento.
    [SerializeField] private CameraShake shake;

    // Rotación isométrica fija, calculada una sola vez. Nunca cambia durante el juego.
    private Quaternion fixedRotation;
    // Velocidad interna que SmoothDamp necesita conservar entre frames.
    private Vector3 currentVelocity;
    // Posición del seguimiento SIN sacudida (para que el SmoothDamp no absorba el shake).
    private Vector3 basePosition;

    // Alcance de la vista sobre el suelo (desde el centro de la toma hasta cada borde
    // visible). Se mide una vez porque la forma no cambia si la altura y el ángulo son fijos.
    private bool alcancesListos;
    private float alcanceMenosX, alcanceMasX, alcanceMenosZ, alcanceMasZ;

    private void Awake()
    {
        // Calculamos la orientación isométrica una vez; el resto del juego solo trasladamos la cámara.
        fixedRotation = Quaternion.Euler(pitch, yaw, 0f);

        // Cámara para medir el área visible (normalmente en este mismo objeto).
        if (camara == null)
            camara = GetComponent<Camera>();

        // Si no se asignó a mano, buscamos la sacudida en esta misma cámara.
        if (shake == null)
            shake = GetComponent<CameraShake>();
    }

    private void OnValidate()
    {
        // Recalcula el ángulo al ajustar pitch/yaw desde el Inspector, para verlo al instante.
        fixedRotation = Quaternion.Euler(pitch, yaw, 0f);
        // Al cambiar distancia/ángulo cambia el área visible: forzamos que se vuelva a medir.
        alcancesListos = false;
    }

    private void Start()
    {
        if (target == null) return;

        transform.rotation = fixedRotation;

        // Primero colocamos la cámara SIN límites, para que quede a su altura real:
        // así la medición del área visible (que depende de la altura) sale correcta.
        Vector3 focoInicial = target.position + lookOffset;
        transform.position = focoInicial - fixedRotation * Vector3.forward * distance;

        // Posición final, ya con los límites del área aplicados (sin deslizamiento inicial).
        basePosition = GetDesiredPosition();
        transform.position = basePosition;
    }

    // LateUpdate: se ejecuta después de que el jugador ya se movió este frame.
    private void LateUpdate()
    {
        if (target == null) return;

        // Seguimiento suave de la posición BASE (sin sacudida), para que el SmoothDamp
        // no se coma el shake al suavizar.
        basePosition = Vector3.SmoothDamp(basePosition, GetDesiredPosition(), ref currentVelocity, followSmoothTime);

        // Sumamos el desplazamiento de la sacudida encima de la posición base.
        Vector3 desplazamiento = shake != null ? shake.OffsetActual : Vector3.zero;
        transform.position = basePosition + desplazamiento;
        transform.rotation = fixedRotation;
    }

    // Punto donde debería estar la cámara para mantener al objetivo encuadrado,
    // pero con el foco LIMITADO al área del plano.
    private Vector3 GetDesiredPosition()
    {
        Vector3 focusPoint = target.position + lookOffset;
        focusPoint = LimitarFoco(focusPoint);
        // Retrocedemos "distance" a lo largo del eje de visión de la cámara.
        return focusPoint - fixedRotation * Vector3.forward * distance;
    }

    // Limita el punto que la cámara centra, de modo que el área visible del suelo
    // no rebase el borde del plano. Cada lado se limita por separado porque en
    // isométrico se ve más "lejos" que "cerca".
    private Vector3 LimitarFoco(Vector3 foco)
    {
        ArenaBounds arena = ArenaBounds.Instancia;
        if (!limitarAlArea || arena == null) return foco;

        // Medimos (una sola vez) cuánto suelo abarca la cámara por cada lado.
        if (!alcancesListos) CalcularAlcances();
        if (!alcancesListos) return foco;

        Vector3 centro = arena.Centro;
        Vector2 mitad = arena.MitadArea;

        // Rango permitido del foco: el borde del plano menos lo que la cámara ve a cada lado.
        float minX = centro.x - mitad.x + alcanceMenosX;
        float maxX = centro.x + mitad.x - alcanceMasX;
        float minZ = centro.z - mitad.y + alcanceMenosZ;
        float maxZ = centro.z + mitad.y - alcanceMasZ;

        // Si el plano es más chico que lo que se ve en un eje, centramos en ese eje.
        foco.x = minX <= maxX ? Mathf.Clamp(foco.x, minX, maxX) : centro.x;
        foco.z = minZ <= maxZ ? Mathf.Clamp(foco.z, minZ, maxZ) : centro.z;
        return foco;
    }

    // Mide cuánto suelo ve la cámara (por lado) proyectando las esquinas del
    // viewport sobre el plano del suelo. La cámara debe estar ya a su altura real.
    private void CalcularAlcances()
    {
        if (camara == null) return;

        // Plano del suelo a la altura de la arena (o del objetivo si no hay arena).
        float alturaSuelo = ArenaBounds.Instancia != null
            ? ArenaBounds.Instancia.Centro.y
            : (target != null ? target.position.y : 0f);
        Plane suelo = new Plane(Vector3.up, new Vector3(0f, alturaSuelo, 0f));

        // Centro de la toma sobre el suelo y las cuatro esquinas del viewport.
        Vector3 centroVista, e0, e1, e2, e3;
        bool ok = ProyectarAlSuelo(new Vector3(0.5f, 0.5f, 0f), suelo, out centroVista)
                & ProyectarAlSuelo(new Vector3(0f, 0f, 0f), suelo, out e0)
                & ProyectarAlSuelo(new Vector3(1f, 0f, 0f), suelo, out e1)
                & ProyectarAlSuelo(new Vector3(0f, 1f, 0f), suelo, out e2)
                & ProyectarAlSuelo(new Vector3(1f, 1f, 0f), suelo, out e3);
        if (!ok) return;

        // Rectángulo (en XZ) que abarca lo que se ve en el suelo.
        float minX = Mathf.Min(Mathf.Min(e0.x, e1.x), Mathf.Min(e2.x, e3.x));
        float maxX = Mathf.Max(Mathf.Max(e0.x, e1.x), Mathf.Max(e2.x, e3.x));
        float minZ = Mathf.Min(Mathf.Min(e0.z, e1.z), Mathf.Min(e2.z, e3.z));
        float maxZ = Mathf.Max(Mathf.Max(e0.z, e1.z), Mathf.Max(e2.z, e3.z));

        // Alcance por lado respecto al centro de la toma (asimétrico en isométrico).
        alcanceMenosX = centroVista.x - minX;
        alcanceMasX = maxX - centroVista.x;
        alcanceMenosZ = centroVista.z - minZ;
        alcanceMasZ = maxZ - centroVista.z;
        alcancesListos = true;
    }

    // Proyecta un punto del viewport sobre el plano del suelo. Devuelve false si no lo toca.
    private bool ProyectarAlSuelo(Vector3 puntoViewport, Plane suelo, out Vector3 punto)
    {
        Ray rayo = camara.ViewportPointToRay(puntoViewport);
        if (suelo.Raycast(rayo, out float distanciaRayo))
        {
            punto = rayo.GetPoint(distanciaRayo);
            return true;
        }
        punto = Vector3.zero;
        return false;
    }
}
