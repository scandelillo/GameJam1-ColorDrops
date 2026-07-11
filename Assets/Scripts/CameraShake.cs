using UnityEngine;

// Sacudida de cámara (screen shake). Va en el MISMO objeto que la cámara.
// Se dispara cuando el JUGADOR recibe daño, para darle impacto al golpe.
// No mueve la cámara directamente: expone OffsetActual, que IsometricCameraFollow
// suma a su posición de seguimiento (así no pelean por transform.position).
public class CameraShake : MonoBehaviour
{
    [Header("Sacudida")]
    // Qué tanto se desplaza la cámara al sacudirse (unidades del mundo).
    [SerializeField] private float intensidad = 0.35f;
    // Cuánto dura la sacudida (segundos).
    [SerializeField] private float duracion = 0.25f;

    // Desplazamiento actual de la sacudida (lo lee la cámara).
    public Vector3 OffsetActual { get; private set; }

    // Vida del jugador (nos suscribimos a su daño).
    private Health vidaJugador;
    // Estado de la sacudida en curso.
    private float tiempoRestante;
    private float duracionActual;
    private float intensidadActual;

    private void Start()
    {
        // Buscamos al jugador y escuchamos cuando recibe daño.
        PlayerController jugador = FindObjectOfType<PlayerController>();
        if (jugador != null)
        {
            vidaJugador = jugador.GetComponent<Health>();
            if (vidaJugador != null)
                vidaJugador.AlRecibirDanio.AddListener(Sacudir);
        }
    }

    private void OnDestroy()
    {
        if (vidaJugador != null)
            vidaJugador.AlRecibirDanio.RemoveListener(Sacudir);
    }

    // Inicia una sacudida con los valores por defecto (lo llama el evento de daño).
    public void Sacudir()
    {
        Sacudir(intensidad, duracion);
    }

    // Inicia una sacudida con fuerza y duración concretas (reinicia la anterior).
    public void Sacudir(float fuerza, float tiempo)
    {
        intensidadActual = fuerza;
        duracionActual = Mathf.Max(0.01f, tiempo);
        tiempoRestante = duracionActual;
    }

    // Calculamos el desplazamiento en Update (antes del LateUpdate de la cámara,
    // así ésta lee siempre el offset fresco de este frame).
    private void Update()
    {
        // Sin sacudida activa: sin desplazamiento.
        if (tiempoRestante <= 0f)
        {
            OffsetActual = Vector3.zero;
            return;
        }

        tiempoRestante -= Time.deltaTime;

        // La sacudida se va apagando hacia el final (1 -> 0).
        float atenuacion = Mathf.Clamp01(tiempoRestante / duracionActual);

        // Desplazamiento aleatorio en el plano de la pantalla (ejes de la cámara).
        Vector2 aleatorio = Random.insideUnitCircle;
        OffsetActual = (transform.right * aleatorio.x + transform.up * aleatorio.y)
                       * (intensidadActual * atenuacion);
    }
}
