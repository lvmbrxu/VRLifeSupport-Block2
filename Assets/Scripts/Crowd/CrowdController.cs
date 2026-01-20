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

        [Header("Optional leave clips (if empty, uses clips)")]
        public List<AudioClip> leaveClips = new List<AudioClip>();
    }

    private sealed class NpcRuntime
    {
        public GameObject root;
        public NavMeshAgent agent;
        public Animator animator;
    }

    [Header("Scenario")]
    [SerializeField] private ScenarioDirector scenario;
    [SerializeField] private string startFlag = "DogHandled";
    [SerializeField] private string clearFlag = "CrowdCleared";

    [Header("Crowd NPCs (pre-placed, disabled at start)")]
    [SerializeField] private List<GameObject> npcRoots = new List<GameObject>();

    [Header("Slots Around Victim")]
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
    [SerializeField] private List<NpcVoice> npcVoices = new List<NpcVoice>();
    [SerializeField] private bool playVoicesOnArrived = true;
    [SerializeField] private float voiceStartDelay = 0.2f;
    [SerializeField] private Vector2 voiceGapRange = new Vector2(0.15f, 0.5f);
    [SerializeField] private bool loopChatter = true;
    [SerializeField] private Vector2 chatterRoundPauseRange = new Vector2(2.0f, 5.0f);

    [Header("Leaving VO")]
    [SerializeField] private bool playLeaveVoices = true;
    [SerializeField] private float leaveVoiceStartDelay = 0.1f;

    [Header("Animator")]
    [Tooltip("Bool parameter that is TRUE while NPC is moving.")]
    [SerializeField] private string isWalkingBoolParam = "IsWalking";

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private readonly List<NpcRuntime> _npcs = new List<NpcRuntime>(16);

    private bool started;
    private bool clearing;
    private Coroutine routine;
    private Coroutine voiceRoutine;

    private void Awake()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<ScenarioDirector>();

        _npcs.Clear();
        for (int i = 0; i < npcRoots.Count; i++)
        {
            var root = npcRoots[i];
            if (root == null) continue;

            // Cache agent/anim once
            var rt = new NpcRuntime
            {
                root = root,
                agent = root.GetComponentInChildren<NavMeshAgent>(true),
                animator = root.GetComponentInChildren<Animator>(true)
            };
            _npcs.Add(rt);

            // start hidden
            root.SetActive(false);
        }

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

        int count = Mathf.Min(_npcs.Count, crowdSlots.Count);

        // Enable and send to slots
        for (int i = 0; i < count; i++)
        {
            var npc = _npcs[i];
            var slot = crowdSlots[i];
            if (npc.root == null || slot == null) continue;

            npc.root.SetActive(true);

            SetWalking(npc, true);

            if (npc.agent != null && npc.agent.enabled)
            {
                if (!npc.agent.isOnNavMesh && NavMesh.SamplePosition(npc.agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
                    npc.agent.Warp(hit.position);

                npc.agent.isStopped = false;
                npc.agent.ResetPath();
                npc.agent.SetDestination(slot.position);
            }
            else
            {
                npc.root.transform.SetPositionAndRotation(slot.position, slot.rotation);
                SetWalking(npc, false);
            }
        }

        StartCrowdAudio();

        while (!clearing && !AllArrived(count))
            yield return null;

        if (clearing) yield break;

        // Everyone arrived -> stop walking anim
        for (int i = 0; i < count; i++)
            SetWalking(_npcs[i], false);

        if (raiseCrowdArrivedFlag)
            scenario.RaiseFlag(crowdArrivedFlag);

        if (playVoicesOnArrived)
        {
            if (voiceRoutine != null) StopCoroutine(voiceRoutine);
            voiceRoutine = StartCoroutine(VoicesRoutine());
        }

        while (!clearing)
            yield return null;
    }

    private IEnumerator ClearRoutine()
    {
        if (log) Debug.Log("[Crowd] Clearing crowd", this);

        // Stop chatter coroutine, then optionally play leave lines
        if (voiceRoutine != null)
        {
            StopCoroutine(voiceRoutine);
            voiceRoutine = null;
        }

        if (playLeaveVoices)
            yield return StartCoroutine(PlayLeaveLinesRoutine());

        StopCrowdAudio();

        // Send them away
        for (int i = 0; i < _npcs.Count; i++)
        {
            var npc = _npcs[i];
            if (npc.root == null || !npc.root.activeSelf) continue;

            SetWalking(npc, true);

            if (npc.agent != null && npc.agent.enabled && exitPoint != null)
            {
                npc.agent.isStopped = false;
                npc.agent.ResetPath();
                npc.agent.SetDestination(exitPoint.position);
            }
        }

        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < _npcs.Count; i++)
        {
            if (_npcs[i].root != null)
                _npcs[i].root.SetActive(false);
        }
    }

    // ===== VO =====

    private IEnumerator VoicesRoutine()
    {
        if (voiceStartDelay > 0f)
            yield return new WaitForSeconds(voiceStartDelay);

        AutoBuildVoicesIfNeeded();

        while (!clearing)
        {
            // One round: strict sequential (no overlap)
            for (int i = 0; i < npcVoices.Count; i++)
            {
                if (clearing) yield break;

                var v = npcVoices[i];
                if (v == null || v.npcRoot == null || !v.npcRoot.activeSelf) continue;
                if (v.audioSource == null) continue;
                if (v.clips == null || v.clips.Count == 0) continue;

                var clip = v.clips[Random.Range(0, v.clips.Count)];
                if (clip == null) continue;

                v.audioSource.Stop();
                v.audioSource.PlayOneShot(clip);

                yield return new WaitForSeconds(clip.length);

                float gap = Random.Range(voiceGapRange.x, voiceGapRange.y);
                if (gap > 0f) yield return new WaitForSeconds(gap);
            }

            if (!loopChatter) yield break;

            float pause = Random.Range(chatterRoundPauseRange.x, chatterRoundPauseRange.y);
            if (pause > 0f) yield return new WaitForSeconds(pause);
        }
    }

    private IEnumerator PlayLeaveLinesRoutine()
    {
        if (leaveVoiceStartDelay > 0f)
            yield return new WaitForSeconds(leaveVoiceStartDelay);

        AutoBuildVoicesIfNeeded();

        for (int i = 0; i < npcVoices.Count; i++)
        {
            var v = npcVoices[i];
            if (v == null || v.npcRoot == null || !v.npcRoot.activeSelf) continue;
            if (v.audioSource == null) continue;

            List<AudioClip> pool = (v.leaveClips != null && v.leaveClips.Count > 0) ? v.leaveClips : v.clips;
            if (pool == null || pool.Count == 0) continue;

            var clip = pool[Random.Range(0, pool.Count)];
            if (clip == null) continue;

            v.audioSource.Stop();
            v.audioSource.PlayOneShot(clip);

            yield return new WaitForSeconds(clip.length);

            float gap = Random.Range(voiceGapRange.x, voiceGapRange.y);
            if (gap > 0f) yield return new WaitForSeconds(gap);
        }
    }

    private void AutoBuildVoicesIfNeeded()
    {
        if (npcVoices != null && npcVoices.Count > 0) return;

        npcVoices = new List<NpcVoice>(npcRoots.Count);
        for (int i = 0; i < npcRoots.Count; i++)
        {
            var npc = npcRoots[i];
            if (npc == null) continue;

            npcVoices.Add(new NpcVoice
            {
                npcRoot = npc,
                audioSource = npc.GetComponentInChildren<AudioSource>(true),
                clips = new List<AudioClip>(),
                leaveClips = new List<AudioClip>()
            });
        }

        if (log)
            Debug.Log("[Crowd] npcVoices list was empty. Auto-found AudioSources, but you still need to assign clips.", this);
    }

    // ===== movement helpers =====

    private bool AllArrived(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var npc = _npcs[i];
            var slot = crowdSlots[i];
            if (npc.root == null || slot == null) continue;

            if (npc.agent != null && npc.agent.enabled)
            {
                if (npc.agent.pathPending) return false;

                float stop = Mathf.Max(arriveDistance, npc.agent.stoppingDistance + 0.05f);
                if (npc.agent.remainingDistance > stop)
                    return false;
            }
            else
            {
                if (Vector3.Distance(npc.root.transform.position, slot.position) > arriveDistance)
                    return false;
            }
        }
        return true;
    }

    private void SetWalking(NpcRuntime npc, bool walking)
    {
        if (npc == null || npc.animator == null) return;
        if (string.IsNullOrWhiteSpace(isWalkingBoolParam)) return;

        npc.animator.SetBool(isWalkingBoolParam, walking);
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
