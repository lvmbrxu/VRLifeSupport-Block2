using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class CrowdDistractionController : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string startFlag = "DogHandled";
    [SerializeField] private string clearFlag = "CrowdCleared";

    [Header("Crowd NPCs (pre-placed, disabled at start)")]
    [Tooltip("Put your NPC roots here. Each should have a NavMeshAgent somewhere in children.")]
    [SerializeField] private List<GameObject> npcRoots = new List<GameObject>();

    [Header("Slots Around Victim")]
    [Tooltip("One slot per NPC. They will walk to these points and stand there.")]
    [SerializeField] private List<Transform> crowdSlots = new List<Transform>();

    [Header("Exit")]
    [SerializeField] private Transform exitPoint;

    [Header("Audio")]
    [SerializeField] private AudioSource crowdAudio;
    [SerializeField] private AudioClip crowdLoopClip;

    [Header("Timing")]
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 6f;

    [Header("Arrival")]
    [SerializeField] private float arriveDistance = 0.8f;

    [Header("Optional flags")]
    [SerializeField] private bool raiseCrowdArrivedFlag = true;
    [SerializeField] private string crowdArrivedFlag = "CrowdArrived";

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool started;
    private bool clearing;
    private Coroutine routine;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        // start hidden
        for (int i = 0; i < npcRoots.Count; i++)
            if (npcRoots[i] != null) npcRoots[i].SetActive(false);

        if (crowdAudio != null)
        {
            crowdAudio.playOnAwake = false;
            crowdAudio.loop = true;
            crowdAudio.Stop();
        }
    }

    private void OnEnable()
    {
        if (scenario != null) scenario.FlagsChanged += Evaluate;
        Evaluate();
    }

    private void OnDisable()
    {
        if (scenario != null) scenario.FlagsChanged -= Evaluate;
    }

    private void Evaluate()
    {
        if (scenario == null) return;

        if (!started && scenario.HasFlag(startFlag))
        {
            started = true;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(StartRoutine());
        }

        if (started && !clearing && scenario.HasFlag(clearFlag))
        {
            clearing = true;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ClearRoutine());
        }
    }

    private IEnumerator StartRoutine()
    {
        float delay = Random.Range(minDelay, maxDelay);
        if (log) Debug.Log($"[Crowd] Will arrive in {delay:F1}s", this);
        yield return new WaitForSeconds(delay);

        // Enable and send to slots
        int count = Mathf.Min(npcRoots.Count, crowdSlots.Count);
        for (int i = 0; i < count; i++)
        {
            var npc = npcRoots[i];
            var slot = crowdSlots[i];
            if (npc == null || slot == null) continue;

            npc.SetActive(true);

            var agent = npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(slot.position);
            }
            else
            {
                // fallback teleport if no agent
                npc.transform.position = slot.position;
                npc.transform.rotation = slot.rotation;
            }
        }

        // Start noise once they begin arriving
        StartCrowdAudio();

        // Wait until all arrived (or until cleared)
        while (!clearing && !AllArrived())
            yield return null;

        if (!clearing && raiseCrowdArrivedFlag)
            scenario.RaiseFlag(crowdArrivedFlag);

        // idle until cleared
        while (!clearing)
            yield return null;
    }

    private IEnumerator ClearRoutine()
    {
        if (log) Debug.Log("[Crowd] Clearing crowd", this);

        StopCrowdAudio();

        // Send them away
        for (int i = 0; i < npcRoots.Count; i++)
        {
            var npc = npcRoots[i];
            if (npc == null) continue;

            var agent = npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null && agent.enabled && exitPoint != null)
            {
                agent.isStopped = false;
                agent.SetDestination(exitPoint.position);
            }
        }

        // Wait a little then hide (simple)
        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < npcRoots.Count; i++)
            if (npcRoots[i] != null) npcRoots[i].SetActive(false);
    }

    private bool AllArrived()
    {
        int count = Mathf.Min(npcRoots.Count, crowdSlots.Count);
        for (int i = 0; i < count; i++)
        {
            var npc = npcRoots[i];
            var slot = crowdSlots[i];
            if (npc == null || slot == null) continue;

            var agent = npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null && agent.enabled)
            {
                if (agent.pathPending) return false;
                if (agent.remainingDistance > Mathf.Max(arriveDistance, agent.stoppingDistance + 0.05f))
                    return false;
            }
            else
            {
                if (Vector3.Distance(npc.transform.position, slot.position) > arriveDistance)
                    return false;
            }
        }
        return true;
    }

    private void StartCrowdAudio()
    {
        if (crowdAudio == null || crowdLoopClip == null) return;
        crowdAudio.clip = crowdLoopClip;
        crowdAudio.loop = true;
        crowdAudio.Play();
    }

    private void StopCrowdAudio()
    {
        if (crowdAudio == null) return;
        crowdAudio.Stop();
        crowdAudio.clip = null;
    }
}
