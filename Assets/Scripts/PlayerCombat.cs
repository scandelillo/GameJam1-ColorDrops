using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerColor))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combate")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private LayerMask droplerLayer;

    private InputSystem_Actions controls;
    private PlayerColor playerColor; // Referencia al script que sabe el color actual

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerColor = GetComponent<PlayerColor>(); // Buscamos el componente en el mismo GameObject
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
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, attackRange, droplerLayer);

        foreach (Collider hit in hits)
        {
            IColorEntity targetColorEntity = hit.GetComponent<IColorEntity>();
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (targetColorEntity == null || damageable == null) continue;

            // Preguntamos el color actual a través de playerColor en vez de una variable local
            if (targetColorEntity.Color == playerColor.Color) continue;

            float multiplier = ColorEffectiveness.GetMultiplier(playerColor.Color, targetColorEntity.Color);
            damageable.TakeDamage(baseDamage * multiplier);
        }
    }
}