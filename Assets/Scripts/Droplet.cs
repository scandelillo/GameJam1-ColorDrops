using UnityEngine;

[RequireComponent(typeof(Health))]
public class Droplet : MonoBehaviour, IColorEntity
{
    [Header("Identidad")]
    [SerializeField] private DropletColor color;
    public DropletColor Color => color; // Exponemos el color de solo lectura (cumple IColorEntity)

    [Header("Movimiento e IA")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 5f; // Qué tan lejos "ve" a sus enemigos
    [SerializeField] private float attackRange = 1f;      // Qué tan cerca necesita estar para atacar
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float baseDamage = 10f;

    [Header("Drop al morir")]
    [SerializeField] private GameObject dropPrefab; // El objeto recolectable que suelta

    private Health health;
    private Transform currentTarget; // A quién está persiguiendo/atacando ahora mismo
    private float lastAttackTime;


    // Evento ESTÁTICO: se dispara cada vez que CUALQUIER gota en la escena muere.
    // Al ser estático, el Spawner puede escucharlo sin necesitar una referencia
    // a cada gota individual.
    public static event System.Action OnAnyDropletDeath;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        // Nos suscribimos a nuestra propia muerte para saber cuándo spawnear el drop
        health.OnDeath.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        health.OnDeath.RemoveListener(HandleDeath);
    }

    private void Update()
    {
        FindTarget();

        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > attackRange)
        {
            MoveTowards(currentTarget.position);
        }
        else
        {
            TryAttack(currentTarget);
        }
    }

    public void SetColor(DropletColor newColor)
    {
        color = newColor;
    }

    // Busca el enemigo hostil más cercano (jugador o gota de color distinto) dentro del radio de detección
    private void FindTarget()
    {
        // Physics.OverlapSphere devuelve todos los colliders dentro de un radio
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider hit in hits)
        {
            // Buscamos si el objeto detectado tiene un color (IColorEntity)
            IColorEntity otherColorEntity = hit.GetComponent<IColorEntity>();
            if (otherColorEntity == null) continue; // No es una gota ni el jugador, lo ignoramos

            // Si es del mismo color, no es hostil, lo ignoramos
            if (otherColorEntity.Color == color) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hit.transform;
            }
        }

        currentTarget = closest;
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f; // Evitamos que la gota intente moverse en el eje vertical
        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.forward = direction; // La gota mira hacia donde se mueve
    }

    private void TryAttack(Transform target)
    {
        // Respetamos el cooldown para no atacar cada frame
        if (Time.time < lastAttackTime + attackCooldown) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        IColorEntity targetColorEntity = target.GetComponent<IColorEntity>();

        if (damageable == null || targetColorEntity == null) return;

        // Calculamos el daño real según la tabla de efectividad de colores
        float multiplier = ColorEffectiveness.GetMultiplier(color, targetColorEntity.Color);
        damageable.TakeDamage(baseDamage * multiplier);

        lastAttackTime = Time.time;
    }

    // Se llama automáticamente cuando Health dispara OnDeath
    private void HandleDeath()
    {
        if (dropPrefab != null)
        {
            GameObject dropInstance = Instantiate(dropPrefab, transform.position, Quaternion.identity);
            DropletPickup pickup = dropInstance.GetComponent<DropletPickup>();
            if (pickup != null)
            {
                pickup.SetColor(color);
            }
        }

        OnAnyDropletDeath?.Invoke(); //  avisa globalmente que una gota murió
        Destroy(gameObject);
    }

    // Dibuja el radio de detección en la escena (solo visible en el Editor, no en el juego)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = UnityEngine.Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}