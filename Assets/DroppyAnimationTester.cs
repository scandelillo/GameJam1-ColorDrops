using UnityEngine;

/// <summary>
/// TEMPORARY test driver for DroppyAnimationController.
/// Attach it next to the controller, press Play and use the keyboard.
/// Delete this file once everything is verified.
///
/// Controls:
///   W (hold)      -> Speed = 1 (walking), release = 0 (idle)
///   Space         -> Attack
///   H             -> Hurt
///   K             -> Die
///   J             -> Inflate state trigger (Animator)
///   Up / Down     -> Inflate amount +/- (blendshape)
///   1 / 2 / 3 / 4 / 5 -> Switch color to slice 0..4
/// </summary>
public class DroppyAnimationTester : MonoBehaviour
{
    [SerializeField] private DroppyAnimationController droppy;

    private float inflate;

    private void Awake()
    {
        if (droppy == null) droppy = GetComponent<DroppyAnimationController>();
    }

    private void Update()
    {
        // --- 1. Locomotion (idle <-> walking) ---
        droppy.SetSpeed(Input.GetKey(KeyCode.W) ? 1f : 0f);

        // --- 2. Action triggers ---
        if (Input.GetKeyDown(KeyCode.Space)) droppy.Attack();
        if (Input.GetKeyDown(KeyCode.H))     droppy.Hurt();
        if (Input.GetKeyDown(KeyCode.K))     droppy.Die();
        if (Input.GetKeyDown(KeyCode.J))     droppy.TriggerInflateState();

        // --- 3. Inflate blendshape (hold to grow/shrink) ---
        if (Input.GetKey(KeyCode.UpArrow))   inflate += Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) inflate -= Time.deltaTime;
        inflate = Mathf.Clamp01(inflate);
        droppy.SetInflateAmount(inflate);

        // --- 4. Color switching (any-to-any) ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) droppy.SwitchColorTo(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) droppy.SwitchColorTo(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) droppy.SwitchColorTo(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) droppy.SwitchColorTo(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) droppy.SwitchColorTo(4);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 340, 190), GUI.skin.box);
        GUILayout.Label("<b>Droppy tester</b>  (delete me later)",
            new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"Color index: {droppy.CurrentColorIndex}" +
                        (droppy.IsChangingColor ? "  (fading...)" : ""));
        GUILayout.Label($"Inflate: {inflate:P0}   (Up/Down arrows)");
        GUILayout.Label("W walk | Space attack | H hurt | K die | J inflate state");
        GUILayout.Label("1-5 switch color");
        GUILayout.EndArea();
    }
}
