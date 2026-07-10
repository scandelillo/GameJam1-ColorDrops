using UnityEngine;

/// <summary>
/// Controlador de animación y color para las gotas enemigas.
/// Similar a DroppyAnimationController pero simplificado para enemigos.
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class DropletAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SkinnedMeshRenderer bodyRenderer;
    [SerializeField] private Animator animator;

    [Header("Color Transition")]
    [SerializeField] private float colorTransitionSpeed = 2f;
    [SerializeField] private int startingColorIndex = 0;

    [Header("Shader Properties")]
    [SerializeField] private string blendProperty = "_Blend";
    [SerializeField] private string fromIndexProperty = "_FromIndex";
    [SerializeField] private string toIndexProperty = "_ToIndex";

    [Header("Animación")]
    [SerializeField] private float animationSpeedMultiplier = 1f;

    // Hashes para el Animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int ColorIndexHash = Animator.StringToHash("ColorIndex");

    // Estado interno
    private Material material;
    private int blendId, fromId, toId;
    private int currentColorIndex;
    private Coroutine colorRoutine;

    public int CurrentColorIndex => currentColorIndex;
    public bool IsChangingColor => colorRoutine != null;

    private void Awake()
    {
        // Obtener referencias
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SkinnedMeshRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        // Crear instancia del material
        material = bodyRenderer.material;

        // Obtener IDs de propiedades del shader
        blendId = Shader.PropertyToID(blendProperty);
        fromId = Shader.PropertyToID(fromIndexProperty);
        toId = Shader.PropertyToID(toIndexProperty);

        // Inicializar color
        currentColorIndex = startingColorIndex;
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 0f);

        // Inicializar animator
        if (animator != null)
        {
            animator.SetInteger(ColorIndexHash, currentColorIndex);
        }
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    // ---- MÉTODOS PÚBLICOS ----

    /// <summary>
    /// Establece la velocidad para la animación de movimiento
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, speed * animationSpeedMultiplier);
        }
    }

    /// <summary>
    /// Dispara la animación de ataque
    /// </summary>
    public void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }
    }

    /// <summary>
    /// Dispara la animación de daño
    /// </summary>
    public void Hurt()
    {
        if (animator != null)
        {
            animator.SetTrigger(HurtHash);
        }
    }

    /// <summary>
    /// Dispara la animación de muerte
    /// </summary>
    public void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger(DeathHash);
        }
    }

    /// <summary>
    /// Cambia el color con transición suave
    /// </summary>
    public void SwitchColorTo(int colorIndex)
    {
        if (colorIndex == currentColorIndex && colorRoutine == null)
            return;

        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
            FinishColorTransition();
        }

        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, colorIndex);
        material.SetFloat(blendId, 0f);

        // También actualizar el Animator si tiene el parámetro
        if (animator != null)
        {
            animator.SetInteger(ColorIndexHash, colorIndex);
        }

        colorRoutine = StartCoroutine(ColorTransitionRoutine(colorIndex));
    }

    /// <summary>
    /// Cambia el color instantáneamente
    /// </summary>
    public void SetColorInstant(int colorIndex)
    {
        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        currentColorIndex = colorIndex;
        FinishColorTransition();

        if (animator != null)
        {
            animator.SetInteger(ColorIndexHash, colorIndex);
        }
    }

    private System.Collections.IEnumerator ColorTransitionRoutine(int targetIndex)
    {
        float blend = 0f;

        while (blend < 1f)
        {
            blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime * colorTransitionSpeed);
            material.SetFloat(blendId, blend);
            yield return null;
        }

        currentColorIndex = targetIndex;
        FinishColorTransition();
        colorRoutine = null;
    }

    private void FinishColorTransition()
    {
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 0f);
    }
}