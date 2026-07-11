using UnityEngine;

// Única fuente del color de una entidad (jugador o enemigo).
// Guarda el color lógico Y actualiza el visual llamando al DroppyAnimationController.
// Así, el color que usan el combate y la IA SIEMPRE coincide con el que se ve en pantalla
// (esto evita el bug de "se ve un color pero lógicamente es otro").
public class EntityColor : MonoBehaviour
{
    [Header("Color")]
    // Color inicial de la entidad.
    [SerializeField] private DropletColor colorActual = DropletColor.Rojo;
    // Si es true, el cambio de color se hace con fundido; si es false, instantáneo.
    [SerializeField] private bool conFundido = true;

    // Referencia al visual (animación + material). Suele estar en el hijo del modelo.
    [SerializeField] private DroppyAnimationController visual;

    // Consulta pública del color lógico actual.
    public DropletColor ColorActual => colorActual;

    private void Awake()
    {
        // Si no se asignó a mano, lo buscamos en este objeto o en sus hijos.
        if (visual == null)
            visual = GetComponentInChildren<DroppyAnimationController>();
    }

    private void Start()
    {
        // Pintamos el color inicial (instantáneo) en Start, para asegurar que el
        // DroppyAnimationController ya creó su instancia de material en su Awake.
        if (visual != null)
            visual.SetColorInstant((int)colorActual);
    }

    // Cambia el color lógico y el visual a la vez. Único punto de cambio de color.
    public void EstablecerColor(DropletColor nuevoColor)
    {
        // Si ya somos ese color, no hacemos nada.
        if (nuevoColor == colorActual) return;

        colorActual = nuevoColor;

        // El valor del enum es directamente el índice del slice en el material.
        if (visual != null)
        {
            if (conFundido)
                visual.SwitchColorTo((int)nuevoColor);
            else
                visual.SetColorInstant((int)nuevoColor);
        }
    }
}
