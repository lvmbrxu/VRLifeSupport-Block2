using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class FlagTextMarker : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;

    [Tooltip("If set, marker only shows when this flag is ON. Leave empty to ignore.")]
    [SerializeField] private string showWhenFlag;

    [Tooltip("If set, marker hides when this flag is ON. Leave empty to ignore.")]
    [SerializeField] private string hideWhenFlag;

    [Header("Visual Root (this gets toggled, NOT the whole GameObject)")]
    [Tooltip("Assign the Canvas/Text parent here (child object). This will be enabled/disabled.")]
    [SerializeField] private GameObject visualRoot;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI text;
    [TextArea] [SerializeField] private string message = "POINT";

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Transform cameraTransform;

    [Header("Debug")]
    [SerializeField] private bool log;

    private void Reset()
    {
        // Try to auto-pick first child as visual root
        if (transform.childCount > 0)
            visualRoot = transform.GetChild(0).gameObject;
    }

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0).gameObject;

        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text != null)
            text.text = message;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Refresh(); // safe initial refresh
    }

    private void OnEnable()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (scenario != null)
            scenario.FlagsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (scenario != null)
            scenario.FlagsChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (!faceCamera || cameraTransform == null) return;
        if (visualRoot == null || !visualRoot.activeSelf) return;

        Vector3 forward = transform.position - cameraTransform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private void Refresh()
    {
        if (scenario == null)
        {
            // Try again (helps if ScenarioDirector loads later)
            scenario = FindFirstObjectByType<ScenarioDirector>();
            if (scenario == null)
            {
                if (log) Debug.LogWarning("[FlagTextMarker] No ScenarioDirector found.", this);
                return;
            }
        }

        bool showOk = string.IsNullOrWhiteSpace(showWhenFlag) || scenario.HasFlag(showWhenFlag.Trim());
        bool hideOk = !string.IsNullOrWhiteSpace(hideWhenFlag) && scenario.HasFlag(hideWhenFlag.Trim());

        bool visible = showOk && !hideOk;

        if (visualRoot != null)
            visualRoot.SetActive(visible);

        if (log)
            Debug.Log($"[FlagTextMarker] {name} visible={visible} (show={showWhenFlag}, hide={hideWhenFlag})", this);
    }
}
