using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ClothOverlapMaterialDriver : MonoBehaviour
{
    public float interval = 2f;

    private MaterialStageController clothStage;
    private MaterialStageController characterStage;

    private float timer;
    private bool overlapping;

    void Awake()
    {
        clothStage = GetComponent<MaterialStageController>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!overlapping || characterStage == null) return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;

            if (clothStage.CanAdvance())
                clothStage.Advance();

            if (characterStage.CanAdvance())
                characterStage.Advance();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var stage = other.GetComponentInParent<MaterialStageController>();
        if (stage == null) return;

        Debug.Log("Character entered");

        characterStage = stage;
        overlapping = true;
        timer = 0f;
    }

    void OnTriggerExit(Collider other)
    {
        var stage = other.GetComponentInParent<MaterialStageController>();
        if (stage != characterStage) return;

        Debug.Log("Character exited");

        overlapping = false;
        characterStage = null;
    }
}
