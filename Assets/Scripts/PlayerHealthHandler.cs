using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealthHandler : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] private DroppyAnimationController droppy;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath.AddListener(HandlePlayerDeath);
        health.OnDamaged.AddListener(HandlePlayerDamaged); // nuevo
    }

    private void OnDisable()
    {
        health.OnDeath.RemoveListener(HandlePlayerDeath);
        health.OnDamaged.RemoveListener(HandlePlayerDamaged); // nuevo
    }

    private void HandlePlayerDamaged()
    {
        droppy.Hurt();
    }

    private void HandlePlayerDeath()
    {
        droppy.Die();
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerCombat>().enabled = false;
    }
}