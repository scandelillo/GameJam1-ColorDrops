using UnityEngine;

/// <summary>
/// Plays particle effects from Animation Events, choosing the one that matches
/// the character's CURRENT color (read from DroppyAnimationController).
///
/// Must sit on the SAME GameObject as the Animator (Animation Events look here).
///
/// Fill each array with one particle system PER COLOR, in the same order as the
/// texture-array slices:  element 0 = color 0, element 1 = color 1, ...
/// The event method names must match EXACTLY (case-sensitive):
///   PlayHurtEffects
///   PlayDeathEffects
/// </summary>
public class DroppyEffects : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private DroppyAnimationController droppy;

    [Header("Effects per color (index = color index)")]
    [Tooltip("One hurt particle system per color, ordered to match the color slices.")]
    [SerializeField] private ParticleSystem[] hurtEffectsByColor;

    [Tooltip("One death particle system per color, ordered to match the color slices.")]
    [SerializeField] private ParticleSystem[] deathEffectsByColor;

    // ------------------------------------------------------------------ //
    //  Animation Event targets — must be public.
    //  Hook these from the Animation window (Add Event on the clip).
    // ------------------------------------------------------------------ //

    public void PlayHurtEffects()
    {
        PlayForCurrentColor(hurtEffectsByColor);
    }

    public void PlayDeathEffects()
    {
        PlayForCurrentColor(deathEffectsByColor);
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private void PlayForCurrentColor(ParticleSystem[] systems)
    {
        if (systems == null || systems.Length == 0)
        {
            Debug.LogWarning("[DroppyEffects] No effects assigned for this event.", this);
            return;
        }

        int color = (droppy != null) ? droppy.CurrentColorIndex : 0;

        // Clamp so a missing/short slot never throws — falls back to the last one.
        int i = Mathf.Clamp(color, 0, systems.Length - 1);
        ParticleSystem ps = systems[i];

        if (ps == null)
        {
            Debug.LogWarning($"[DroppyEffects] Effect slot {i} is empty.", this);
            return;
        }

        // Restart cleanly so rapid re-triggers don't overlap oddly.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.Clear(true);
        ps.Play(true);
    }

    private void Awake()
    {
        if (droppy == null) droppy = GetComponent<DroppyAnimationController>();
    }
}