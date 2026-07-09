using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerColor : MonoBehaviour, IColorEntity
{
    [Header("Color inicial")]
    [SerializeField] private DropletColor currentColor = DropletColor.Red;
    public DropletColor Color => currentColor;

    public UnityEvent<DropletColor> OnColorChanged;

    // Guarda qué colores puede usar el jugador. Empieza solo con el color inicial.
    private HashSet<DropletColor> unlockedColors = new HashSet<DropletColor>();

    private InputSystem_Actions controls;

    private void Awake()
    {
        controls = new InputSystem_Actions();

        // El color con el que arrancas ya cuenta como "desbloqueado"
        unlockedColors.Add(currentColor);
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        // Cada tecla (1, 2, 3) intenta cambiar a un color específico
        controls.Player.SwitchRed.performed += OnSwitchRed;
        controls.Player.SwitchGreen.performed += OnSwitchGreen;
        controls.Player.SwitchBlue.performed += OnSwitchBlue;
    }

    private void OnDisable()
    {
        controls.Player.SwitchRed.performed -= OnSwitchRed;
        controls.Player.SwitchGreen.performed -= OnSwitchGreen;
        controls.Player.SwitchBlue.performed -= OnSwitchBlue;
        controls.Player.Disable();
    }

    private void OnSwitchRed(InputAction.CallbackContext ctx) => TrySetColor(DropletColor.Red);
    private void OnSwitchGreen(InputAction.CallbackContext ctx) => TrySetColor(DropletColor.Green);
    private void OnSwitchBlue(InputAction.CallbackContext ctx) => TrySetColor(DropletColor.Blue);

    // Llamado desde el pickup: agrega un color a la lista de desbloqueados
    // (no lo activa automáticamente, solo lo habilita para poder elegirlo después)
    public void UnlockColor(DropletColor color)
    {
        unlockedColors.Add(color);
    }

    public bool IsUnlocked(DropletColor color) => unlockedColors.Contains(color);

    // Cambia el color actual, solo si ya está desbloqueado
    private void TrySetColor(DropletColor newColor)
    {
        if (!unlockedColors.Contains(newColor))
        {
            Debug.Log($"Todavía no desbloqueas el color {newColor}");
            return;
        }

        if (newColor == currentColor) return; // Ya estás en ese color, no hacemos nada

        currentColor = newColor;
        OnColorChanged?.Invoke(currentColor);
    }
}