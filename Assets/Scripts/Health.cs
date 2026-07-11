using UnityEngine;
using UnityEngine.Events;

// Componente de vida genérico. Lo usan tanto el jugador como los enemigos.
public class Health : MonoBehaviour
{
    [Header("Vida")]
    // Vida máxima con la que arranca la entidad.
    [SerializeField] private float vidaMaxima = 100f;

    [Header("Referencias")]
    // Visual del modelo, para reproducir las animaciones de daño y muerte.
    // Suele estar en el mismo objeto (raíz del FBX); se busca solo si se deja vacío.
    [SerializeField] private DroppyAnimationController visual;

    // Vida actual en runtime.
    private float vidaActual;
    // Una vez muerto, ignora cualquier daño o curación posterior.
    private bool muerto;

    // Consultas públicas (útiles para la UI).
    public float VidaMaxima => vidaMaxima;
    public float VidaActual => vidaActual;
    public bool EstaMuerto => muerto;

    // Eventos para que otros scripts reaccionen (animación, UI, soltar gota, etc.).
    public UnityEvent AlMorir;                      // se dispara una sola vez, al morir
    public UnityEvent AlRecibirDanio;               // se dispara cada vez que recibe daño
    public UnityEvent<float, float> AlCambiarVida;  // (vidaActual, vidaMaxima) para la UI

    private void Awake()
    {
        // Empezamos con la vida llena.
        vidaActual = vidaMaxima;

        // Si no se asignó a mano, buscamos el visual en este objeto o sus hijos.
        if (visual == null)
            visual = GetComponentInChildren<DroppyAnimationController>();
    }

    // Aplica daño y comprueba si murió.
    public void RecibirDanio(float cantidad)
    {
        // Si ya está muerto o el daño no es positivo, no hacemos nada.
        if (muerto || cantidad <= 0f) return;

        // Restamos sin bajar de 0.
        vidaActual = Mathf.Max(vidaActual - cantidad, 0f);
        AlRecibirDanio?.Invoke();
        AlCambiarVida?.Invoke(vidaActual, vidaMaxima);

        // Muerte: se marca antes de avisar para no volver a entrar.
        if (vidaActual <= 0f)
        {
            muerto = true;
            // Animación de muerte.
            if (visual != null)
                visual.Die();
            AlMorir?.Invoke();
        }
        else
        {
            // Sigue vivo: animación de recibir daño.
            if (visual != null)
                visual.Hurt();
        }
    }

    // Daño "silencioso" (veneno de la pintura del suelo): baja la vida y puede matar,
    // pero NO dispara la animación de daño ni el evento AlRecibirDanio.
    // Así el veneno no interrumpe ataques, no empuja y no sacude la cámara;
    // el feedback visual lo pone PaintPoison (contracción de escala).
    public void RecibirDanioVeneno(float cantidad)
    {
        // Si ya está muerto o el daño no es positivo, no hacemos nada.
        if (muerto || cantidad <= 0f) return;

        // Restamos sin bajar de 0 y avisamos solo a la UI (no al resto del combate).
        vidaActual = Mathf.Max(vidaActual - cantidad, 0f);
        AlCambiarVida?.Invoke(vidaActual, vidaMaxima);

        // La muerte por veneno sí se gestiona igual que la muerte normal.
        if (vidaActual <= 0f)
        {
            muerto = true;
            // Animación de muerte.
            if (visual != null)
                visual.Die();
            AlMorir?.Invoke();
        }
    }

    // Rellena vida sin pasar del máximo.
    public void Curar(float cantidad)
    {
        // No se cura si está muerto o la cantidad no es positiva.
        if (muerto || cantidad <= 0f) return;

        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        AlCambiarVida?.Invoke(vidaActual, vidaMaxima);
    }
}
