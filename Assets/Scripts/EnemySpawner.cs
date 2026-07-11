using UnityEngine;

// Generación progresiva de enemigos sobre el área del plano (ArenaBounds):
//  - Tanda inicial CERCA del centro (para que el jugador los vea).
//  - Por cada enemigo muerto, aparece una nueva tanda, cada vez MÁS LEJOS del centro.
//  - Cada enemigo nace con un color ALEATORIO.
//  - Se detiene al agotar el presupuesto total (60 por ahora).
public class EnemySpawner : MonoBehaviour
{
    [Header("Qué generar")]
    // Prefab del enemigo a instanciar.
    [SerializeField] private GameObject prefabEnemigo;

    [Header("Cantidades")]
    // Enemigos de la primera tanda (al empezar la partida).
    [SerializeField] private int tandaInicial = 3;
    // Enemigos que aparecen por cada enemigo muerto.
    [SerializeField] private int tandaPorMuerte = 2;
    // Presupuesto total de enemigos de la partida (por el momento).
    [SerializeField] private int totalMaximo = 60;

    [Header("Distancia de aparición (desde el centro de la arena)")]
    // Radio de la primera tanda (cerca, visible para el jugador).
    [SerializeField] private float radioInicial = 8f;
    // Cuánto se aleja el radio tras cada tanda generada.
    [SerializeField] private float incrementoRadio = 3f;

    // Cuántos enemigos se han generado en total.
    private int generados;
    // Radio de aparición actual (crece con cada tanda).
    private float radioActual;

    private void Start()
    {
        // Primera tanda: cerca del centro.
        radioActual = radioInicial;
        GenerarTanda(tandaInicial);
    }

    // Genera una tanda completa y aleja el radio para la siguiente.
    private void GenerarTanda(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
            GenerarUno();

        // La siguiente tanda aparecerá un poco más lejos del centro.
        radioActual += incrementoRadio;
    }

    // Genera un enemigo en un punto aleatorio del anillo actual, con color aleatorio.
    private void GenerarUno()
    {
        // Sin prefab o presupuesto agotado: no se genera más.
        if (prefabEnemigo == null || generados >= totalMaximo) return;
        generados++;

        // Centro y radio máximo los da la arena (si no hay, usamos este objeto).
        Vector3 centro = ArenaBounds.Instancia != null ? ArenaBounds.Instancia.Centro : transform.position;
        float radioMaximo = ArenaBounds.Instancia != null ? ArenaBounds.Instancia.RadioMaximo : radioActual;
        float radio = Mathf.Min(radioActual, radioMaximo);

        // Punto aleatorio: dirección al azar, distancia entre el 70% y el 100% del
        // radio (para que la tanda no forme un anillo perfecto).
        Vector2 direccion = Random.insideUnitCircle.normalized;
        float distancia = Random.Range(radio * 0.7f, radio);
        Vector3 posicion = centro + new Vector3(direccion.x, 0f, direccion.y) * distancia;

        // Nos aseguramos de quedar dentro del plano y a la altura del spawner (el suelo).
        posicion = ArenaBounds.LimitarPosicion(posicion);
        posicion.y = transform.position.y;

        GameObject enemigo = Instantiate(prefabEnemigo, posicion, Quaternion.identity);

        // Color aleatorio entre los tres del juego.
        EntityColor color = enemigo.GetComponent<EntityColor>();
        if (color != null)
            color.EstablecerColor((DropletColor)Random.Range(0, 3));

        // Nos suscribimos a su muerte para generar la siguiente tanda.
        Health vida = enemigo.GetComponent<Health>();
        if (vida != null)
            vida.AlMorir.AddListener(AlMorirEnemigo);
    }

    // Cada enemigo muerto trae una nueva tanda (más lejos del centro).
    private void AlMorirEnemigo()
    {
        GenerarTanda(tandaPorMuerte);
    }
}
