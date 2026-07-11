using UnityEngine;

/// <summary>
/// Plays sound effects triggered by Animation Events, with a randomized pitch
/// each time so repeated sounds feel slightly different.
///
/// Must sit on the SAME GameObject as the Animator (Animation Events look here).
///
/// Event method names (case-sensitive):
///   PlayAttackEffects    -> attack clip
///   PlayWalkingEffects   -> drag / crawl clip
///   PlayIdleEffects      -> idle clip   (random pitch AND volume)
///   PlayHurtSound        -> hurt clip
///   PlayDeathSound       -> death clip
///
/// NOTE ON SHARED EVENTS: the VFX script already uses 'PlayHurtEffects' and
/// 'PlayDeathEffects'. Unity warns against two components sharing a method name,
/// so the hurt/death SOUNDS use their own names here. On those clips, add a
/// SECOND animation event (same frame is fine) pointing to PlayHurtSound /
/// PlayDeathSound.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DropletSFX : MonoBehaviour
{
    [Header("Audio source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip dragClip;    // walking / crawling
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Pitch randomization")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;

    [Header("Volume randomization (idle only)")]
    [SerializeField] private float minVolume = 0.7f;
    [SerializeField] private float maxVolume = 1f;

    // ------------------------------------------------------------------ //
    //  Animation Event targets — must be public.
    // ------------------------------------------------------------------ //

    public void PlayAttackEffects()
    {
        PlayRandomized(attackClip);
    }

    public void PlayWalkingEffects()
    {
        PlayRandomized(dragClip);
    }

    public void PlayIdleEffects()
    {
        // Idle also varies volume, not just pitch.
        PlayRandomized(idleClip, randomizeVolume: true);
    }

    public void PlayHurtSound()
    {
        PlayRandomized(hurtClip);
    }

    public void PlayDeathSound()
    {
        PlayRandomized(deathClip);
    }

    // ------------------------------------------------------------------ //
    //  Helper
    // ------------------------------------------------------------------ //

    private void PlayRandomized(AudioClip clip, bool randomizeVolume = false)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[DroppySFX] A clip is not assigned on {name}. Check the Inspector.", this);
            return;
        }

        // New random pitch every time -> each sound is a little different.
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // PlayOneShot takes a per-call volume scale; 1 = the clip's normal volume.
        float volume = randomizeVolume ? Random.Range(minVolume, maxVolume) : 1f;
        audioSource.PlayOneShot(clip, volume);
    }

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
}