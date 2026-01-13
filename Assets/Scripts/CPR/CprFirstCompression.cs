using System.Reflection;
using UnityEngine;

public sealed class CprFirstCompressionFlag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private MonoBehaviour cprHeartMonitor; // drag your CprHeartMonitor component here

    [Header("Flag")]
    [SerializeField] private string startedCprFlag = "StartedCPR";

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private FieldInfo _compressionCountField;
    private bool _raised;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        if (cprHeartMonitor != null)
        {
            _compressionCountField = cprHeartMonitor.GetType().GetField("_compressionCount",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (_compressionCountField == null && log)
            Debug.LogWarning("[CprFirstCompressionFlag] Could not find _compressionCount. " +
                             "If your CPR script name/field changed, we need to update reflection.", this);
    }

    private void Update()
    {
        if (_raised || scenario == null || cprHeartMonitor == null || _compressionCountField == null)
            return;

        int count = (int)_compressionCountField.GetValue(cprHeartMonitor);
        if (count >= 1)
        {
            _raised = true;
            scenario.RaiseFlag(startedCprFlag);

            if (log) Debug.Log("[CprFirstCompressionFlag] StartedCPR raised (first compression).", this);

            // no need to keep checking
            enabled = false;
        }
    }
}