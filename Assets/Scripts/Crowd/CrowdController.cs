using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class CrowdDistractionController : MonoBehaviour
{
    [System.Serializable]
    public sealed class NpcVoice
    {
        public GameObject npcRoot;
        public AudioSource audioSource;
        public List<AudioClip> clips = new List<AudioClip>();
    }

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

    [Header("Timing")]
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 6f;

    [Header("Arrival")]
    [SerializeField] private float arriveDistance = 0.8f;

    [Header("Optional flags")]
    [SerializeField] private bool raiseCrowdArrivedFlag = true;
    [SerializeField] private string crowdArrivedFlag = "CrowdArrived";

    [Header("Crowd Loop Audio (optional ambience)")]
    [SerializeField] private AudioSource crowdAudio;
    [SerializeField] private AudioClip crowdLoopClip;

    [Header("Staggered NPC Voice Lines")]
    [Tooltip("Fill this for per-NPC voice lines. If empty, controller will try to auto-find AudioSources on npcRoots.")]
    [SerializeField] private List<NpcVoice> npcVoices = new List<NpcVoice>();

    [Tooltip("Start VO lines when CrowdArrived is raised.")]
    [SerializeField] private bool playVoicesOnArrived = true;

    [Tooltip("Delay before first VO line after crowd arrives.")]
    [SerializeField] private float voiceStartDelay = 0.2f;

    [Tooltip("Time between each NPC speaking (random range).")]
    [SerializeField] private Vector2 voiceStaggerRange = new Vector2(0.4f, 1.2f);

    [Tooltip("If true, continues chatter in a loop until cleared.")]
    [SerializeField] private bool loopChatter = true;

    [Tooltip("If looping, wait random time between 'rounds' of chatter.")]
    [SerializeField] private Vector2 chatterRoundPauseRange = new Vector2(2.0f, 5.0f);

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool started;
    private bool clearing;
    private Coroutine routine;
    private Coroutine voiceRoutine;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        // Start hidden
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
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
                    agent.Warp(hit.position);

                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(slot.position);
            }
            else
            {
                npc.transform.SetPositionAndRotation(slot.position, slot.rotation);
            }
        }

        StartCrowdAudio();

        // Wait until all arrived (or until cleared)
        while (!clearing && !AllArrived())
            yield return null;

        if (clearing) yield break;

        if (raiseCrowdArrivedFlag)
            scenario.RaiseFlag(crowdArrivedFlag);

        // Start staggered VO
        if (playVoicesOnArrived)
        {
            if (voiceRoutine != null) StopCoroutine(voiceRoutine);
            voiceRoutine = StartCoroutine(VoicesRoutine());
        }

        // Idle until cleared
        while (!clearing)
            yield return null;
    }

    private IEnumerator ClearRoutine()
    {
        if (log) Debug.Log("[Crowd] Clearing crowd", this);

        // Stop chatter immediately
        if (voiceRoutine != null)
        {
            StopCoroutine(voiceRoutine);
            voiceRoutine = null;
        }

        StopCrowdAudio();

        // Send them away
        for (int i = 0; i < npcRoots.Count; i++)
        {
            var npc = npcRoots[i];
            if (npc == null || !npc.activeSelf) continue;

            var agent = npc.GetComponentInChildren<NavMeshAgent>(true);
            if (agent != null && agent.enabled && exitPoint != null)
            {
                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);
            }
        }

        // Simple hide after a bit
        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < npcRoots.Count; i++)
            if (npcRoots[i] != null) npcRoots[i].SetActive(false);
    }

    private IEnumerator VoicesRoutine()
    {
        if (voiceStartDelay > 0f)
            yield return new WaitForSeconds(voiceStartDelay);

        // Build voice list if empty (auto-find AudioSources)
        if (npcVoices == null || npcVoices.Count == 0)
        {
            npcVoices = new List<NpcVoice>(npcRoots.Count);
            for (int i = 0; i < npcRoots.Count; i++)
            {
                var npc = npcRoots[i];
                if (npc == null) continue;
                npcVoices.Add(new NpcVoice
                {
                    npcRoot = npc,
                    audioSource = npc.GetComponentInChildren<AudioSource>(true),
                    clips = new List<AudioClip>() // you must assign clips if using auto mode
                });
            }

            if (log)
                Debug.Log("[Crowd] npcVoices list was empty. Auto-found AudioSources, but you still need to assign clips.", this);
        }

        while (!clearing)
        {
            // One "round" of chatter: NPCs talk one-by-one with random stagger
            for (int i = 0; i < npcVoices.Count; i++)
            {
                if (clearing) yield break;

                var v = npcVoices[i];
                if (v == null || v.npcRoot == null || !v.npcRoot.activeSelf) continue;
                if (v.audioSource == null) continue;
                if (v.clips == null || v.clips.Count == 0) continue;

                float gap = Random.Range(voiceStaggerRange.x, voiceStaggerRange.y);
                if (gap > 0f) yield return new WaitForSeconds(gap);

                // Play random clip
                var clip = v.clips[Random.Range(0, v.clips.Count)];
                if (clip != null)
                    v.audioSource.PlayOneShot(clip);
            }

            if (!loopChatter) yield break;

            float pause = Random.Range(chatterRoundPauseRange.x, chatterRoundPauseRange.y);
            if (pause > 0f) yield return new WaitForSeconds(pause);
        }
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
