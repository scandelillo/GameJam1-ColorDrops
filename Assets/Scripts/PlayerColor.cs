using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerColor : MonoBehaviour, IColorEntity
{
    [Header("DColor inicial")]
    [SerializeField] private DropletColor currentColor = DropletColor.Red;
    public DropletColor DColor => currentColor;

    public UnityEvent<DropletColor> OnColorChanged;

    // Guarda qué colores puede usar el jugador. Empieza solo con el color inicial.
    private HashSet<DropletColor> unlockedColors = new HashSet<DropletColor>();

    private InputSystem_Actions controls;


    [Header("Animación")]
    [SerializeField] private DroppyAnimationController droppy;


    private void Awake()
    {
        controls = new InputSystem_Actions();

        // El color con el que arrancas ya cuenta como "desbloqueado"
        unlockedColors.Add(currentColor);
    }

    private void Start()
    {
        // Pintamos el material con el color lógico inicial.
        // Lo hacemos en Start (no en Awake) para asegurar que el DroppyAnimationController
        // ya creó su instancia de material.
        if (droppy != null)
            droppy.SetColorInstant((int)currentColor);
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    // Leemos el input de cambio de color por polling, igual que el movimiento.
    // WasPressedThisFrame() devuelve true solo en el frame en que se presiona la tecla.
    private void Update()
    {
        // Tecla 1 -> Rojo, Tecla 2 -> Amarillo (la acción sigue llamándose "SwitchGreen"), Tecla 3 -> Azul.
        if (controls.Player.SwitchRed.WasPressedThisFrame()) TrySetColor(DropletColor.Red);
        if (controls.Player.SwitchGreen.WasPressedThisFrame()) TrySetColor(DropletColor.Yellow);
        if (controls.Player.SwitchBlue.WasPressedThisFrame()) TrySetColor(DropletColor.Blue);
    }

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
        if (!unlockedColors.Contains(newColor)) return;
        if (newColor == currentColor) return;

        currentColor = newColor;
        // El valor del enum es directamente el índice del slice en el material
        // (0=rojo, 1=amarillo, 2=azul), así que lo convertimos sin tabla de mapeo.
        droppy.SwitchColorTo((int)newColor); // cambia el color visual con fundido
        OnColorChanged?.Invoke(currentColor);
    }
}