using UnityEngine;
using UnityEngine.Events;

// Maneja las gotas: cuántas tienes, sumarlas, gastarlas.
// Separado de PlayerColor porque el conteo de gotas y el color desbloqueado
// son dos responsabilidades distintas, aunque estén relacionadas.
public class PlayerInventory : MonoBehaviour
{
    [Header("Gotas")]
    [SerializeField] private int dropletCount = 0;

    public int DropletCount => dropletCount;

    // Se dispara cada vez que la cantidad cambia (sirve para actualizar UI después)
    public UnityEvent<int> OnDropletCountChanged;

    // Llamado desde el pickup cuando el jugador recoge una gota
    public void AddDroplet(int amount = 1)
    {
        dropletCount += amount;
        OnDropletCountChanged?.Invoke(dropletCount);
    }

    // Intenta gastar gotas (por ejemplo, al atacar). Devuelve true si había suficientes.
    public bool TrySpendDroplets(int amount)
    {
        if (dropletCount < amount)
        {
            return false; // No hay suficientes gotas, no se puede pagar el costo
        }

        dropletCount -= amount;
        OnDropletCountChanged?.Invoke(dropletCount);
        return true;
    }
}