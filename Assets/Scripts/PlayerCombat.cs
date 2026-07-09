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
        float totalDamage;

        // Intentamos gastar las gotas del costo del ataque
        if (inventory.TrySpendDroplets(dropletsCostPerAttack))
        {
            // Si tenías suficientes, el ataque sale con el bono de daño
            // (usamos el conteo actual + lo que ya gastamos, igual que antes)
            totalDamage = baseDamage + ((inventory.DropletCount + dropletsCostPerAttack) * damagePerDroplet);
        }
        else
        {
            // Si no tenías suficientes gotas, el ataque igual se ejecuta,
            // pero solo con el daño base, sin bono y sin gastar nada
            totalDamage = baseDamage;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, attackRange, droplerLayer);

        foreach (Collider hit in hits)
        {
            IColorEntity targetColorEntity = hit.GetComponent<IColorEntity>();
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (targetColorEntity == null || damageable == null) continue;
            if (targetColorEntity.Color == playerColor.Color) continue;

            float multiplier = ColorEffectiveness.GetMultiplier(playerColor.Color, targetColorEntity.Color);
            damageable.TakeDamage(totalDamage * multiplier);
        }
    }
}