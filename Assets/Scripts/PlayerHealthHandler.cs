using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealthHandler : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath.AddListener(HandlePlayerDeath);
    }

    private void OnDisable()
    {
        health.OnDeath.RemoveListener(HandlePlayerDeath);
    }

    private void HandlePlayerDeath()
    {
        // Por ahora, algo simple para probar. Aquí después conectamos
        // pantalla de Game Over, reinicio de escena, etc.
        Debug.Log("El jugador murió. Aquí va la lógica de Game Over.");

        // Ejemplo simple: deshabilitar el movimiento y el combate en vez de destruir
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerCombat>().enabled = false;
    }
}