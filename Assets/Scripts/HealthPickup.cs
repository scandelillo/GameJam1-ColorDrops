using UnityEngine;

// Gota curativa que sueltan los enemigos al morir.
// Cuando el JUGADOR la toca, le rellena vida y la gota se destruye.
// Requiere un Collider con "Is Trigger" activado.
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [Header("Curación")]
    // Cuánta vida devuelve al recogerla.
    [SerializeField] private float cantidadCuracion = 25f;

    [Header("Tiempo de vida")]
    // Segundos que dura en el suelo antes de desaparecer sola (0 = nunca).
    [SerializeField] private float duracion = 10f;

    private void Start()
    {
        // Si tiene duración, se autodestruye para no acumular gotas en la escena.
        if (duracion > 0f)
            Destroy(gameObject, duracion);
    }

    private void OnTriggerEnter(Collider otro)
    {
        // Solo cura al jugador (debe llevar PlayerController y Health).
        PlayerController jugador = otro.GetComponent<PlayerController>();
        if (jugador == null) return;

        Health vida = otro.GetComponent<Health>();
        if (vida == null || vida.EstaMuerto) return;

        // Rellenamos vida y consumimos la gota.
        vida.Curar(cantidadCuracion);
        Destroy(gameObject);
    }
}
