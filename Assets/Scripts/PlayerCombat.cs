using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerColor))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combate")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask droplerLayer;

    [Header("Daño")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float damagePerDroplet = 0.5f; // Cuánto sube el daño por cada gota que tengas
    [SerializeField] private int dropletsCostPerAttack = 1; // Cuántas gotas gasta cada ataque

    private InputSystem_Actions controls;
    private PlayerColor playerColor;
    private PlayerInventory inventory;

    [Header("Animación")]
    [SerializeField] private DroppyAnimationController droppy;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerColor = GetComponent<PlayerColor>();
        inventory = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Attack.performed -= OnAttackPerformed;
        controls.Player.Disable();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        Attack();
    }

    private void Attack()
    {
        Debug.Log("=== ⚔️ ATAQUE DEL JUGADOR ===");

        float totalDamage;

        if (droppy != null)
            droppy.Attack();

        if (inventory.TrySpendDroplets(dropletsCostPerAttack))
        {
            totalDamage = baseDamage + ((inventory.DropletCount + dropletsCostPerAttack) * damagePerDroplet);
        }
        else
        {
            totalDamage = baseDamage;
        }

        Debug.Log($"💰 Daño calculado: {totalDamage}");

        // ✅ CORREGIDO: Detectar alrededor del jugador
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, droplerLayer);

        Debug.Log($"📡 Enemigos detectados: {hits.Length} en un radio de {attackRange} alrededor del jugador");

        foreach (Collider hit in hits)
        {
            Debug.Log($"🎯 Hit: {hit.gameObject.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");

            IColorEntity targetColorEntity = hit.GetComponent<IColorEntity>();
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (targetColorEntity == null)
            {
                Debug.Log($"❌ {hit.gameObject.name} NO tiene IColorEntity");
                continue;
            }
            if (damageable == null)
            {
                Debug.Log($"❌ {hit.gameObject.name} NO tiene IDamageable");
                continue;
            }

            Debug.Log($"🎨 Color del enemigo: {targetColorEntity.DColor}");
            Debug.Log($"🎨 Color del jugador: {playerColor.DColor}");

            if (targetColorEntity.DColor == playerColor.DColor)
            {
                Debug.Log($"❌ Mismo color, ignorando");
                continue;
            }

            float multiplier = ColorEffectiveness.GetMultiplier(playerColor.DColor, targetColorEntity.DColor);
            float finalDamage = totalDamage * multiplier;

            Debug.Log($"💥 Aplicando {finalDamage} de daño (multiplicador: {multiplier})");
            damageable.TakeDamage(finalDamage);
        }
    }
}