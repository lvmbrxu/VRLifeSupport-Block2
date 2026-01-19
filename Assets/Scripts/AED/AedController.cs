using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public sealed class AedController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Header("Flags")]
    [SerializeField] private string aedArrivedFlag = "AedArrived";
    [SerializeField] private string padAPlacedFlag = "PadAPlaced";
    [SerializeField] private string padBPlacedFlag = "PadBPlaced";

    [Tooltip("Optional flags to raise")]
    [SerializeField] private string aedPoweredOnFlag = "AedPoweredOn";
    [SerializeField] private string padsAppliedFlag = "AedPadsApplied";
    [SerializeField] private string shockPressedFlag = "AedShockPressed";

    [Header("Buttons (XRSimpleInteractable)")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable powerButton;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable shockButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip aedSound1_powerOn;
    [SerializeField] private AudioClip aedSound2_afterPads;
    [SerializeField] private AudioClip aedSound3;
    [SerializeField] private AudioClip aedSound4;
    [SerializeField] private AudioClip aedSound5_shock;

    [Header("Timing")]
    [SerializeField] private float delayAfterSound2 = 5f;
    [SerializeField] private float delayAfterSound3 = 5f;

    [Header("Debug")]
    [SerializeField] private bool log;

    private bool _poweredOn;
    private bool _padsSoundDone;
    private Coroutine _sequenceRoutine;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (audioSource != null) audioSource.playOnAwake = false;

        // Disable buttons until AED arrives (simple)
        SetButtonEnabled(powerButton, false);
        SetButtonEnabled(shockButton, false);
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Evaluate;

        if (powerButton != null) powerButton.selectEntered.AddListener(OnPowerPressed);
        if (shockButton != null) shockButton.selectEntered.AddListener(OnShockPressed);

        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;

        if (powerButton != null) powerButton.selectEntered.RemoveListener(OnPowerPressed);
        if (shockButton != null) shockButton.selectEntered.RemoveListener(OnShockPressed);
    }

    private void Evaluate()
    {
        // Buttons only become available after AED arrives
        bool arrived = scenario != null && scenario.HasFlag(aedArrivedFlag);
        SetButtonEnabled(powerButton, arrived);

        // Shock button only after we reach the “ready to shock” point (we enable it later in the sequence)
        if (!arrived)
            SetButtonEnabled(shockButton, false);

        // If powered on and both pads placed, trigger sound2 + timed sequence once
        if (arrived && _poweredOn && !_padsSoundDone && PadsPlaced())
        {
            _padsSoundDone = true;
            if (!string.IsNullOrWhiteSpace(padsAppliedFlag)) scenario.RaiseFlag(padsAppliedFlag);

            if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = StartCoroutine(PadsAppliedSequence());
        }
    }

    private void OnPowerPressed(SelectEnterEventArgs args)
    {
        if (_poweredOn) return;

        // Only if AED arrived
        if (scenario == null || !scenario.HasFlag(aedArrivedFlag))
            return;

        _poweredOn = true;
        if (!string.IsNullOrWhiteSpace(aedPoweredOnFlag)) scenario.RaiseFlag(aedPoweredOnFlag);

        PlayOneShot(aedSound1_powerOn);

        if (log) Debug.Log("[AED] Power pressed", this);

        // Re-check pad placement state
        Evaluate();
    }

    private void OnShockPressed(SelectEnterEventArgs args)
    {
        // Only allow if enabled (we enable after sound4 finishes)
        if (shockButton == null || !shockButton.enabled)
            return;

        PlayOneShot(aedSound5_shock);

        if (scenario != null && !string.IsNullOrWhiteSpace(shockPressedFlag))
            scenario.RaiseFlag(shockPressedFlag);

        if (log) Debug.Log("[AED] Shock pressed", this);

        // Optional: disable shock after pressed
        SetButtonEnabled(shockButton, false);
    }

    private IEnumerator PadsAppliedSequence()
    {
        // Sound2: after pads applied
        PlayOneShot(aedSound2_afterPads);

        // Wait for sound2 to end, then +5 seconds
        yield return WaitForClipEnd(aedSound2_afterPads);
        yield return new WaitForSeconds(delayAfterSound2);

        // Sound3
        PlayOneShot(aedSound3);
        yield return WaitForClipEnd(aedSound3);
        yield return new WaitForSeconds(delayAfterSound3);

        // Sound4
        PlayOneShot(aedSound4);
        yield return WaitForClipEnd(aedSound4);

        // Now allow shock press
        SetButtonEnabled(shockButton, true);

        if (log) Debug.Log("[AED] Ready for shock (shock button enabled)", this);
    }

    private bool PadsPlaced()
    {
        return scenario != null &&
               scenario.HasFlag(padAPlacedFlag) &&
               scenario.HasFlag(padBPlacedFlag);
    }

    private void SetButtonEnabled(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable b, bool on)
    {
        if (b == null) return;
        b.enabled = on;
        // Optional: hide/show visuals if you want:
        // b.gameObject.SetActive(on);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator WaitForClipEnd(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            yield break;

        // If another clip interrupts, this will end early, which is fine for a student-simple setup.
        float t = clip.length;
        yield return new WaitForSeconds(t);
    }
}
