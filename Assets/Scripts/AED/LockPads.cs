using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public sealed class LockPadInSocket : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string placedFlag = "PadPlaced";

    [Header("Socket")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    [Header("Locking")]
    [Tooltip("If true, pad cannot be grabbed after placed.")]
    [SerializeField] private bool disableGrabOnPlaced = true;

    [Tooltip("Also force the rigidbody kinematic for stability.")]
    [SerializeField] private bool forceKinematic = true;

    [Header("Debug")]
    [SerializeField] private bool log;

    private bool _done;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (socket == null) socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    private void OnEnable()
    {
        if (socket != null) socket.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        if (socket != null) socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_done) return;
        _done = true;

        // Raise flag
        if (scenario != null && !string.IsNullOrWhiteSpace(placedFlag))
            scenario.RaiseFlag(placedFlag);

        // Lock the pad
        var go = args.interactableObject.transform.gameObject;

        if (disableGrabOnPlaced)
        {
            var grab = go.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
        }

        if (forceKinematic)
        {
            var rb = go.GetComponentInParent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        if (log) Debug.Log($"[LockPadInSocket] Locked {go.name} and raised {placedFlag}", this);
    }
}