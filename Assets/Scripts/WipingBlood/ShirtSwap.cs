using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public sealed class ShirtSwapOnGrab : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string requiredFlag = "AedArrived";
    [SerializeField] private string shirtRemovedFlag = "ShirtRemoved";

    [Header("XR")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    [Header("Swap")]
    [Tooltip("Object for the intact shirt (usually this object or its mesh root).")]
    [SerializeField] private GameObject intactShirt;

    [Tooltip("Object for the ripped shirt model (start disabled).")]
    [SerializeField] private GameObject rippedShirt;

    [Tooltip("Optional: hide/remove the grabbed shirt object after swap.")]
    [SerializeField] private bool disableThisAfterSwap = true;

    [Header("Debug")]
    [SerializeField] private bool log;

    private bool _done;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (grab == null) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (rippedShirt != null) rippedShirt.SetActive(false);
        ApplyGate();
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += ApplyGate;
        if (grab != null) grab.selectEntered.AddListener(OnGrabbed);
        ApplyGate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= ApplyGate;
        if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
    }

    private void ApplyGate()
    {
        bool allowed = scenario == null || string.IsNullOrWhiteSpace(requiredFlag) || scenario.HasFlag(requiredFlag);
        if (grab != null) grab.enabled = allowed;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_done) return;

        if (scenario != null && !string.IsNullOrWhiteSpace(requiredFlag) && !scenario.HasFlag(requiredFlag))
            return;

        _done = true;

        if (intactShirt != null) intactShirt.SetActive(false);
        if (rippedShirt != null) rippedShirt.SetActive(true);

        if (scenario != null && !string.IsNullOrWhiteSpace(shirtRemovedFlag))
            scenario.RaiseFlag(shirtRemovedFlag);

        if (log) Debug.Log("[Shirt] Shirt removed -> swapped to ripped model", this);

        if (disableThisAfterSwap)
        {
            // Stop any further interaction
            if (grab != null) grab.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
