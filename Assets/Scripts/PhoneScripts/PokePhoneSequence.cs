using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PokePhoneSequence : MonoBehaviour
{
    [Header("Targets")]
    public Transform pose1Target;
    public Transform pose2Target;

    [Header("Renderer / Materials")]
    public Renderer phoneRenderer;
    public Material step1Material;            // after 1st poke
    public Material notificationMaterial;     // shown on 2nd poke (stays until 3rd poke)
    public Material step2Material;            // final after 3rd poke

    [Header("Motion")]
    public float moveSpeed = 2f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("2nd poke audio")]
    public AudioClip notificationClip;
    [Tooltip("Delay before playing notification sound after 2nd poke.")]
    public float notificationSoundDelay = 0.2f;

    [Header("3rd poke audio (FINAL)")]
    public AudioClip finalClip;
    [Tooltip("Delay before playing final sound after 3rd poke.")]
    public float finalSoundDelay = 0.2f;

    [Tooltip("Disable AudioSource again after playing a clip (prevents any weird autoplay).")]
    public bool disableAudioSourceAfterPlay = true;

    private int step = 0;
    private Coroutine moveRoutine;
    private Coroutine soundRoutine;

    private void Awake()
    {
        // Force audio OFF at start (so it can't play on awake)
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.enabled = false;
            audioSource.playOnAwake = false;
        }
    }

    // Hook this to XRSimpleInteractable -> Select Entered
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Only allow poke
        if (args.interactorObject is not XRPokeInteractor)
            return;

        Debug.Log("Poke detected → step: " + step);

        switch (step)
        {
            case 0:
                // 1st poke
                ApplyMaterial(step1Material);
                StartMove(pose1Target);
                step = 1;
                break;

            case 1:
                // 2nd poke: show notification and play sound
                ApplyMaterial(notificationMaterial);
                PlayClipWithDelay(notificationClip, notificationSoundDelay);
                step = 2;
                break;

            case 2:
                // 3rd poke: finalize (material + move + FINAL sound)
                ApplyMaterial(step2Material);
                StartMove(pose2Target);
                PlayClipWithDelay(finalClip, finalSoundDelay);
                step = 3;
                break;

            // step 3+: ignore further pokes
        }
    }

    private void PlayClipWithDelay(AudioClip clip, float delay)
    {
        if (audioSource == null || clip == null)
            return;

        if (soundRoutine != null)
        {
            StopCoroutine(soundRoutine);
            soundRoutine = null;
        }

        soundRoutine = StartCoroutine(PlayClipDelayedRoutine(clip, delay));
    }

    private IEnumerator PlayClipDelayedRoutine(AudioClip clip, float delay)
    {
        audioSource.enabled = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        audioSource.PlayOneShot(clip);

        // optionally disable after it finishes
        if (disableAudioSourceAfterPlay)
        {
            yield return new WaitForSeconds(clip.length);
            audioSource.Stop();
            audioSource.enabled = false;
        }

        soundRoutine = null;
    }

    private void StartMove(Transform target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveTo(target));
    }

    private IEnumerator MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("Target is NULL!");
            yield break;
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);
            yield return null;
        }
    }

    private void ApplyMaterial(Material mat)
    {
        if (phoneRenderer == null || mat == null)
            return;

        // sharedMaterial avoids instancing materials every assignment
        phoneRenderer.sharedMaterial = mat;
    }
}
