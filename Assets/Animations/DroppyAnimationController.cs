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

        // Leave the material in a known, clean state.
        currentColorIndex = startingColorIndex;
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 0f);
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
        animator.SetTrigger(AttackHash);
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
        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        currentColorIndex = colorIndex;
        FinishColorTransition();
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

    /// <summary>Leaves the material resting on currentColorIndex with Blend = 0.</summary>
    private void FinishColorTransition()
    {
        material.SetFloat(fromId, currentColorIndex);
        material.SetFloat(toId, currentColorIndex);
        material.SetFloat(blendId, 0f);
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