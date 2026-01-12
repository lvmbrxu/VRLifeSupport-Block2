using UnityEngine;

public sealed class MultiFlagGate : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector director;

    [Tooltip("ALL of these must be true before enabling the targets.")]
    [SerializeField] private string[] requiredFlags;

    [Header("Enable/Disable These When Unlocked")]
    [Tooltip("Drag your existing mechanic scripts here (CPR scripts, gesture scripts, AED scripts, etc.).")]
    [SerializeField] private Behaviour[] enableTheseBehaviours;

    [Tooltip("Optional: force these objects OFF while locked (like CPR pose hands).")]
    [SerializeField] private GameObject[] forceOffWhileLocked;

    [Header("Debug")]
    [SerializeField] private bool logState = true;

    private bool _lastUnlocked;

    private void OnEnable()
    {
        if (director != null)
            director.FlagsChanged += Apply;

        Apply();
    }

    private void OnDisable()
    {
        if (director != null)
            director.FlagsChanged -= Apply;
    }

    private void Apply()
    {
        if (director == null)
        {
            Debug.LogError("MultiFlagGate: Missing ScenarioDirector reference.", this);
            return;
        }

        bool unlocked = AreAllFlagsTrue();
        if (unlocked == _lastUnlocked) return;
        _lastUnlocked = unlocked;

        // Toggle behaviours
        for (int i = 0; i < enableTheseBehaviours.Length; i++)
        {
            if (enableTheseBehaviours[i] != null)
                enableTheseBehaviours[i].enabled = unlocked;
        }

        // Force objects off while locked
        if (!unlocked)
        {
            for (int i = 0; i < forceOffWhileLocked.Length; i++)
            {
                if (forceOffWhileLocked[i] != null)
                    forceOffWhileLocked[i].SetActive(false);
            }
        }

        if (logState)
            Debug.Log($"[Gate] {(unlocked ? "UNLOCKED" : "LOCKED")} (needs: {string.Join(", ", requiredFlags)})", this);
    }

    private bool AreAllFlagsTrue()
    {
        if (requiredFlags == null || requiredFlags.Length == 0) return true;

        for (int i = 0; i < requiredFlags.Length; i++)
        {
            if (!director.HasFlag(requiredFlags[i]))
                return false;
        }
        return true;
    }
}
