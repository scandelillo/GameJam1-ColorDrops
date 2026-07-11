using System.Collections;
using UnityEngine;

/// <summary>
/// Central animation facade for the Droppy character.
/// Other scripts should ONLY talk to this class — never to the Animator,
/// the SkinnedMeshRenderer or the material directly.
///
/// Covers three independent channels:
///   1. Body/face poses  -> Animator (triggers + Speed float)
///   2. Inflate amount   -> blendshape weight (0..1, settable from outside)
///   3. Body color       -> material From/To/Blend transition (any-to-any)
/// </summary>
public class DroppyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer bodyRenderer;

    [Header("Reinicio del ataque (evita atascos al spamear)")]
    // Capa donde vive el estado de ataque del cuerpo (Body Animation = 0).
    [SerializeField] private int attackLayer = 0;
    // Nombre EXACTO del estado de ataque del cuerpo, para forzar su reinicio en cada Attack().
    // Si queda vacío o no existe, se usa solo el trigger (comportamiento normal).
    [SerializeField] private string attackStateName = "Droppy_Rig|Droppy_Attack";

    [Header("Inflate blendshape")]
    [SerializeField] private string inflateBlendShapeName = "Droppy_Inflated_BlendShape";

    [Header("Color transition")]
    [Tooltip("How fast the color cross-fade completes. 2 = half a second.")]
    [SerializeField] private float colorTransitionSpeed = 2f;
    [Tooltip("Slice index the character starts with (must match the material).")]
    [SerializeField] private int startingColorIndex = 0;

    [Header("Shader reference names (check the Shader Graph Blackboard!)")]
    [SerializeField] private string blendProperty = "_Blend";
    [SerializeField] private string fromIndexProperty = "_FromIndex";
    [SerializeField] private string toIndexProperty = "_ToIndex";

    [Header("Destello de daño (veneno)")]
    // Slice hacia el que parpadea el daño. -1 = usa un color vecino automáticamente.
    [SerializeField] private int damageFlashSliceIndex = -1;
    // Qué tanto mezcla hacia ese color en el destello (0 = nada, 1 = cambio total). Sutil ~0.3.
    [SerializeField] private float damageFlashDepth = 0.3f;
    // Duración total del destello (ir y volver), en segundos.
    [SerializeField] private float damageFlashDuration = 0.18f;

    // ---- Animator parameter hashes (faster + typo-proof after startup) ----
    private static readonly int SpeedHash   = Animator.StringToHash("Speed");
    private static readonly int AttackHash  = Animator.StringToHash("Attack");
    private static readonly int HurtHash    = Animator.StringToHash("Hurt");
    private static readonly int DeathHash   = Animator.StringToHash("Death");
    private static readonly int InflateHash = Animator.StringToHash("Inflate");

    // ---- Internal state ----
    private Material material;          // instance, safe to write to
    private int blendId, fromId, toId;  // shader property ids
    private int inflateShapeIndex = -1;
    private int currentColorIndex;
    private Coroutine colorRoutine;
    // Destello de daño en curso (independiente del cambio de color real).
    private Coroutine damageFlashRoutine;
    // Hash del estado de ataque del cuerpo (para forzar su reinicio con Play).
    private int attackStateHash;

    /// <summary>Slice index currently shown (or being transitioned TO).</summary>
    public int CurrentColorIndex => currentColorIndex;

    /// <summary>True while a color cross-fade is running.</summary>
    public bool IsChangingColor => colorRoutine != null;

    // ------------------------------------------------------------------ //
    //  Setup
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (animator == null)      animator = GetComponent<Animator>();
        if (bodyRenderer == null)  bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // Precalculamos el hash del estado de ataque para poder reiniciarlo con Play().
        if (!string.IsNullOrEmpty(attackStateName))
            attackStateHash = Animator.StringToHash(attackStateName);

        // .material returns a per-object instance, so writing _Blend here
        // never touches the shared asset or other characters.
        material = bodyRenderer.material;

        blendId = Shader.PropertyToID(blendProperty);
        fromId  = Shader.PropertyToID(fromIndexProperty);
        toId    = Shader.PropertyToID(toIndexProperty);

        inflateShapeIndex = bodyRenderer.sharedMesh.GetBlendShapeIndex(inflateBlendShapeName);
        if (inflateShapeIndex < 0)
            Debug.LogError($"[DroppyAnimation] Blendshape '{inflateBlendShapeName}' not found on {bodyRenderer.name}. " +
                           "Check the exact name in the SkinnedMeshRenderer's BlendShapes foldout.", this);

        // Estado inicial limpio con la CONVENCIÓN DE DESTINO:
        // el color actual es siempre el _ToIndex, con el slider (_Blend) en 1 (mostrando el destino).
        currentColorIndex = startingColorIndex;
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 1f);
    }

    // ------------------------------------------------------------------ //
    //  1. Body / face animations
    //     Call these from movement, combat, etc. — nothing else needed.
    // ------------------------------------------------------------------ //

    /// <summary>Drives Idle vs Walking. Pass the character's current speed.</summary>
    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void Attack()
    {
        // El trigger mueve la cara (capa Face) y, como respaldo, el cuerpo.
        animator.SetTrigger(AttackHash);

        // Forzamos que el clip de ataque del CUERPO reinicie desde el frame 0.
        // Con solo el trigger, al spamear a veces el disparo se pierde o se queda
        // atorado en el retorno a Idle; con Play() el reinicio es garantizado.
        if (attackStateHash != 0 && animator.HasState(attackLayer, attackStateHash))
            animator.Play(attackStateHash, attackLayer, 0f);
    }

    public void Hurt()
    {
        animator.SetTrigger(HurtHash);
    }

    public void Die()
    {
        animator.SetTrigger(DeathHash);
    }

    /// <summary>
    /// Fires the 'Inflate' trigger (the inflate-mode face/state).
    /// NOTE: this only switches the Animator state. The actual body
    /// expansion is controlled separately with SetInflateAmount().
    /// </summary>
    public void TriggerInflateState()
    {
        animator.SetTrigger(InflateHash);
    }

    // ------------------------------------------------------------------ //
    //  2. Inflate blendshape
    //     The currency script will call SetInflateAmount(percentage).
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Sets how inflated the body is. 0 = normal, 1 = fully inflated.
    /// Safe to call every frame.
    /// </summary>
    public void SetInflateAmount(float amount01)
    {
        if (inflateShapeIndex < 0) return;

        float weight = Mathf.Clamp01(amount01) * 100f; // blendshape weights go 0..100
        bodyRenderer.SetBlendShapeWeight(inflateShapeIndex, weight);
    }

    /// <summary>Current inflate amount, 0..1.</summary>
    public float GetInflateAmount()
    {
        if (inflateShapeIndex < 0) return 0f;
        return bodyRenderer.GetBlendShapeWeight(inflateShapeIndex) / 100f;
    }

    // ------------------------------------------------------------------ //
    //  3. Body color (material From/To/Blend)
    //     Buttons call SwitchColorTo(sliceIndex) — that's all.
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Cross-fades the body color to the given texture-array slice,
    /// directly from whatever color is showing now (no in-between colors).
    /// If a transition is already running it is committed first, then the
    /// new one starts from there.
    /// </summary>
    public void SwitchColorTo(int colorIndex)
    {
        if (colorIndex == currentColorIndex && colorRoutine == null)
            return; // already there, nothing to do

        // Un cambio de color real manda sobre cualquier destello de daño en curso.
        StopDamageFlash();

        if (colorRoutine != null)
        {
            // Commit the transition in progress so the new one has a
            // clean starting point (tiny pop, but never a wrong color).
            StopCoroutine(colorRoutine);
            colorRoutine = null;
            FinishColorTransition();
        }

        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, colorIndex);
        material.SetFloat(blendId, 0f);

        colorRoutine = StartCoroutine(ColorTransitionRoutine(colorIndex));
    }

    /// <summary>Jumps to a color instantly, no fade.</summary>
    public void SetColorInstant(int colorIndex)
    {
        // Cortamos cualquier transición o destello: el color queda fijo y limpio.
        StopDamageFlash();
        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        currentColorIndex = colorIndex;
        FinishColorTransition();
    }

    /// <summary>
    /// Destello SUTIL de color para reflejar daño (p. ej. el veneno del suelo).
    /// Mezcla un poquito hacia un color vecino y vuelve, SIN cambiar el color lógico.
    /// Se ignora si hay una transición de color real en curso (esa ya es feedback de sobra).
    /// </summary>
    public void FlashDamage()
    {
        // No pisamos un cambio de color real en curso.
        if (colorRoutine != null) return;

        StopDamageFlash();
        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        // Color hacia el que "parpadea" el daño: el configurado, o un vecino válido.
        int flashIndex = damageFlashSliceIndex >= 0
            ? damageFlashSliceIndex
            : (currentColorIndex > 0 ? currentColorIndex - 1 : currentColorIndex + 1);

        // Convención: _ToIndex = color actual, _FromIndex = destello; el _Blend baja de 1
        // (todo color actual) hacia minBlend (una pizca del vecino) y vuelve a 1.
        material.SetFloat(fromId, flashIndex);
        material.SetFloat(toId, currentColorIndex);

        float mitad = Mathf.Max(0.01f, damageFlashDuration * 0.5f);
        float minBlend = Mathf.Clamp01(1f - damageFlashDepth);

        // Ir: 1 -> minBlend.
        float t = 0f;
        while (t < mitad)
        {
            t += Time.deltaTime;
            material.SetFloat(blendId, Mathf.Lerp(1f, minBlend, t / mitad));
            yield return null;
        }

        // Volver: minBlend -> 1.
        t = 0f;
        while (t < mitad)
        {
            t += Time.deltaTime;
            material.SetFloat(blendId, Mathf.Lerp(minBlend, 1f, t / mitad));
            yield return null;
        }

        // Reposo limpio (color actual como destino, _Blend en 1).
        FinishColorTransition();
        damageFlashRoutine = null;
    }

    // Corta el destello de daño si está activo (lo llama todo cambio de color real).
    private void StopDamageFlash()
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }
    }

    private IEnumerator ColorTransitionRoutine(int targetIndex)
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

    // Deja el material en reposo mostrando el color actual como DESTINO (_ToIndex) con el slider en 1.
    private void FinishColorTransition()
    {
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 1f);
    }

    // ------------------------------------------------------------------ //
    //  Cleanup
    // ------------------------------------------------------------------ //

    private void OnDestroy()
    {
        // .material created an instance in Awake; destroy it to avoid leaks.
        if (material != null)
            Destroy(material);
    }
}