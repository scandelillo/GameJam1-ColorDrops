using UnityEngine;

// Rastro de pintura: deja pinceladas en el lienzo (PaintCanvas) al moverse.
// Va tanto en el jugador como en los enemigos.
// El color de cada pincelada se lee de EntityColor EN ESE MOMENTO, así el trail
// del jugador cambia solo cuando cambia de color con 1/2/3.
// IMPORTANTE: se pinta un poco DETRÁS de la entidad (no debajo). Si pintáramos
// debajo, taparíamos al instante la pintura ajena que estamos pisando y el
// veneno (PaintPoison) nunca podría detectarla.
[RequireComponent(typeof(EntityColor))]
public class PaintTrail : MonoBehaviour
{
    [Header("Pinceladas")]
    // Distancia que hay que recorrer para dejar la siguiente pincelada.
    // Debe ser menor que el diámetro del pincel para que el trazo sea continuo.
    [SerializeField] private float distanciaEntrePinceladas = 0.35f;
    // Qué tan atrás de la entidad cae la pincelada (deja libre el suelo bajo los pies).
    [SerializeField] private float retrasoPincel = 0.6f;

    // Color propio (decide de qué color se pinta).
    private EntityColor color;
    // Vida propia (los muertos ya no pintan).
    private Health vida;
    // Última posición donde se evaluó la pincelada.
    private Vector3 ultimaPosicion;

    private void Awake()
    {
        color = GetComponent<EntityColor>();
        vida = GetComponent<Health>();
    }

    private void Start()
    {
        // Arrancamos desde donde aparecemos (sin pintar todavía: la primera
        // pincelada cae al empezar a movernos).
        ultimaPosicion = transform.position;
    }

    private void Update()
    {
        // Los muertos no pintan; sin lienzo en la escena tampoco hay nada que hacer.
        if (vida != null && vida.EstaMuerto) return;
        if (PaintCanvas.Instancia == null) return;

        // ¿Ya recorrimos suficiente distancia desde la última pincelada?
        Vector3 recorrido = transform.position - ultimaPosicion;
        recorrido.y = 0f;
        if (recorrido.magnitude < distanciaEntrePinceladas) return;

        // Pintamos DETRÁS (en la dirección de la que venimos) con el color actual.
        Vector3 puntoPincel = transform.position - recorrido.normalized * retrasoPincel;
        PaintCanvas.Instancia.Pintar(puntoPincel, color.ColorActual);

        ultimaPosicion = transform.position;
    }
}
