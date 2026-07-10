using UnityEngine;

[RequireComponent(typeof(Health))]
public class Droplet : MonoBehaviour, IColorEntity
{
    [Header("Identidad")]
    [SerializeField] private DropletColor color;
    public DropletColor DColor => color;

    [Header("Movimiento e IA")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float baseDamage = 10f;

    [Header("Drop al morir")]
    [SerializeField] private GameObject dropPrefab;

    [Header("Visual - Texture Array")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private float colorTransitionSpeed = 2f;

    private Health health;
    private Transform currentTarget;
    private float lastAttackTime;
    private Material materialInstance;

    // IDs de las propiedades del shader
    private int fromIndexId;
    private int toIndexId;
    private int blendId;

    private int currentColorIndex;
    private bool isTransitioning;

    public static event System.Action OnAnyDropletDeath;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();

        // Obtener IDs de las propiedades
        fromIndexId = Shader.PropertyToID("_FromIndex");
        toIndexId = Shader.PropertyToID("_ToIndex");
        blendId = Shader.PropertyToID("_Blend");

        // Crear instancia del material
        if (bodyRenderer != null)
        {
            materialInstance = bodyRenderer.material;
            // Inicializar con el color actual
            SetColorInstant((int)color);
        }
    }

    private void OnEnable()
    {
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
        Debug.Log($"🎯 Persiguiendo a {currentTarget.name}, distancia: {distance}");

        if (distance > attackRange)
        {
            MoveTowards(currentTarget.position);
        }
        else
        {

            Debug.Log("⚔️ ¡Enemigo en rango de ataque!");
            TryAttack(currentTarget);
        }
    }

    public void SetColor(DropletColor newColor)
    {
        color = newColor;

        // Cambiar color instantáneamente usando el índice del enum
        SetColorInstant((int)color);
    }

    // Cambio instantáneo de color
    private void SetColorInstant(int colorIndex)
    {
        if (materialInstance == null) return;

        currentColorIndex = colorIndex;

        // Establecer From y To al mismo índice, Blend en 0
        materialInstance.SetFloat(fromIndexId, colorIndex);
        materialInstance.SetFloat(toIndexId, colorIndex);
        materialInstance.SetFloat(blendId, 0f);
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider hit in hits)
        {
            IColorEntity otherColorEntity = hit.GetComponent<IColorEntity>();
            if (otherColorEntity == null) continue;
            if (otherColorEntity.DColor == color) continue;

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
        direction.y = 0f;
        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }

    private void TryAttack(Transform target)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        IColorEntity targetColorEntity = target.GetComponent<IColorEntity>();

        if (damageable == null || targetColorEntity == null) return;

        float multiplier = ColorEffectiveness.GetMultiplier(color, targetColorEntity.DColor);
        damageable.TakeDamage(baseDamage * multiplier);

        lastAttackTime = Time.time;
    }

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

        OnAnyDropletDeath?.Invoke();

        if (materialInstance != null)
            Destroy(materialInstance);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}