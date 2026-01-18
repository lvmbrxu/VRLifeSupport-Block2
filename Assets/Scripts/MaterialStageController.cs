using UnityEngine;

public class MaterialStageController : MonoBehaviour
{
    public Material[] stages;
    private int currentStage = 0;
    private Renderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ApplyStage();
    }

    public bool CanAdvance()
    {
        return stages != null && currentStage < stages.Length - 1;
    }

    public void Advance()
    {
        if (!CanAdvance()) return;
        currentStage++;
        ApplyStage();
    }

    public void ResetStage()
    {
        currentStage = 0;
        ApplyStage();
    }

    private void ApplyStage()
    {
        if (stages == null || stages.Length == 0) return;

        foreach (var r in renderers)
            r.sharedMaterial = stages[currentStage];
    }
}
