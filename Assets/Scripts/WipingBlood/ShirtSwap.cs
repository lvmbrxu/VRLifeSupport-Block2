using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class ShirtSwapOnPoke : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string requiredFlag = "AedArrived";
    [SerializeField] private string shirtRemovedFlag = "ShirtRemoved";

    [Header("XR (put this on the HANDLE object)")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    [Header("Swap Targets")]
    [SerializeField] private GameObject intactShirt;
    [SerializeField] private GameObject rippedShirt;

    [Header("After Swap")]
    [SerializeField] private bool disableHandleAfterSwap = true;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool _done;

    private void Awake()
    {
        if (scenario == null) scenario = FindFirstObjectByType<ScenarioDirector>();
        if (interactable == null) interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (rippedShirt != null) rippedShirt.SetActive(false);
        ApplyGate();
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += ApplyGate;
        if (interactable != null) interactable.selectEntered.AddListener(OnSelectEntered);
        ApplyGate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= ApplyGate;
        if (interactable != null) interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void ApplyGate()
    {
        bool allowed =
            scenario == null ||
            string.IsNullOrWhiteSpace(requiredFlag) ||
            scenario.HasFlag(requiredFlag);

        if (interactable != null) interactable.enabled = allowed;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_done) return;

        // Only allow poke (prevents ray/controller select triggering it)
        if (args.interactorObject is not XRPokeInteractor)
            return;

        if (scenario != null && !string.IsNullOrWhiteSpace(requiredFlag) && !scenario.HasFlag(requiredFlag))
            return;

        _done = true;

        if (intactShirt != null) intactShirt.SetActive(false);
        if (rippedShirt != null) rippedShirt.SetActive(true);

        if (scenario != null && !string.IsNullOrWhiteSpace(shirtRemovedFlag))
            scenario.RaiseFlag(shirtRemovedFlag);

        if (log) Debug.Log("[Shirt] Poked -> swapped to ripped model", this);

        if (disableHandleAfterSwap)
            gameObject.SetActive(false);
    }
}
