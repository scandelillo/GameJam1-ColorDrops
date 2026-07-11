using UnityEngine;

// Límites del área jugable (el plano donde se mueven todos).
// Única fuente de los límites del mapa: el jugador, los enemigos y el spawner
// la consultan para no salirse del plano ni generar fuera de él.
// Ponlo en el objeto del PLANO: toma el tamaño de su Renderer automáticamente
// (si no hay Renderer, usa el tamaño manual).
public class ArenaBounds : MonoBehaviour
{
    // Instancia única para acceso sencillo desde cualquier script.
    public static ArenaBounds Instancia { get; private set; }

    [Header("Área (rectángulo en el plano XZ)")]
    // Tamaño manual (X, Z). Solo se usa si este objeto NO tiene Renderer.
    [SerializeField] private Vector2 tamanoManual = new Vector2(20f, 20f);
    // Margen interior para que nadie quede justo en el borde.
    [SerializeField] private float margen = 0.5f;

    // Renderer del plano (si existe, el área se ajusta sola a él).
    private Renderer rendererPlano;

    // Centro del área jugable.
    public Vector3 Centro => rendererPlano != null ? rendererPlano.bounds.center : transform.position;

    // Tamaño del área en X y Z.
    private Vector2 Tamano => rendererPlano != null
        ? new Vector2(rendererPlano.bounds.size.x, rendererPlano.bounds.size.z)
        : tamanoManual;

    // Distancia máxima útil desde el centro hasta el borde más cercano
    // (la usa el spawner para no pedir puntos fuera del plano).
    public float RadioMaximo => Mathf.Min(Tamano.x, Tamano.y) * 0.5f - margen;

    // Mitad del área (X, Z): del centro al borde REAL del plano (sin margen).
    // La usa la cámara para hacer coincidir su borde visible con el borde del plano.
    public Vector2 MitadArea => Tamano * 0.5f;

    private void Awake()
    {
        Instancia = this;
        rendererPlano = GetComponent<Renderer>();
    }

    private void OnDestroy()
    {
        // Limpiamos la instancia si somos nosotros (por si se recarga la escena).
        if (Instancia == this)
            Instancia = null;
    }

    // Devuelve la posición limitada al interior del área (la Y no se toca).
    public Vector3 Limitar(Vector3 posicion)
    {
        Vector3 centro = Centro;
        Vector2 mitad = Tamano * 0.5f;

        posicion.x = Mathf.Clamp(posicion.x, centro.x - mitad.x + margen, centro.x + mitad.x - margen);
        posicion.z = Mathf.Clamp(posicion.z, centro.z - mitad.y + margen, centro.z + mitad.y - margen);
        return posicion;
    }

    // Versión estática cómoda: si no hay arena en la escena, no limita nada.
    public static Vector3 LimitarPosicion(Vector3 posicion)
    {
        return Instancia != null ? Instancia.Limitar(posicion) : posicion;
    }

    // Dibuja el área jugable (con margen) en el editor.
    private void OnDrawGizmos()
    {
        Renderer r = rendererPlano != null ? rendererPlano : GetComponent<Renderer>();
        Vector3 centro = r != null ? r.bounds.center : transform.position;
        Vector2 t = r != null ? new Vector2(r.bounds.size.x, r.bounds.size.z) : tamanoManual;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(centro, new Vector3(t.x - margen * 2f, 0.1f, t.y - margen * 2f));
    }
}
