using UnityEngine;
using UnityEngine.UI; // Asegúrate de incluir esto para usar Slider

public class HealthBar : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Health targetHealth; // El script Health del jugador

    private void OnEnable()
    {
        if (targetHealth != null)
        {
            // Nos suscribimos al evento cuando el script se activa
            targetHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            // Es buena práctica desuscribirse para evitar errores de memoria
            targetHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }

    private void Start()
    {
        // Inicializamos la barra con los valores iniciales
        if (targetHealth != null)
        {
            InitializeBar(targetHealth.CurrentHealth, targetHealth.MaxHealth);
        }
    }

    private void InitializeBar(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    // Esta función se ejecutará automáticamente cada vez que el jugador reciba daño
    private void UpdateHealthBar(float current, float max)
    {
        healthSlider.value = current;
    }
}
