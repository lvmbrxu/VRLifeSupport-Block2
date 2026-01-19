using UnityEngine;
using UnityEngine.Events;

public class MaterialStageController : MonoBehaviour
{
    [Header("Stages")]
    public Material[] stages;

    [Header("Events")]
    public UnityEvent onReachedLastStage;

    private int currentStage = 0;
    private Renderer[] renderers;
    private bool _firedLastStageEvent;

    public int CurrentStage => currentStage;
    public int LastStageIndex => (stages == null) ? -1 : stages.Length - 1;
    public bool IsLastStage => stages != null && stages.Length > 0 && currentStage >= stages.Length - 1;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        ApplyStage();
        TryFireLastStageEvent();
    }

    public bool CanAdvance()
    {
        return stages != null && stages.Length > 0 && currentStage < stages.Length - 1;
    }

    public void Advance()
    {
        if (!CanAdvance()) return;

        currentStage++;
        ApplyStage();
        TryFireLastStageEvent();
    }

    public void ResetStage()
    {
        currentStage = 0;
        _firedLastStageEvent = false;
        ApplyStage();
    }

    private void ApplyStage()
    {
        if (stages == null || stages.Length == 0) return;

        var mat = stages[Mathf.Clamp(currentStage, 0, stages.Length - 1)];
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.sharedMaterial = mat;
        }
    }

    private void TryFireLastStageEvent()
    {
        if (_firedLastStageEvent) return;
        if (!IsLastStage) return;

        _firedLastStageEvent = true;
        onReachedLastStage?.Invoke();
    }
}