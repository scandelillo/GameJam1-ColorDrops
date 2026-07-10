using UnityEngine;
using UnityEngine.Events;

// Componente genérico de salud. Se puede poner tanto en el jugador como en las gotas.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Salud")]
    [SerializeField] private float maxHealth = 30f;

    private float currentHealth;
    // 🔴 AGREGAMOS ESTO: Propiedades públicas para que la UI pueda leer los valores
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    // 🔴 AGREGAMOS ESTO: Un evento que pasa la salud actual y la máxima
    public UnityEvent<float, float> OnHealthChanged;

    // Evento que se dispara cuando este objeto muere.
    // Lo usamos para que Droplet.cs sepa cuándo debe spawnear el drop,
    // sin que Health.cs necesite saber nada sobre drops.
    public UnityEvent OnDeath;

    public UnityEvent OnDamaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Implementación de IDamageable: reduce la salud y revisa si murió
    //public void TakeDamage(float amount)
    //{
    //    if (amount <= 0f) return;

    //    currentHealth -= amount;
    //    OnDamaged?.Invoke(); // Avisa que recibió daño, sin importar si murió o no

    //    if (currentHealth <= 0f)
    //    {
    //        Die();
    //    }
    //}

    public void TakeDamage(float amount)
    {
        Debug.Log($"💥 {gameObject.name} recibe {amount} de daño. Salud actual: {currentHealth}/{maxHealth}");

        if (amount <= 0f) return;

        currentHealth -= amount;

// 🔴 INVOCAMOS EL NUEVO EVENTO pasándole la vida actual y la máxima
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        OnDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            Debug.Log($"💀 {gameObject.name} ha muerto");
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke(); // Avisa a quien esté escuchando (ej: Droplet.cs) que este objeto murió
    }
}