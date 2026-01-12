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

        [Header("Complete this step when ALL these flags are raised")]
        public List<string> requiredFlags = new List<string>();

        [Header("Mechanics enabled while this step is ACTIVE")]
        [Tooltip("Drag scripts/components here (CPR scripts, AED scripts, gesture scripts, etc.)")]
        public List<Behaviour> enableBehaviours = new List<Behaviour>();

        [Tooltip("Optional: objects enabled while this step is ACTIVE (UI, models, etc.)")]
        public List<GameObject> enableObjects = new List<GameObject>();

        [Tooltip("Optional: objects forced OFF while this step is ACTIVE (if needed)")]
        public List<GameObject> forceOffObjects = new List<GameObject>();

        [Header("Events")]
        public UnityEvent onEnter;
        public UnityEvent onExit;
    }

    [Header("Steps (in order)")]
    [SerializeField] private List<Step> steps = new List<Step>();

    [Header("Debug")]
    [SerializeField] private bool log = true;
    [SerializeField] private List<string> startingFlags = new List<string>();

    private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.Ordinal);
    private int _stepIndex;

    public int StepIndex => _stepIndex;
    public string CurrentStepName => (_stepIndex >= 0 && _stepIndex < steps.Count) ? steps[_stepIndex].name : "NONE";

    public event Action FlagsChanged;
    public event Action<int> StepChanged;

    private void Awake()
    {
        // preload starting flags
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

        _stepIndex = Mathf.Clamp(_stepIndex, 0, steps.Count - 1);
        ApplyStepActivation(initial: true);
        EvaluateProgression(); // in case startingFlags already satisfy step 0
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
            if (log) Debug.Log($"[Scenario] Flag ON: {flag}  (Step: {CurrentStepName})", this);
            FlagsChanged?.Invoke();
            EvaluateProgression();
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
        }
    }

    private void EvaluateProgression()
    {
        // advance while current step is complete
        while (_stepIndex < steps.Count && IsStepComplete(steps[_stepIndex]))
        {
            if (_stepIndex >= steps.Count - 1)
                return;

            ChangeStep(_stepIndex + 1);
        }
    }

    private bool IsStepComplete(Step s)
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

    private void ChangeStep(int newIndex)
    {
        if (newIndex == _stepIndex) return;

        Step oldStep = steps[_stepIndex];
        if (log) Debug.Log($"[Scenario] Exit step {_stepIndex}: {oldStep.name}", this);
        oldStep.onExit?.Invoke();

        _stepIndex = Mathf.Clamp(newIndex, 0, steps.Count - 1);

        ApplyStepActivation(initial: false);

        StepChanged?.Invoke(_stepIndex);

        if (log) Debug.Log($"[Scenario] Enter step {_stepIndex}: {steps[_stepIndex].name}", this);
        steps[_stepIndex].onEnter?.Invoke();
    }

    private void ApplyStepActivation(bool initial)
    {
        // Simple rule: ONLY the active step’s mechanics are enabled.
        // Everything in other steps gets disabled.
        for (int si = 0; si < steps.Count; si++)
        {
            bool active = (si == _stepIndex);
            Step s = steps[si];

            // behaviours
            if (s.enableBehaviours != null)
            {
                for (int i = 0; i < s.enableBehaviours.Count; i++)
                {
                    var b = s.enableBehaviours[i];
                    if (b != null) b.enabled = active;
                }
            }

            // objects enabled
            if (s.enableObjects != null)
            {
                for (int i = 0; i < s.enableObjects.Count; i++)
                {
                    var go = s.enableObjects[i];
                    if (go != null) go.SetActive(active);
                }
            }

            // objects forced off while active
            if (active && s.forceOffObjects != null)
            {
                for (int i = 0; i < s.forceOffObjects.Count; i++)
                {
                    var go = s.forceOffObjects[i];
                    if (go != null) go.SetActive(false);
                }
            }
        }

        if (initial && log)
            Debug.Log($"[Scenario] Initial step: {_stepIndex} ({CurrentStepName})", this);
    }

    private static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

    // Optional testing buttons you can call from UnityEvents
    public void DebugAdvanceStep() => ChangeStep(Mathf.Min(_stepIndex + 1, steps.Count - 1));
    public void DebugRaiseFlag(string f) => RaiseFlag(f);
}
