using UnityEngine;

public class DropletPickup : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] private DropletColor color;

    // Nuevo: permite que quien instancie este pickup le asigne el color
    // justo después de crearlo, sobrescribiendo el valor del Inspector
    public void SetColor(DropletColor newColor)
    {
        color = newColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerColor playerColor = other.GetComponent<PlayerColor>();
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();

        if (playerColor == null || inventory == null) return;

        playerColor.UnlockColor(color);
        inventory.AddDroplet(1);

        Destroy(gameObject);
    }
}