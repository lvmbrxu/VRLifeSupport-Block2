using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunResultsStore : MonoBehaviour
{
    public static RunResultsStore Instance { get; private set; }

    public bool HasResults { get; private set; }
    public CprResultsSnapshot Results { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Save(CprResultsSnapshot snapshot)
    {
        Results = snapshot;
        HasResults = true;
    }

    public void Clear()
    {
        HasResults = false;
        Results = default;
    }
}