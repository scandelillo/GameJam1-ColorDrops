using UnityEngine;
using UnityEngine.Events;

// Componente genérico de salud. Se puede poner tanto en el jugador como en las gotas.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Salud")]
    [SerializeField] private float maxHealth = 30f;

    private float currentHealth;

    // Evento que se dispara cuando este objeto muere.
    // Lo usamos para que Droplet.cs sepa cuándo debe spawnear el drop,
    // sin que Health.cs necesite saber nada sobre drops.
    public UnityEvent OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Implementación de IDamageable: reduce la salud y revisa si murió
    public void TakeDamage(float amount)
    {
        // Si el multiplicador de efectividad fue 0 (mismo color), no hacemos nada
        if (amount <= 0f) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }

        Debug.Log("tienes " + currentHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke(); // Avisa a quien esté escuchando (ej: Droplet.cs) que este objeto murió
        Destroy(gameObject);
    }
}