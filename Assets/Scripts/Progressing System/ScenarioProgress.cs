using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ScenarioDirector : MonoBehaviour
{
    [Serializable]
    public sealed class Step
    {
        public string name;

        [Header("Unlock this step when ALL these flags are raised")]
        public List<string> requiredFlags = new List<string>();

        [Header("Unlocked mechanics (stay enabled once unlocked)")]
        [Tooltip("Drag scripts/components here (CPR scripts, AED scripts, gesture scripts, etc.)")]
        public List<Behaviour> enableBehaviours = new List<Behaviour>();

        [Tooltip("Optional: objects enabled once unlocked (UI, models, etc.)")]
        public List<GameObject> enableObjects = new List<GameObject>();

        [Tooltip("Optional: objects forced OFF until this step is unlocked (e.g., CPR pose GO).")]
        public List<GameObject> forceOffUntilUnlocked = new List<GameObject>();

        [Header("Events")]
        public UnityEvent onUnlocked;
    }

    [Header("Steps (in order)")]
    [SerializeField] private List<Step> steps = new List<Step>();

    [Header("Debug")]
    [SerializeField] private bool log = true;
    [SerializeField] private List<string> startingFlags = new List<string>();

    private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.Ordinal);

    // Tracks which steps are already unlocked
    private bool[] _unlocked;

    // Optional: an index you can use for UI/debug (“what step are we on now?”)
    private int _currentStepIndex;
    public int CurrentStepIndex => _currentStepIndex;
    public string CurrentStepName => (_currentStepIndex >= 0 && _currentStepIndex < steps.Count) ? steps[_currentStepIndex].name : "NONE";

    public event Action FlagsChanged;
    public event Action<int> StepUnlocked; // passes step index

    private void Awake()
    {
        for (int i = 0; i < startingFlags.Count; i++)
        {
            string f = Normalize(startingFlags[i]);
            if (!string.IsNullOrEmpty(f))
                _flags.Add(f);
        }
    }

    private void Start()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogError("[ScenarioDirector] No steps configured.", this);
            enabled = false;
            return;
        }

        _unlocked = new bool[steps.Count];

        // First apply "locked" state (force-off objects)
        ApplyUnlockState();

        // Evaluate immediately in case startingFlags unlock something
        EvaluateUnlocks();
    }

    public bool HasFlag(string flag)
    {
        flag = Normalize(flag);
        return !string.IsNullOrEmpty(flag) && _flags.Contains(flag);
    }

    public void RaiseFlag(string flag)
    {
        flag = Normalize(flag);
        if (string.IsNullOrEmpty(flag)) return;

        if (_flags.Add(flag))
        {
            if (log) Debug.Log($"[Scenario] Flag ON: {flag}", this);
            FlagsChanged?.Invoke();
            EvaluateUnlocks();
        }
    }

    public void ClearFlag(string flag)
    {
        flag = Normalize(flag);
        if (string.IsNullOrEmpty(flag)) return;

        if (_flags.Remove(flag))
        {
            if (log) Debug.Log($"[Scenario] Flag OFF: {flag}", this);
            FlagsChanged?.Invoke();

            // NOTE: We do NOT re-lock steps if flags are cleared.
            // This is intentional for milestone progression.
        }
    }

    private void EvaluateUnlocks()
    {
        // Unlock steps in order if their requirements are met.
        // This allows sequential progression, but you can also remove "break" to allow out-of-order unlocks.
        for (int i = 0; i < steps.Count; i++)
        {
            if (_unlocked[i])
                continue;

            if (IsStepReadyToUnlock(steps[i]))
            {
                UnlockStep(i);
                // Continue checking next steps in case multiple unlock instantly
                continue;
            }

            // If you want STRICT order, uncomment this:
            // break;
        }
    }

    private bool IsStepReadyToUnlock(Step s)
    {
        if (s.requiredFlags == null || s.requiredFlags.Count == 0)
            return false;

        for (int i = 0; i < s.requiredFlags.Count; i++)
        {
            string req = Normalize(s.requiredFlags[i]);
            if (string.IsNullOrEmpty(req)) continue;

            if (!_flags.Contains(req))
                return false;
        }
        return true;
    }

    private void UnlockStep(int index)
    {
        _unlocked[index] = true;
        _currentStepIndex = Mathf.Max(_currentStepIndex, index);

        if (log) Debug.Log($"[Scenario] Step UNLOCKED: {index} ({steps[index].name})", this);

        ApplyUnlockState();

        steps[index].onUnlocked?.Invoke();
        StepUnlocked?.Invoke(index);
    }

    private void ApplyUnlockState()
    {
        // Enable everything that belongs to unlocked steps.
        // Keep everything else disabled (locked).
        for (int si = 0; si < steps.Count; si++)
        {
            bool unlocked = _unlocked != null && si < _unlocked.Length && _unlocked[si];
            Step s = steps[si];

            // Behaviours
            if (s.enableBehaviours != null)
            {
                for (int i = 0; i < s.enableBehaviours.Count; i++)
                {
                    var b = s.enableBehaviours[i];
                    if (b != null) b.enabled = unlocked;
                }
            }

            // Objects enabled when unlocked
            if (s.enableObjects != null)
            {
                for (int i = 0; i < s.enableObjects.Count; i++)
                {
                    var go = s.enableObjects[i];
                    if (go != null) go.SetActive(unlocked);
                }
            }

            // Objects forced off until unlocked
            if (!unlocked && s.forceOffUntilUnlocked != null)
            {
                for (int i = 0; i < s.forceOffUntilUnlocked.Count; i++)
                {
                    var go = s.forceOffUntilUnlocked[i];
                    if (go != null) go.SetActive(false);
                }
            }
        }
    }

    private static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

    // Debug helpers
    public void DebugRaiseFlag(string f) => RaiseFlag(f);

    public bool IsStepUnlocked(int stepIndex)
    {
        if (_unlocked == null) return false;
        if (stepIndex < 0 || stepIndex >= _unlocked.Length) return false;
        return _unlocked[stepIndex];
    }
}
