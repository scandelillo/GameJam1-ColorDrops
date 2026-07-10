using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Representa una zona circular del mapa donde pueden aparecer gotas.
// No es un MonoBehaviour: es una clase de datos que se muestra en el Inspector
// gracias a [System.Serializable].
[System.Serializable]
public class SpawnZone
{
    public Transform center; // Un GameObject vacío colocado en el mapa que marca el centro
    public float radius = 5f; // Qué tan lejos del centro pueden aparecer gotas
}

public class DropletSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject dropletPrefab;
    [SerializeField] private PlayerColor playerColor; // Para saber qué color evitar en la gota inicial

    [Header("Zonas de spawn")]
    [SerializeField] private List<SpawnZone> spawnZones = new List<SpawnZone>();
    [SerializeField] private float spawnHeightOffset = 1.0f; // Ajusta según el tamaño de tu gota

    [Header("Límite global")]
    [SerializeField] private int maxAliveDroplets = 20;

    [Header("Oleadas de grupos (a partir de la oleada 1)")]
    [SerializeField] private int maxGroupsPerWave = 3;
    [SerializeField] private int baseGroupSize = 3;
    [SerializeField] private int groupSizeIncreasePerWave = 2;
    [SerializeField] private float timeBetweenWaves = 30f; // tiempo máximo antes de forzar la siguiente oleada
    [SerializeField] private int lowDropletThreshold = 2;  // si quedan estas o menos, se adelanta la siguiente oleada

    private int currentAliveCount = 0;
    private int waveNumber = 0; // 0 = gota inicial solitaria, 1 = primera oleada de grupos, 2+ = oleadas escaladas
    private Coroutine waveRoutine;

    private void OnEnable()
    {
        // Nos suscribimos al evento ESTÁTICO de Droplet: no importa cuál gota muera, nos enteramos igual
        Droplet.OnAnyDropletDeath += HandleDropletDeath;
    }

    private void OnDisable()
    {
        Droplet.OnAnyDropletDeath -= HandleDropletDeath;
    }

    private void Start()
    {
        SpawnInitialDroplet();
    }

    // ---------- Oleada 0: la única gota inicial ----------
    private void SpawnInitialDroplet()
    {
        DropletColor forcedColor = GetColorDifferentFromPlayer();
        SpawnZone zone = spawnZones[Random.Range(0, spawnZones.Count)];
        Vector3 position = GetRandomPositionInZone(zone);

        SpawnDroplet(position, forcedColor);
        currentAliveCount++;
    }

    // Elige un color al azar, garantizando que sea distinto al color ACTUAL del jugador
    private DropletColor GetColorDifferentFromPlayer()
    {
        DropletColor chosen;
        do
        {
            chosen = (DropletColor)Random.Range(0, 3);
        }
        while (chosen == playerColor.DColor);

        return chosen;
    }

    // ---------- Se ejecuta cada vez que CUALQUIER gota muere ----------
    private void HandleDropletDeath()
    {
        currentAliveCount = Mathf.Max(0, currentAliveCount - 1);

        // Si la que murió fue la gota inicial (oleada 0), pasamos directo a la primera oleada de grupos
        if (waveNumber == 0)
        {
            waveNumber = 1;
            SpawnWave();
            return;
        }

        // Si ya quedan pocas gotas vivas, adelantamos la siguiente oleada sin esperar el tiempo completo
        if (currentAliveCount <= lowDropletThreshold)
        {
            StartNextWave();
        }
    }

    // ---------- Spawnea los grupos de la oleada actual ----------
    private void SpawnWave()
    {
        // El tamaño de grupo crece según el número de oleada
        int groupSize = baseGroupSize + (groupSizeIncreasePerWave * (waveNumber - 1));
        int groupsThisWave = Mathf.Min(maxGroupsPerWave, spawnZones.Count);

        // Copiamos la lista de zonas para poder ir sacando las que ya usamos
        // y que no se repita la misma zona dos veces en la misma oleada
        List<SpawnZone> availableZones = new List<SpawnZone>(spawnZones);

        for (int i = 0; i < groupsThisWave; i++)
        {
            if (availableZones.Count == 0) break;

            int zoneIndex = Random.Range(0, availableZones.Count);
            SpawnZone zone = availableZones[zoneIndex];
            availableZones.RemoveAt(zoneIndex);

            DropletColor groupColor = (DropletColor)Random.Range(0, 3); // Todo el grupo comparte color
            SpawnGroup(zone, groupColor, groupSize);
        }

        // Arrancamos el temporizador de la siguiente oleada (por si no llega antes por "pocas gotas")
        waveRoutine = StartCoroutine(WaveTimer());
    }

    private void SpawnGroup(SpawnZone zone, DropletColor color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Respetamos el máximo global: si ya se llegó al tope, dejamos de spawnear
            if (currentAliveCount >= maxAliveDroplets) return;

            Vector3 position = GetRandomPositionInZone(zone);
            SpawnDroplet(position, color);
            currentAliveCount++;
        }
    }

    private IEnumerator WaveTimer()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        waveRoutine = null;
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        waveNumber++;
        SpawnWave();
    }

    // ---------- Utilidades ----------

    // Devuelve una posición aleatoria dentro del círculo de una zona
    private Vector3 GetRandomPositionInZone(SpawnZone zone)
    {
        Vector2 randomCircle = Random.insideUnitCircle * zone.radius;
        Vector3 offset = new Vector3(randomCircle.x, spawnHeightOffset, randomCircle.y);
        return zone.center.position + offset;
    }

    private void SpawnDroplet(Vector3 position, DropletColor color)
    {
        GameObject instance = Instantiate(dropletPrefab, position, Quaternion.identity);
        Droplet droplet = instance.GetComponent<Droplet>();
        if (droplet != null)
        {
            droplet.SetColor(color);
        }
    }


}