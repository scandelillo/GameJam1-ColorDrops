using UnityEngine;
using UnityEngine.Events;

// Componente independiente: solo sabe "de qué color es el jugador ahora"
// y avisa a quien le interese cuando ese color cambia.
public class PlayerColor : MonoBehaviour, IColorEntity
{
    [Header("Color inicial")]
    [SerializeField] private DropletColor currentColor = DropletColor.Red;

    // Implementación de IColorEntity: expone el color de solo lectura
    public DropletColor Color => currentColor;

    // Evento que se dispara cada vez que el color cambia.
    // Otros scripts (UI, VFX, PlayerCombat, etc.) pueden suscribirse sin
    // que este script necesite saber que existen.
    public UnityEvent<DropletColor> OnColorChanged;

    // Método público para cambiar el color, llamado por ejemplo desde el pickup
    public void SetColor(DropletColor newColor)
    {
        // Evitamos disparar el evento si en realidad no cambió nada
        if (newColor == currentColor) return;

        currentColor = newColor;
        OnColorChanged?.Invoke(currentColor);
    }
}