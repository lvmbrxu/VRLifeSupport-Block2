using UnityEngine;
using UnityEngine.Events;

public class BreathingCheckTrigger : MonoBehaviour
{
    [Header("Player Head (XR Camera)")]
    [Tooltip("Assign your XR camera transform. If empty, will use Camera.main.")]
    public Transform playerHead;

    [Header("Settings")]
    [Tooltip("How long the head must stay in the zone to count as a breathing check.")]
    public float holdSeconds = 1.5f;

    [Tooltip("If true, only counts if head is also within this distance to the zone center (extra safety).")]
    public bool useDistanceCheck = true;

    public float maxDistanceToCenter = 0.25f;

    [Header("Events")]
    public UnityEvent onBreathingChecked;

    [Header("Debug")]
    public bool logDebug = true;

    private float _timer;
    private bool _completed;
    private bool _headInsideTrigger;

    private void Awake()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;
    }

    private void Update()
    {
        if (_completed) return;
        if (!_headInsideTrigger) return;
        if (playerHead == null) return;

        if (useDistanceCheck)
        {
            float d = Vector3.Distance(playerHead.position, transform.position);
            if (d > maxDistanceToCenter)
            {
                _timer = 0f;
                return;
            }
        }

        _timer += Time.deltaTime;

        if (_timer >= holdSeconds)
        {
            _completed = true;
            if (logDebug) Debug.Log("[BreathingCheck] Breathing checked!", this);
            onBreathingChecked?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // We don't depend on 'other' being a special collider.
        // Just assume player is near enough to enter trigger.
        _headInsideTrigger = true;
        // Don't reset timer here; lets the player dip in/out slightly
    }

    private void OnTriggerExit(Collider other)
    {
        _headInsideTrigger = false;
        _timer = 0f;
    }

    // Optional: allow re-checking in play mode if needed
    public void ResetCheck()
    {
        _completed = false;
        _timer = 0f;
        _headInsideTrigger = false;
    }
}
