using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScenarioProgress : MonoBehaviour
{
    [Serializable]
    public sealed class Step
    {
        public string name;

        [Tooltip("All of these must be raised to complete this step.")]
        public List<ScenarioFlag> requiredFlags = new List<ScenarioFlag>();

        [Header("Actions allowed during this step")]
        public bool allowMakeSpaceGesture = false;
        public bool allowStartCPR = false;
        public bool allowUseAED = false;
    }

    [Header("Steps (Minute 1 first)")]
    [SerializeField] private List<Step> steps = new List<Step>();

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    private readonly HashSet<ScenarioFlag> _flags = new HashSet<ScenarioFlag>();
    private int _stepIndex = 0;

    public int StepIndex => _stepIndex;
    public string CurrentStepName => IsValidStep(_stepIndex) ? steps[_stepIndex].name : "DONE";

    public event Action<int, string> OnStepChanged;

    private void OnEnable()
    {
        ScenarioSignals.OnFlagRaised += HandleFlagRaised;
    }

    private void OnDisable()
    {
        ScenarioSignals.OnFlagRaised -= HandleFlagRaised;
    }

    private void Start()
    {
        // Make sure step list exists
        if (steps == null || steps.Count == 0)
        {
            Debug.LogError("ScenarioProgress: No steps configured.", this);
            enabled = false;
            return;
        }

        NotifyStepChanged();
        TryAdvance(); // handles steps with no required flags
    }

    private void HandleFlagRaised(ScenarioFlag flag)
    {
        if (_flags.Add(flag))
        {
            if (logProgress)
                Debug.Log($"[Scenario] Flag raised: {flag} (Step: {CurrentStepName})", this);

            TryAdvance();
        }
    }

    private void TryAdvance()
    {
        while (IsValidStep(_stepIndex) && IsStepComplete(steps[_stepIndex]))
        {
            if (logProgress)
                Debug.Log($"[Scenario] Step complete: {steps[_stepIndex].name}", this);

            _stepIndex++;

            if (!IsValidStep(_stepIndex))
            {
                if (logProgress)
                    Debug.Log("[Scenario] Scenario complete!", this);
                NotifyStepChanged();
                return;
            }

            NotifyStepChanged();
        }
    }

    private bool IsStepComplete(Step step)
    {
        for (int i = 0; i < step.requiredFlags.Count; i++)
        {
            if (!_flags.Contains(step.requiredFlags[i]))
                return false;
        }
        return true;
    }

    private bool IsValidStep(int index) => steps != null && index >= 0 && index < steps.Count;

    private void NotifyStepChanged()
    {
        OnStepChanged?.Invoke(_stepIndex, CurrentStepName);

        if (logProgress)
            Debug.Log($"[Scenario] Enter step {_stepIndex}: {CurrentStepName}", this);
    }

    public bool CanDo(ScenarioAction action)
    {
        if (!IsValidStep(_stepIndex)) return false;

        Step s = steps[_stepIndex];
        return action switch
        {
            ScenarioAction.MakeSpaceGesture => s.allowMakeSpaceGesture,
            ScenarioAction.StartCPR => s.allowStartCPR,
            ScenarioAction.UseAED => s.allowUseAED,
            _ => false
        };
    }
}
